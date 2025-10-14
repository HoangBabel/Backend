using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // yêu cầu JWT
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;

    public CartController(AppDbContext db)
    {
        _db = db;
    }

    // DTOs
    public class AddCartItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQtyRequest
    {
        public int Quantity { get; set; }
    }

    // Helper: lấy UserId từ JWT
    private int GetUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new UnauthorizedAccessException("Missing user id claim");
        if (!int.TryParse(idStr, out var id)) throw new UnauthorizedAccessException("Invalid user id claim");
        return id;
    }

    // Helper: lấy (hoặc tạo) giỏ của user
    private async Task<Cart> GetOrCreateCartAsync(int userId, CancellationToken ct = default)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

        if (cart == null)
        {
            cart = new Cart { UserId = userId, IsCheckedOut = false, UpdatedAt = DateTime.UtcNow };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync(ct);
        }
        return cart;
    }

    /// GET /api/cart
    [HttpGet]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var userId = GetUserId();
        var cart = await _db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

        cart ??= new Cart { UserId = userId, Items = new List<CartItem>() };

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        return Ok(new
        {
            cart.Id,
            cart.UserId,
            Items = cart.Items.Select(i => new
            {
                i.Id,
                i.ProductId,
                ProductName = i.Product?.Name,
                i.Quantity,
                i.UnitPrice,
                LineTotal = i.UnitPrice * i.Quantity
            }),
            Subtotal = subtotal,
            UpdatedAt = cart.UpdatedAt
        });
    }

    /// POST /api/cart/items
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest req, CancellationToken ct)
    {
        if (req.Quantity <= 0) return BadRequest("Quantity must be > 0");

        var userId = GetUserId();
        var cart = await GetOrCreateCartAsync(userId, ct);

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProduct == req.ProductId, ct);
        if (product == null) return NotFound("Product not found");
        if (product.Status == ProductStatus.HetHang || product.Quantity <= 0)
            return BadRequest("Product is out of stock");

        // Nếu muốn chặn vượt tồn kho (tuỳ chính sách):
        if (req.Quantity > product.Quantity) return BadRequest("Not enough stock");

        // Đã có item? -> tăng số lượng
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == req.ProductId);
        if (existing != null)
        {
            var newQty = existing.Quantity + req.Quantity;
            if (newQty <= 0) return BadRequest("Quantity must be > 0");
            if (newQty > product.Quantity) return BadRequest("Not enough stock");
            existing.Quantity = newQty;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.IdProduct,
                Quantity = req.Quantity,
                UnitPrice = product.Price, // snapshot giá hiện tại
                AddedAt = DateTime.Now
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetCurrent(ct); // trả về giỏ mới nhất
    }

    /// PATCH /api/cart/items/{productId}
    [HttpPatch("items/{productId:int}")]
    public async Task<IActionResult> UpdateQuantity([FromRoute] int productId, [FromBody] UpdateQtyRequest req, CancellationToken ct)
    {
        if (req.Quantity <= 0) return BadRequest("Quantity must be > 0");

        var userId = GetUserId();
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

        if (cart == null) return NotFound("Cart not found");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return NotFound("Item not found");

        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.IdProduct == productId, ct);
        if (product == null) return NotFound("Product not found");
        if (req.Quantity > product.Quantity) return BadRequest("Not enough stock");

        item.Quantity = req.Quantity;
        cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetCurrent(ct);
    }

    /// DELETE /api/cart/items/{productId}
    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> RemoveItem([FromRoute] int productId, CancellationToken ct)
    {
        var userId = GetUserId();
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

        if (cart == null) return NotFound("Cart not found");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return NotFound("Item not found");

        _db.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetCurrent(ct);
    }

    /// DELETE /api/cart (xoá toàn bộ item)
    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        var userId = GetUserId();
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

        if (cart == null) return NoContent();

        _db.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
