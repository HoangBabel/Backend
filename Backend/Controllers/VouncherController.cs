using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Duan.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoucherController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VoucherController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/voucher
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vouncher>>> GetAll()
        {
            return await _context.Vounchers.ToListAsync();
        }

        // GET: api/voucher/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vouncher>> GetById(int id)
        {
            var voucher = await _context.Vounchers.FindAsync(id);
            if (voucher == null) return NotFound();
            return voucher;
        }

        // POST: api/voucher
        [HttpPost]
        public async Task<ActionResult<Vouncher>> Create(Vouncher voucher)
        {
            _context.Vounchers.Add(voucher);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = voucher.Id }, voucher);
        }

        // PUT: api/voucher/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Vouncher voucher)
        {
            if (id != voucher.Id) return BadRequest();

            _context.Entry(voucher).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/voucher/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vounchers.FindAsync(id);
            if (voucher == null) return NotFound();

            _context.Vounchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/voucher/apply?code=ABC123&amount=100000&shippingFee=20000
        [HttpPost("apply")]
        public async Task<ActionResult<object>> ApplyVoucher(string code, decimal amount, decimal shippingFee = 0)
        {
            var voucher = await _context.Vounchers.FirstOrDefaultAsync(v => v.Code == code && v.IsValid);
            if (voucher == null) return BadRequest(new { message = "Voucher không hợp lệ" });

            // Kiểm tra hạn
            if (voucher.ExpirationDate < DateTime.Now)
                return BadRequest(new { message = "Voucher đã hết hạn" });

            // Kiểm tra số lần sử dụng
            if (voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                return BadRequest(new { message = "Voucher đã hết lượt sử dụng" });

            // Kiểm tra giá trị tối thiểu
            if (amount < voucher.MinimumOrderValue)
                return BadRequest(new { message = $"Đơn hàng tối thiểu {voucher.MinimumOrderValue} mới dùng được" });

            decimal discount = 0;

            switch (voucher.Type)
            {
                case "Fixed":
                    discount = voucher.DiscountValue ?? 0;
                    break;

                case "Percent":
                    discount = (amount * (voucher.DiscountPercent ?? 0) / 100);
                    if (voucher.MaximumDiscount.HasValue && discount > voucher.MaximumDiscount.Value)
                        discount = voucher.MaximumDiscount.Value;
                    break;

                case "Shipping":
                    if (voucher.ApplyToShipping)
                        discount = shippingFee;
                    break;
            }

            // Tổng giảm không vượt quá tổng tiền
            if (discount > (amount + shippingFee))
                discount = amount + shippingFee;

            var finalPrice = (amount + shippingFee) - discount;

            // Cập nhật số lần sử dụng
            voucher.CurrentUsageCount++;
            if (voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                voucher.IsValid = false;
            voucher.UsedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                VoucherCode = voucher.Code,
                DiscountType = voucher.Type,
                OriginalPrice = amount,
                ShippingFee = shippingFee,
                Discount = discount,
                FinalPrice = finalPrice
            });
        }
    }
}