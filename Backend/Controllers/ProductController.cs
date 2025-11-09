using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ✅ Lấy danh sách sản phẩm (có tìm kiếm + lọc danh mục)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(string? search, int? categoryId)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            return await query.ToListAsync();
        }


        // ✅ Lấy sản phẩm theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.IdProduct == id);

            if (product == null) return NotFound();
            return product;
        }

        // ✅ Thêm sản phẩm có upload hình ảnh
        [HttpPost("post")]
        public async Task<ActionResult<Product>> PostProductWithImage([FromForm] ProductUploadRequest request)
        {
            if (request == null)
                return BadRequest("Dữ liệu không hợp lệ.");

            var product = new Product
            {
                CategoryId = request.CategoryId,
                Name = request.Name,
                Price = request.Price,
                Quantity = request.Quantity,
                Description = request.Description,
                Status = request.Status,
                IsRental = request.IsRental,     
                Condition = request.Condition

            };

            // ✅ Xử lý upload ảnh
            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(request.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(ext))
                    return BadRequest("Chỉ cho phép các định dạng ảnh: .jpg, .jpeg, .png, .webp");

                if (request.ImageFile.Length > 5 * 1024 * 1024)
                    return BadRequest("Kích thước ảnh không được vượt quá 5MB.");

                var uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ImageFile.CopyToAsync(stream);
                }

                product.Image = $"images/{fileName}";
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.IdProduct }, product);
        }

        // ✅ Cập nhật sản phẩm (có thể cập nhật ảnh)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, [FromForm] ProductUploadRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = request.Name;
            product.CategoryId = request.CategoryId;
            product.Price = request.Price;
            product.Quantity = request.Quantity;
            product.Description = request.Description;
            product.Status = request.Status;

            // Nếu có ảnh mới thì thay thế ảnh cũ
            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(request.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(ext))
                    return BadRequest("Chỉ cho phép các định dạng ảnh: .jpg, .jpeg, .png, .webp");

                var uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(product.Image))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, product.Image);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ImageFile.CopyToAsync(stream);
                }

                product.Image = $"images/{fileName}";
            }

            await _context.SaveChangesAsync();

            // Trả lại sản phẩm đã cập nhật kèm Category
            await _context.Entry(product).Reference(p => p.Category).LoadAsync();

            return NoContent();
        }

        // ✅ Xóa sản phẩm
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (!string.IsNullOrEmpty(product.Image))
            {
                var path = Path.Combine(_env.WebRootPath, product.Image);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // 📦 Class trung gian để nhận dữ liệu từ form (khi upload ảnh)
    public class ProductUploadRequest
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; }
        public ProductStatus Status { get; set; }
        public IFormFile? ImageFile { get; set; }
        public bool IsRental { get; set; } = false;
        public ProductCondition Condition { get; set; } = ProductCondition.New;
    }
}
