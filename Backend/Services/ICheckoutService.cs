    using Backend.Data;
    using Backend.DTOs;
    using Backend.Models;
    using Microsoft.EntityFrameworkCore;
    using static Backend.Helpers.DateTimeHelper; // nếu bạn muốn dùng helper TZ VN
    using static Backend.Helpers.VoucherCalculator;
    using static Backend.Helpers.VoucherValidator;

namespace Backend.Services;

    public interface ICheckoutService
    {
        Task<CheckoutOrderResponse> CheckoutOrderAsync(int userId, CheckoutOrderRequest req, CancellationToken ct);
        //Task<CheckoutRentalResponse> CheckoutRentalByDaysAsync(CheckoutRentalByDaysRequest req, CancellationToken ct = default);
        //Task<CheckoutRentalResponse> CheckoutRentalByDatesAsync(CheckoutRentalByDatesRequest req, CancellationToken ct = default);
    }

    public class CheckoutService : ICheckoutService
    {
        private readonly AppDbContext _context;
        private readonly IShippingService _shippingService; // ✅ THÊM

    public CheckoutService(AppDbContext context, IShippingService shippingService)
    {
        _context = context;
        _shippingService = shippingService;
    }

    // Services/CheckoutService.cs
    public async Task<CheckoutOrderResponse> CheckoutOrderAsync(
        int userId,
        CheckoutOrderRequest req,
        CancellationToken ct)
    {
        // 1️⃣ Lấy giỏ hàng
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Giỏ hàng trống hoặc không tồn tại.");

        // 2️⃣ Tính tổng tiền hàng
        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        // 3️⃣ ===== TÍNH TRỌNG LƯỢNG (KHÔNG CẦN Product.Weight) =====
        int totalWeight = req.Weight ?? 0;

        if (totalWeight == 0)
        {
            // Tính theo số lượng sản phẩm: 200g/sản phẩm
            int totalItems = cart.Items.Sum(i => i.Quantity);
            totalWeight = totalItems * 200;
        }

        // Đảm bảo trong khoảng hợp lệ của GHN
        if (totalWeight < 200) totalWeight = 200;       // Tối thiểu 200g
        if (totalWeight > 30000) totalWeight = 30000;   // Tối đa 30kg

        // 4️⃣ ===== TÍNH PHÍ SHIP =====
        var shippingRequest = new ShippingFeeRequest
        {
            ToDistrictId = req.ToDistrictId,
            ToWardCode = req.ToWardCode,
            ServiceId = req.ServiceId,
            Weight = totalWeight,
            Length = req.Length ?? 20,
            Width = req.Width ?? 20,
            Height = req.Height ?? 20,
            InsuranceValue = (int)subtotal
        };

        var shippingResult = await _shippingService.CalculateShippingFeeAsync(shippingRequest);

        if (!shippingResult.Success)
        {
            throw new InvalidOperationException(
                $"Không thể tính phí vận chuyển: {shippingResult.ErrorMessage}"
            );
        }

        decimal shippingFee = shippingResult.ShippingFee;

        // 5️⃣ Áp dụng voucher nếu có
        Vouncher? voucher = null;
        decimal discount = 0m;

        if (!string.IsNullOrWhiteSpace(req.VoucherCode))
        {
            var code = req.VoucherCode.Trim();
            voucher = await _context.Vounchers.FirstOrDefaultAsync(v => v.Code == code, ct);

            if (voucher == null)
                throw new InvalidOperationException("Mã voucher không tồn tại.");

            if (!IsUsable(voucher, subtotal))
                throw new InvalidOperationException("Voucher không còn hiệu lực hoặc không đạt điều kiện.");

            discount = CalcDiscount(voucher, subtotal);
        }

        // 6️⃣ ===== TÍNH TỔNG CUỐI =====
        var finalAmount = subtotal + shippingFee - discount;
        if (finalAmount < 0) finalAmount = 0;

        // 7️⃣ Lưu Order
        using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                ShippingAddress = req.ShippingAddress,
                PaymentMethod = req.PaymentMethod,
                Status = OrderStatus.Pending,

                TotalAmount = subtotal,
                ShippingFee = shippingFee,
                DiscountAmount = discount,
                FinalAmount = finalAmount,

                ToProvinceId = req.ToProvinceId,
                ToProvinceName = req.ToProvinceName,
                ToDistrictId = req.ToDistrictId,
                ToDistrictName = req.ToDistrictName,
                ToWardCode = req.ToWardCode,
                ToWardName = req.ToWardName,

                ServiceId = shippingResult.ServiceId,
                ServiceType = shippingResult.ServiceType,
                Weight = totalWeight,
                Length = req.Length ?? 20,
                Width = req.Width ?? 20,
                Height = req.Height ?? 20,

                VoucherId = voucher?.Id,
                Voucher = voucher,
                VoucherCodeSnapshot = voucher?.Code
            };

            foreach (var item in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            cart.IsCheckedOut = true;

            if (voucher != null)
            {
                voucher.CurrentUsageCount += 1;
                voucher.UsedAt = DateTime.UtcNow;
                if (voucher.MaxUsageCount > 0 &&
                    voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                {
                    voucher.IsValid = false;
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new CheckoutOrderResponse
            {
                Message = "Đặt hàng thành công.",
                OrderId = order.Id,
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                Discount = discount,
                FinalAmount = finalAmount,
                PaymentMethod = req.PaymentMethod,
                VoucherCode = voucher?.Code,
                ServiceType = shippingResult.ServiceType,
                Weight = totalWeight
            };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }


    //public async Task<CheckoutRentalResponse> CheckoutRentalByDaysAsync(CheckoutRentalByDaysRequest req, CancellationToken ct = default)
    //    {
    //        if (req.Items == null || req.Items.Count == 0)
    //            throw new InvalidOperationException("Đơn thuê phải có ít nhất 1 sản phẩm.");

    //        var now = DateTime.UtcNow;

    //        await using var tx = await _context.Database.BeginTransactionAsync(ct);
    //        try
    //        {
    //            var rental = new Rental
    //            {
    //                UserId = req.UserId,
    //                Status = RentalStatus.Pending,
    //                StartDate = now,
    //                Items = new List<RentalItem>()
    //            };

    //            foreach (var x in req.Items)
    //            {
    //                var product = await _context.Products.FirstOrDefaultAsync(p => p.IdProduct == x.ProductId, ct);
    //                if (product == null)
    //                    throw new InvalidOperationException($"Sản phẩm Id={x.ProductId} không tồn tại.");

    //                if (product.Quantity <= 0)
    //                    throw new InvalidOperationException($"Sản phẩm '{product.Name}' đã hết hàng.");

    //                if (x.RentalDays <= 0)
    //                    throw new InvalidOperationException($"RentalDays phải > 0 (ProductId={x.ProductId}).");

    //                var pricePerDay = (x.PricePerDay.HasValue && x.PricePerDay.Value > 0) ? x.PricePerDay.Value : product.Price;

    //                rental.Items.Add(new RentalItem
    //                {
    //                    ProductId = x.ProductId,
    //                    RentalDays = x.RentalDays,
    //                    PricePerDay = pricePerDay,
    //                    SubTotal = pricePerDay * x.RentalDays
    //                });

    //                // Trừ tồn mỗi item = 1 đơn vị
    //                product.Quantity -= 1;
    //            }

    //            rental.TotalPrice = rental.Items.Sum(i => i.SubTotal);
    //            var maxDays = rental.Items.Max(i => i.RentalDays);
    //            rental.EndDate = rental.StartDate.AddDays(maxDays);

    //            _context.Rentals.Add(rental);
    //            await _context.SaveChangesAsync(ct);
    //            await tx.CommitAsync(ct);

    //            return new CheckoutRentalResponse
    //            {
    //                RentalId = rental.Id,
    //                RentalDays = maxDays,
    //                Message = "Tạo đơn thuê thành công"
    //            };
    //        }
    //        catch
    //        {
    //            await tx.RollbackAsync(ct);
    //            throw;
    //        }
    //    }

    //    public async Task<CheckoutRentalResponse> CheckoutRentalByDatesAsync(CheckoutRentalByDatesRequest req, CancellationToken ct = default)
    //    {
    //        if (req.Items == null || req.Items.Count == 0)
    //            throw new InvalidOperationException("Đơn thuê phải có ít nhất 1 sản phẩm.");

    //        // Bạn có thể dùng TZ Việt Nam nếu muốn validate theo ngày địa phương:
    //        // var tz = GetVietNamTz();
    //        // var startLocal = TimeZoneInfo.ConvertTimeFromUtc(req.StartDateUtc, tz).Date;
    //        // var endLocal = TimeZoneInfo.ConvertTimeFromUtc(req.EndDateUtc, tz).Date;

    //        var startUtc = DateTime.SpecifyKind(req.StartDateUtc, DateTimeKind.Utc);
    //        var endUtc = DateTime.SpecifyKind(req.EndDateUtc, DateTimeKind.Utc);

    //        if (endUtc <= startUtc)
    //            throw new InvalidOperationException("EndDateUtc phải sau StartDateUtc.");

    //        var rentalDays = (int)Math.Ceiling((endUtc - startUtc).TotalDays);
    //        if (rentalDays < 1) rentalDays = 1;

    //        await using var tx = await _context.Database.BeginTransactionAsync(ct);
    //        try
    //        {
    //            var rental = new Rental
    //            {
    //                UserId = req.UserId,
    //                Status = RentalStatus.Pending,
    //                StartDate = startUtc,
    //                EndDate = endUtc,
    //                Items = new List<RentalItem>()
    //            };

    //            foreach (var x in req.Items)
    //            {
    //                var product = await _context.Products.FirstOrDefaultAsync(p => p.IdProduct == x.ProductId, ct);
    //                if (product == null)
    //                    throw new InvalidOperationException($"Sản phẩm Id={x.ProductId} không tồn tại.");

    //                if (product.Quantity <= 0)
    //                    throw new InvalidOperationException($"Sản phẩm '{product.Name}' đã hết hàng.");

    //                var pricePerDay = (x.PricePerDay.HasValue && x.PricePerDay.Value > 0)
    //                                  ? x.PricePerDay.Value
    //                                  : product.Price;

    //                rental.Items.Add(new RentalItem
    //                {
    //                    ProductId = x.ProductId,
    //                    RentalDays = rentalDays,
    //                    PricePerDay = pricePerDay,
    //                    SubTotal = pricePerDay * rentalDays
    //                });

    //                product.Quantity -= 1;
    //            }

    //            rental.TotalPrice = rental.Items.Sum(i => i.SubTotal);

    //            _context.Rentals.Add(rental);
    //            await _context.SaveChangesAsync(ct);
    //            await tx.CommitAsync(ct);

    //            return new CheckoutRentalResponse
    //            {
    //                RentalId = rental.Id,
    //                RentalDays = rentalDays,
    //                Message = "Tạo đơn thuê thành công"
    //            };
    //        }
    //        catch
    //        {
    //            await tx.RollbackAsync(ct);
    //            throw;
    //        }
    //    }
    }


