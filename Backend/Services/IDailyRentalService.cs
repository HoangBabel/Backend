using Backend.Data;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using static Backend.DTOs.DailyRentalDtos;

namespace Backend.Services
{
    public interface IDailyRentalService
    {
        Task<QuoteDailyResponseDto> GetDailyQuoteAsync(QuoteDailyRequestDto dto);
        Task<CreateDailyRentalResponseDto> CreateDailyRentalAsync(CreateDailyRentalRequestDto dto, int userId);

    }
    public class DailyRentalService : IDailyRentalService
    {
        private readonly AppDbContext _db;
        public DailyRentalService(AppDbContext db) => _db = db;

        public async Task<QuoteDailyResponseDto> GetDailyQuoteAsync(QuoteDailyRequestDto dto)
        {
            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProduct == dto.ProductId)
                ?? throw new InvalidOperationException("Sản phẩm không tồn tại.");

            if (!product.IsRental)
                throw new InvalidOperationException("Sản phẩm này không hỗ trợ thuê.");

            var plan = await _db.RentalPlans.AsNoTracking()
         .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId && p.Unit == RentalUnit.Day);

            if (plan == null)
            {
                // Tự tạo plan + tiers từ giá bán
                var _auto = await RentalPlanAutoGenerator.EnsureDailyPlanAsync(_db, dto.ProductId);
                plan = _auto.plan;
            }

            int days = RentalPricingHelper.ComputeDays(dto.StartDate, dto.EndDate);
            if (days < plan.MinUnits)
                throw new InvalidOperationException($"Cần thuê tối thiểu {plan.MinUnits} ngày.");

            var tiers = await _db.RentalPricingTiers.AsNoTracking()
          .Where(t => t.ProductId == dto.ProductId)
          .ToListAsync();

            var (pricePerDay, appliedThreshold) =
                RentalPricingHelper.PickTierPriceOrBase(days, tiers, plan.PricePerUnit);

            var subtotal = Math.Round(pricePerDay * days, 2, MidpointRounding.AwayFromZero);

            return new QuoteDailyResponseDto(days, pricePerDay, appliedThreshold, plan.Deposit, subtotal);
        }

        public async Task<CreateDailyRentalResponseDto> CreateDailyRentalAsync(CreateDailyRentalRequestDto dto, int userId)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.IdProduct == dto.ProductId)
                ?? throw new InvalidOperationException("Sản phẩm không tồn tại.");

            if (!product.IsRental)
                throw new InvalidOperationException("Sản phẩm này không hỗ trợ thuê.");

            if (dto.Quantity <= 0)
                throw new InvalidOperationException("Số lượng thuê phải >= 1.");

            // Check tồn kho tức thời (nếu policy cho phép overbook thì bỏ check này)
            if (product.Quantity < dto.Quantity)
                throw new InvalidOperationException($"Sản phẩm chỉ còn {product.Quantity} cái khả dụng.");

            var plan = await _db.RentalPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId && p.Unit == RentalUnit.Day);

            if (plan == null)
            {
                // bạn đã có helper tự sinh plan theo giá sản phẩm
                var auto = await RentalPlanAutoGenerator.EnsureDailyPlanAsync(_db, dto.ProductId);
                plan = auto.plan;
            }

            int days = RentalPricingHelper.ComputeDays(dto.StartDate, dto.EndDate);
            if (days < plan.MinUnits)
                throw new InvalidOperationException($"Cần thuê tối thiểu {plan.MinUnits} ngày.");

            var tiers = await _db.RentalPricingTiers.AsNoTracking()
                .Where(t => t.ProductId == dto.ProductId)
                .ToListAsync();

            var (pricePerDay, _) = RentalPricingHelper.PickTierPriceOrBase(days, tiers, plan.PricePerUnit);

            var rental = new Rental
            {
                UserId = userId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                //ShippingAddress = dto.ShippingAddress, // ✅ FIX
                //ToProvinceId = dto.ToProvinceId,
                //ToDistrictId = dto.ToDistrictId,
                //ToWardCode = dto.ToWardCode,
                Status = RentalStatus.Pending
            };
            rental.EnsureValidDateRange();

            var item = new RentalItem { ProductId = dto.ProductId };
            item.SnapshotPricing(pricePerUnit: pricePerDay, deposit: plan.Deposit, lateFeePerUnit: plan.LateFeePerUnit);
            item.SetUnits(days);
            item.SetQuantity(dto.Quantity); // ✅ SỐ LƯỢNG

            rental.Items.Add(item);
            rental.RecalculateTotal();
            rental.SnapshotDepositFromItems();

            _db.Rentals.Add(rental);
            await _db.SaveChangesAsync();

            var finalAmountToPay = rental.TotalPrice + rental.DepositPaid;

            return new CreateDailyRentalResponseDto(
                rental.Id,
                rental.TotalPrice,
                rental.DepositPaid,
                finalAmountToPay,
                rental.Status
            );
        }
      
    }

}

