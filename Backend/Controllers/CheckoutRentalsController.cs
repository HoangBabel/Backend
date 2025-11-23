using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/rental-checkout")]
    public class RentalCheckoutController : ControllerBase
    {
        private readonly IRentalCheckoutService _service;
        private readonly AppDbContext _context;

        public RentalCheckoutController(IRentalCheckoutService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out var uid))
                return uid;
            throw new UnauthorizedAccessException("Không tìm thấy userId trong token.");
        }

        /// <summary>
        /// Checkout đơn thuê
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CheckoutRental(
            [FromBody] CheckoutRentalRequest req,
            CancellationToken ct)
        {
            try
            {
                var userId = GetUserId();
                var res = await _service.CheckoutRentalAsync(userId, req, ct);

                return Ok(new
                {
                    message = "Checkout thành công",
                    data = new
                    {
                        res.RentalId,
                        res.Subtotal,
                        res.Deposit,
                        res.ShippingFee,
                        res.SubtotalDiscount,
                        res.ShippingDiscount,
                        res.Discount,
                        res.FinalAmount,
                        PaymentMethod = res.PaymentMethod.ToString(),
                        res.VoucherCode,
                        res.ServiceType,
                        res.Weight,
                        res.CheckoutUrl,
                        res.QrCode,
                        res.PaymentLinkId
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán của rental
        /// </summary>
        [HttpGet("{rentalId}/payment-status")]
        [Authorize]
        public async Task<IActionResult> GetPaymentStatus(int rentalId, CancellationToken ct)
        {
            try
            {
                var userId = GetUserId();

                var rental = await _context.Rentals
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == rentalId && r.UserId == userId, ct);

                if (rental == null)
                    return NotFound(new { message = "Không tìm thấy đơn thuê." });

                var payment = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.Type == PaymentType.Rental && p.RefId == rentalId)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync(ct);

                return Ok(new
                {
                    rentalId = rental.Id,
                    rentalStatus = rental.Status.ToString(),
                    payment = payment == null ? null : new
                    {
                        payment.Id,
                        payment.OrderCode,
                        status = payment.Status.ToString(),
                        payment.ExpectedAmount,
                        payment.PaidAt,
                        payment.CreatedAt
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Hủy payment link
        /// </summary>
        [HttpPost("{rentalId}/cancel-payment")]
        [Authorize]
        public async Task<IActionResult> CancelPayment(int rentalId, CancellationToken ct)
        {
            try
            {
                var userId = GetUserId();

                var payment = await _context.Payments
                    .Where(p => p.Type == PaymentType.Rental &&
                               p.RefId == rentalId &&
                               p.Status == PaymentStatus.Created)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync(ct);

                if (payment == null)
                    return NotFound(new { message = "Không tìm thấy payment link để hủy." });

                payment.Status = PaymentStatus.Cancelled;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);

                return Ok(new { message = "Đã hủy payment link thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
