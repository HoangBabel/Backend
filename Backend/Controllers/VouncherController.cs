using Backend.Data;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoucherController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VoucherController> _logger;

        public VoucherController(AppDbContext context, ILogger<VoucherController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/voucher - Lấy danh sách voucher còn hạn
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vouncher>>> GetAll()
        {
            try
            {
                var vouchers = await _context.Vounchers
                    .Where(v => v.IsValid && v.ExpirationDate > DateTime.UtcNow)
                    .OrderByDescending(v => v.ExpirationDate)
                    .ToListAsync();

                return Ok(vouchers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vouchers");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách voucher" });
            }
        }

        /// <summary>
        /// GET: api/voucher/5 - Lấy chi tiết voucher
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Vouncher>> GetById(int id)
        {
            try
            {
                var voucher = await _context.Vounchers.FindAsync(id);
                if (voucher == null)
                    return NotFound(new { message = "Voucher không tồn tại" });

                return Ok(voucher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voucher {VoucherId}", id);
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin voucher" });
            }
        }

        /// <summary>
        /// POST: api/voucher - Tạo voucher mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Vouncher>> Create(CreateVoucherDto dto)
        {
            try
            {
                // ✅ Validation type
                if (!VoucherValidator.IsValidVoucherType(dto.Type))
                    return BadRequest(new { message = "Type phải là: Fixed, Percent, hoặc Shipping" });

                // ✅ Validation theo type
                var validationError = ValidateCreateDto(dto);
                if (!string.IsNullOrEmpty(validationError))
                    return BadRequest(new { message = validationError });

                // Check code trùng
                if (await _context.Vounchers.AnyAsync(v => v.Code == dto.Code))
                    return BadRequest(new { message = "Mã voucher đã tồn tại" });

                var voucher = new Vouncher
                {
                    Code = dto.Code,
                    Type = dto.Type,
                    DiscountValue = dto.DiscountValue,
                    DiscountPercent = dto.DiscountPercent,
                    MaximumDiscount = dto.MaximumDiscount,
                    MinimumOrderValue = dto.MinimumOrderValue,
                    ApplyToShipping = dto.Type.ToLowerInvariant() == "shipping",
                    ShippingDiscountPercent = dto.ShippingDiscountPercent,
                    ExpirationDate = dto.ExpirationDate,
                    IsValid = true,
                    MaxUsageCount = dto.MaxUsageCount,
                    CurrentUsageCount = 0
                };

                _context.Vounchers.Add(voucher);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created voucher {VoucherCode} with ID {VoucherId}", voucher.Code, voucher.Id);

                return CreatedAtAction(nameof(GetById), new { id = voucher.Id }, voucher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher");
                return StatusCode(500, new { message = "Lỗi khi tạo voucher" });
            }
        }

        /// <summary>
        /// PUT: api/voucher/5 - Cập nhật voucher
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateVoucherDto dto)
        {
            try
            {
                var voucher = await _context.Vounchers.FindAsync(id);
                if (voucher == null)
                    return NotFound(new { message = "Voucher không tồn tại" });

                // Validation type
                if (dto.Type != null && !VoucherValidator.IsValidVoucherType(dto.Type))
                    return BadRequest(new { message = "Type phải là: Fixed, Percent, hoặc Shipping" });

                // Update fields
                if (dto.Code != null) voucher.Code = dto.Code;
                if (dto.Type != null)
                {
                    voucher.Type = dto.Type;
                    voucher.ApplyToShipping = dto.Type.ToLowerInvariant() == "shipping";
                }
                if (dto.DiscountValue.HasValue) voucher.DiscountValue = dto.DiscountValue;
                if (dto.DiscountPercent.HasValue) voucher.DiscountPercent = dto.DiscountPercent;
                if (dto.MaximumDiscount.HasValue) voucher.MaximumDiscount = dto.MaximumDiscount;
                if (dto.MinimumOrderValue.HasValue) voucher.MinimumOrderValue = dto.MinimumOrderValue.Value;
                if (dto.ShippingDiscountPercent.HasValue) voucher.ShippingDiscountPercent = dto.ShippingDiscountPercent;
                if (dto.ExpirationDate.HasValue) voucher.ExpirationDate = dto.ExpirationDate.Value;
                if (dto.IsValid.HasValue) voucher.IsValid = dto.IsValid.Value;
                if (dto.MaxUsageCount.HasValue) voucher.MaxUsageCount = dto.MaxUsageCount.Value;

                // ✅ Validate sau khi update
                if (!VoucherValidator.ValidateVoucherFields(voucher, out string fieldError))
                    return BadRequest(new { message = fieldError });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated voucher {VoucherId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating voucher {VoucherId}", id);
                return StatusCode(500, new { message = "Lỗi khi cập nhật voucher" });
            }
        }

        /// <summary>
        /// DELETE: api/voucher/5 - Xóa voucher
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var voucher = await _context.Vounchers.FindAsync(id);
                if (voucher == null)
                    return NotFound(new { message = "Voucher không tồn tại" });

                _context.Vounchers.Remove(voucher);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted voucher {VoucherId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting voucher {VoucherId}", id);
                return StatusCode(500, new { message = "Lỗi khi xóa voucher" });
            }
        }

        /// <summary>
        /// POST: api/voucher/validate - Validate voucher (preview)
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<VoucherValidationResponse>> ValidateVoucher([FromBody] ValidateVoucherRequest request)
        {
            try
            {
                var voucher = await _context.Vounchers
                    .FirstOrDefaultAsync(v => v.Code == request.Code && v.IsValid);

                // ✅ FIX: Check null trước khi truyền vào helper
                if (voucher == null)
                {
                    return BadRequest(new { message = "Voucher không tồn tại hoặc đã bị vô hiệu hóa" });
                }

                // ✅ Dùng helper validate (voucher đã chắc chắn not null)
                if (!VoucherValidator.IsUsable(voucher, request.SubtotalAmount, out string errorMessage))
                {
                    return BadRequest(new { message = errorMessage });
                }

                // ✅ FIX: Dùng Calculate() thay vì CalcDiscount()
                var result = VoucherCalculator.Calculate(voucher, request.SubtotalAmount, request.ShippingFee);

                return Ok(new VoucherValidationResponse
                {
                    IsValid = true,
                    VoucherId = voucher.Id,
                    VoucherCode = voucher.Code,
                    VoucherType = voucher.Type ?? "Unknown",
                    SubtotalAmount = request.SubtotalAmount,
                    ShippingFee = request.ShippingFee,
                    SubtotalDiscount = result.SubtotalDiscount,
                    ShippingDiscount = result.ShippingDiscount,
                    TotalDiscount = result.TotalDiscount,
                    FinalAmount = result.FinalAmount,
                    RemainingUsage = VoucherValidator.GetRemainingUsage(voucher),
                    Message = "Voucher hợp lệ và đã được áp dụng"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating voucher {VoucherCode}", request.Code);
                return StatusCode(500, new { message = "Lỗi khi validate voucher" });
            }
        }

        /// <summary>
        /// POST: api/voucher/apply - Apply voucher (checkout)
        /// </summary>
        [HttpPost("apply")]
        public async Task<ActionResult<VoucherApplicationResponse>> ApplyVoucher([FromBody] ApplyVoucherRequest request)
        {
            // ✅ Dùng transaction để đảm bảo consistency
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var voucher = await _context.Vounchers
                    .FirstOrDefaultAsync(v => v.Code == request.Code && v.IsValid);

                // ✅ FIX: Check null trước khi truyền vào helper
                if (voucher == null)
                {
                    return BadRequest(new { message = "Voucher không tồn tại hoặc đã bị vô hiệu hóa" });
                }

                // ✅ Dùng helper validate (voucher đã chắc chắn not null)
                if (!VoucherValidator.IsUsable(voucher, request.SubtotalAmount, out string errorMessage))
                {
                    return BadRequest(new { message = errorMessage });
                }

                // ✅ FIX: Dùng Calculate() thay vì CalcDiscount()
                var result = VoucherCalculator.Calculate(voucher, request.SubtotalAmount, request.ShippingFee);

                // ✅ Cập nhật usage
                voucher.CurrentUsageCount++;
                if (voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                {
                    voucher.IsValid = false;
                }
                voucher.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Applied voucher {VoucherCode} (ID: {VoucherId}). Usage: {Current}/{Max}",
                    voucher.Code, voucher.Id, voucher.CurrentUsageCount, voucher.MaxUsageCount
                );

                return Ok(new VoucherApplicationResponse
                {
                    Success = true,
                    VoucherId = voucher.Id,
                    VoucherCode = voucher.Code,
                    VoucherType = voucher.Type ?? "Unknown",
                    SubtotalAmount = request.SubtotalAmount,
                    ShippingFee = request.ShippingFee,
                    SubtotalDiscount = result.SubtotalDiscount,
                    ShippingDiscount = result.ShippingDiscount,
                    TotalDiscount = result.TotalDiscount,
                    FinalAmount = result.FinalAmount,
                    RemainingUsage = VoucherValidator.GetRemainingUsage(voucher),
                    Message = "Voucher đã được áp dụng thành công"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error applying voucher {VoucherCode}", request.Code);
                return StatusCode(500, new { message = "Lỗi khi áp dụng voucher" });
            }
        }

        /// <summary>
        /// ✅ PRIVATE HELPER: Validate CreateVoucherDto
        /// </summary>
        private string? ValidateCreateDto(CreateVoucherDto dto)
        {
            var type = dto.Type.ToLowerInvariant();

            switch (type)
            {
                case "fixed":
                    if (!dto.DiscountValue.HasValue || dto.DiscountValue.Value <= 0)
                        return "Voucher Fixed phải có DiscountValue > 0";
                    break;

                case "percent":
                    if (!dto.DiscountPercent.HasValue || dto.DiscountPercent.Value <= 0)
                        return "Voucher Percent phải có DiscountPercent > 0";
                    if (dto.DiscountPercent.Value > 100)
                        return "DiscountPercent không được vượt quá 100%";
                    break;

                case "shipping":
                    // ✅ ShippingDiscountPercent có thể null (= 100% freeship)
                    if (dto.ShippingDiscountPercent.HasValue)
                    {
                        if (dto.ShippingDiscountPercent.Value < 0 || dto.ShippingDiscountPercent.Value > 100)
                            return "ShippingDiscountPercent phải từ 0-100% (hoặc null cho miễn phí 100%)";
                    }
                    break;

                default:
                    return $"Loại voucher '{dto.Type}' không hợp lệ";
            }

            return null;
        }
    }
}
