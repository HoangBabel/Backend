using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Backend.DTOs.DailyRentalDtos;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DailyRentalsController : ControllerBase // ✅ ĐỔI THÀNH ControllerBase
    {
        private readonly IDailyRentalService _svc;
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;

        public DailyRentalsController(
            IDailyRentalService svc,
            AppDbContext db,
            IEmailService emailService)
        {
            _svc = svc;
            _db = db;
            _emailService = emailService;
        }

        public static int GetUserId(ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue("sub")
                     ?? user.FindFirstValue("uid");
            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedAccessException("Token không chứa user id.");
            return int.Parse(id);
        }

        // POST: /api/DailyRentals/quote
        [HttpPost("quote")]
        public async Task<ActionResult<QuoteDailyResponseDto>> Quote(
            [FromBody] QuoteDailyRequestDto dto)
        {
            try
            {
                var res = await _svc.GetDailyQuoteAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: /api/DailyRentals
        [HttpPost]
        public async Task<ActionResult<CreateDailyRentalResponseDto>> Create(
            [FromBody] CreateDailyRentalRequestDto dto)
        {
            try
            {
                var userId = GetUserId(User);
                var res = await _svc.CreateDailyRentalAsync(dto, userId);

                // ✅ GỬI EMAIL XÁC NHẬN (FIRE-AND-FORGET)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendRentalConfirmationEmailAsync(
                            res.RentalId,
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to send rental confirmation email: {ex.Message}");
                    }
                });

                return CreatedAtAction(nameof(GetById), new { id = res.RentalId }, res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: /api/DailyRentals/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Rental>> GetById(int id)
        {
            var rental = await _db.Rentals
                .Include(r => r.User)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            return rental is null ? NotFound() : Ok(rental);
        }

        // GET: /api/DailyRentals/user/{userId}
        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<List<Rental>>> GetUserRentals(int userId)
        {
            var rentals = await _db.Rentals
                .Include(r => r.User)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.StartDate)
                .ToListAsync();

            return Ok(rentals);
        }

        // POST: /api/DailyRentals/{id}/activate
        [HttpPost("{id:int}/activate")]
        public async Task<ActionResult> Activate(int id)
        {
            var rental = await _db.Rentals
                .Include(r => r.User)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental is null)
                return NotFound($"Rental #{id} not found");

            try
            {
                var oldStatus = rental.Status;
                rental.Activate();
                await _db.SaveChangesAsync();

                // ✅ GỬI EMAIL KÍCH HOẠT (CHỈ KHI THAY ĐỔI TRẠNG THÁI)
                if (oldStatus != RentalStatus.Active)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendRentalStatusUpdateEmailAsync(
                                id,
                                RentalStatus.Active,
                                CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Failed to send activation email: {ex.Message}");
                        }
                    });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: /api/DailyRentals/{id}/settle
        [HttpPost("{id:int}/settle")]
        public async Task<ActionResult<object>> Settle(
            int id,
            [FromBody] SettleRentalRequestDto dto)
        {
            var rental = await _db.Rentals
                .Include(r => r.User)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental is null)
                return NotFound($"Rental #{id} not found");

            try
            {
                // ✅ TÍNH PHÍ TRỄ HẠN
                var lateFee = Backend.Helpers.RentalSettlementHelper
                    .ComputeTotalLateFee(rental, dto.ReturnedAt);

                // ✅ CẬP NHẬT QUYẾT TOÁN
                rental.SetSettlement(
                    returnedAt: dto.ReturnedAt,
                    lateFee: lateFee,
                    cleaningFee: dto.CleaningFee,
                    damageFee: dto.DamageFee
                );

                await _db.SaveChangesAsync();

                // ✅ TÍNH SỐ NGÀY TRỄ
                var lateDays = Backend.Helpers.RentalSettlementHelper
                    .ComputeLateDays(rental.EndDate, dto.ReturnedAt);

                // ✅ GỬI EMAIL QUYẾT TOÁN (FIRE-AND-FORGET)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendRentalSettlementEmailAsync(
                            rentalId: id,
                            lateDays: lateDays,
                            lateFee: rental.LateFee,
                            cleaningFee: rental.CleaningFee,
                            damageFee: rental.DamageFee,
                            depositPaid: rental.DepositPaid,
                            depositRefund: rental.DepositRefund,
                            ct: CancellationToken.None
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to send settlement email: {ex.Message}");
                    }
                });

                // ✅ TRẢ VỀ KẾT QUẢ QUYẾT TOÁN
                return Ok(new
                {
                    RentalId = rental.Id,
                    Status = rental.Status.ToString(),
                    ReturnedAt = rental.ReturnedAt,
                    LateDays = lateDays,
                    LateFee = rental.LateFee,
                    CleaningFee = rental.CleaningFee,
                    DamageFee = rental.DamageFee,
                    TotalDeductions = rental.LateFee + rental.CleaningFee + rental.DamageFee,
                    DepositPaid = rental.DepositPaid,
                    DepositRefund = rental.DepositRefund,
                    TotalPrice = rental.TotalPrice,
                    NetCollectOrRefund = -rental.DepositRefund,
                    Message = rental.DepositRefund > 0
                        ? $"Hoàn lại {rental.DepositRefund:N0}đ vào tài khoản trong 3-5 ngày"
                        : "Tiền cọc đã được khấu trừ hoàn toàn"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // DTO cho Settle
        public record SettleRentalRequestDto(
            DateTime ReturnedAt,
            decimal CleaningFee,
            decimal DamageFee);
    }
}
