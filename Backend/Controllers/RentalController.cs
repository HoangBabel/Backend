using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Backend.Helpers.DateTimeHelper;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RentalController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RentalController(AppDbContext context)
        {
            _context = context;
        }

        //[HttpPost("create")]
        //public async Task<IActionResult> CreateRental([FromBody] Rental rental)
        //{
        //    // Tính tổng tiền thuê
        //    foreach (var item in rental.Items)
        //    {
        //        item.SubTotal = item.PricePerDay * item.RentalDays;
        //    }
        //    rental.TotalPrice = rental.Items.Sum(i => i.SubTotal);
        //    rental.Status = RentalStatus.Pending;
        //    rental.StartDate = DateTime.Now;
        //    rental.EndDate = rental.StartDate.AddDays(rental.Items.Max(i => i.RentalDays));

        //    _context.Rentals.Add(rental);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Tạo đơn thuê thành công", rental });
        //}
        [HttpPost("create")]
        public async Task<IActionResult> CreateRental([FromBody] Rental rental)
        {
            if (rental == null || rental.Items == null || rental.Items.Count == 0)
                return BadRequest("Đơn thuê phải có ít nhất 1 sản phẩm.");

            // Gợi ý: đồng nhất dùng UTC
            var now = DateTime.UtcNow;

            // Nếu client gửi StartDate/EndDate thì bỏ qua và chuẩn hóa lại tại server
            rental.Status = RentalStatus.Pending;
            rental.StartDate = now;

            // Transaction để đảm bảo trừ tồn & tạo đơn là nguyên tử
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // Kiểm tra từng item
                foreach (var item in rental.Items)
                {
                    // Tìm product
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.IdProduct == item.ProductId);

                    if (product == null)
                        return BadRequest($"Sản phẩm (Id={item.ProductId}) không tồn tại.");

                    if (product.Quantity <= 0)
                        return BadRequest($"Sản phẩm '{product.Name}' đã hết hàng.");

                    if (item.RentalDays <= 0)
                        return BadRequest($"RentalDays phải > 0 (ProductId={item.ProductId}).");

                    // Nếu client không gửi PricePerDay hoặc gửi 0, mặc định lấy từ Product.Price
                    if (item.PricePerDay <= 0)
                    {
                        item.PricePerDay = product.Price;
                    }

                    // Tính tiền dòng
                    item.SubTotal = item.PricePerDay * item.RentalDays;

                    // Trừ tồn (mỗi item tương đương thuê 1 đơn vị)
                    product.Quantity -= 1;

                    // Gán lại snapshot tên SP nếu muốn lưu (tuỳ chọn)
                    // -> Bạn đã có Product navigation; nếu cần tên snapshot:
                    // item.ProductName = product.Name;  (cần thêm field nếu muốn)
                }

                // Tổng tiền & ngày kết thúc
                rental.TotalPrice = rental.Items.Sum(i => i.SubTotal);
                var maxDays = rental.Items.Max(i => i.RentalDays);
                rental.EndDate = rental.StartDate.AddDays(maxDays);

                // Lưu DB
                _context.Rentals.Add(rental);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return Ok(new
                {
                    message = "Tạo đơn thuê thành công",
                    rental
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                // Log ex...
                return StatusCode(StatusCodes.Status500InternalServerError, "Có lỗi xảy ra khi tạo đơn thuê.");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserRentals(int userId)
        {
            var rentals = await _context.Rentals
                .Include(r => r.Items)
                .ThenInclude(i => i.Product)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return Ok(rentals);
        }

        [HttpPut("complete/{id}")]
        public async Task<IActionResult> CompleteRental(int id)
        {
            var rental = await _context.Rentals.FindAsync(id);
            if (rental == null) return NotFound();

            rental.Status = RentalStatus.Completed;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đơn thuê đã hoàn tất" });
        }

        [HttpPost("create-by-dates")]
        public async Task<IActionResult> CreateRentalByDates([FromBody] CreateRentalByDatesDto dto)
        {

            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest("Đơn thuê phải có ít nhất 1 sản phẩm.");

            var tz = GetVietNamTz();

            // Chuyển sang giờ VN và lấy phần ngày
            var startLocal = LocalDateOnly(ToLocal(dto.StartDate, tz));
            var endLocal = LocalDateOnly(ToLocal(dto.EndDate, tz));

            var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;

            // Validate ngày
            if (startLocal < todayLocal)
                return BadRequest("Ngày bắt đầu thuê không được trước ngày hôm nay.");

            if (endLocal <= startLocal)
                return BadRequest("Ngày kết thúc phải sau ngày bắt đầu.");

            // End exclusive -> số ngày thuê
            var rentalDays = (int)(endLocal - startLocal).TotalDays;
            if (rentalDays < 1) rentalDays = 1; // an toàn

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var rental = new Rental
                {
                    UserId = dto.UserId,
                    Status = RentalStatus.Pending,
                    // Lưu UTC vào DB để nhất quán
                    StartDate = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz),
                    EndDate = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz),
                    Items = new List<RentalItem>()
                };

                foreach (var x in dto.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.IdProduct == x.ProductId);
                    if (product == null)
                        return BadRequest($"Sản phẩm Id={x.ProductId} không tồn tại.");

                    if (product.Quantity <= 0)
                        return BadRequest($"Sản phẩm '{product.Name}' đã hết hàng.");

                    var pricePerDay = (x.PricePerDay.HasValue && x.PricePerDay.Value > 0)
                                      ? x.PricePerDay.Value
                                      : product.Price; // hoặc Product.RentPricePerDay nếu bạn tách riêng

                    var item = new RentalItem
                    {
                        ProductId = x.ProductId,
                        RentalDays = rentalDays,                  // <-- TỰ ĐỘNG GÁN
                        PricePerDay = pricePerDay,
                        SubTotal = pricePerDay * rentalDays
                    };

                    rental.Items.Add(item);

                    // Trừ tồn (mỗi item tương ứng 1 đơn vị)
                    product.Quantity -= 1;
                }

                rental.TotalPrice = rental.Items.Sum(i => i.SubTotal);

                _context.Rentals.Add(rental);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { message = "Tạo đơn thuê thành công", rentalId = rental.Id, rentalDays });
            }
            catch
            {
                await tx.RollbackAsync();
                return StatusCode(500, "Có lỗi khi tạo đơn thuê.");
            }
        }
    }
}
