using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models.Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ReviewsController> _logger;
        private readonly IAntiSpamService _antiSpam;


        public ReviewsController(AppDbContext db, ILogger<ReviewsController> logger, IAntiSpamService antiSpam)
        {
            _db = db;
            _logger = logger;
            _antiSpam = antiSpam;
        }

        // Lấy tất cả review của một sản phẩm
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetProductReviews(int productId)
        {
            try
            {
                // ⭐ Sửa: Sử dụng IdProduct thay vì Id
                var productExists = await _db.Products.AnyAsync(p => p.IdProduct == productId);
                if (!productExists)
                {
                    return NotFound(new { error = "Sản phẩm không tồn tại" });
                }

                var reviews = await _db.Reviews
                    .Where(r => r.ProductId == productId && r.ParentReviewId == null)
                    .Include(r => r.User)
                    .Include(r => r.Replies)
                        .ThenInclude(reply => reply.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(); // ⭐ Lấy data trước, sau đó map

                // ⭐ Map sau khi lấy data để tránh lỗi expression tree
                var reviewDtos = reviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User.FullName,
                    UserAvatar = r.User.AvatarUrl,
                    ProductId = r.ProductId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    LikeCount = r.LikeCount,
                    ImageUrls = !string.IsNullOrEmpty(r.ImageUrls)
                        ? r.ImageUrls.Split(',').ToList()
                        : new List<string>(),
                    Replies = r.Replies.Select(reply => new ReviewDto
                    {
                        Id = reply.Id,
                        UserId = reply.UserId,
                        UserName = reply.User.FullName,
                        UserAvatar = reply.User.AvatarUrl,
                        Comment = reply.Comment,
                        CreatedAt = reply.CreatedAt,
                        UpdatedAt = reply.UpdatedAt
                    }).ToList()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = reviewDtos,
                    total = reviewDtos.Count,
                    averageRating = reviewDtos.Any() ? reviewDtos.Average(r => r.Rating) : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách review cho sản phẩm {ProductId}", productId);
                return StatusCode(500, new { error = "Lỗi khi lấy danh sách đánh giá.", detail = ex.Message });
            }
        }

        // Tạo review mới
        [HttpPost]
        public async Task<ActionResult<ReviewDto>> CreateReview(
            [FromBody] CreateReviewDto? dto,
            [FromQuery] int? devUserId)
        {
            if (dto is null)
            {
                return BadRequest(new { error = "Body JSON rỗng hoặc sai Content-Type: application/json." });
            }

            // Lấy UserId
            int userId;
            var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out var uid))
            {
                userId = uid;
            }
            else if (devUserId.HasValue)
            {
                userId = devUserId.Value;
            }
            else
            {
                return Unauthorized(new { error = "Thiếu token hoặc devUserId để test." });
            }

            try
            {
                // ⭐ 1. Kiểm tra user có bị ban không
                if (_antiSpam.IsUserBanned(userId))
                {
                    var banTime = _antiSpam.GetBanRemainingTime(userId);
                    return StatusCode(403, new
                    {
                        error = $"Bạn đã bị cấm đánh giá do vi phạm quy định. Thời gian còn lại: {banTime?.Minutes} phút {banTime?.Seconds} giây.",
                        code = "USER_BANNED",
                        remainingSeconds = (int?)banTime?.TotalSeconds,
                        bannedUntil = DateTime.UtcNow.Add(banTime ?? TimeSpan.Zero)
                    });
                }

                // ⭐ 2. Kiểm tra từ thô tục TRƯỚC KHI lưu
                if (_antiSpam.ContainsProfanity(dto.Comment))
                {
                    // Ban user 15 phút
                    _antiSpam.BanUser(userId, TimeSpan.FromMinutes(15));

                    _logger.LogWarning("User {UserId} đã sử dụng từ thô tục và bị ban 15 phút. Nội dung: {Content}",
                        userId, dto.Comment);

                    return StatusCode(403, new
                    {
                        error = "Nội dung đánh giá chứa từ ngữ không phù hợp. Bạn đã bị cấm đánh giá trong 15 phút.",
                        code = "PROFANITY_DETECTED",
                        bannedDuration = 15,
                        bannedUntil = DateTime.UtcNow.AddMinutes(15)
                    });
                }

                // ⭐ 3. Kiểm tra spam chung (rate limiting)
                if (await _antiSpam.IsSpamming(userId))
                {
                    var banTime = _antiSpam.GetBanRemainingTime(userId);

                    if (banTime.HasValue)
                    {
                        return StatusCode(429, new
                        {
                            error = $"Bạn đang đánh giá quá nhanh và đã bị cấm tạm thời. Thời gian còn lại: {banTime?.Minutes} phút {banTime?.Seconds} giây.",
                            code = "TOO_MANY_REVIEWS_BANNED",
                            remainingSeconds = (int?)banTime?.TotalSeconds
                        });
                    }

                    return StatusCode(429, new
                    {
                        error = "Bạn đang đánh giá quá nhanh. Vui lòng thử lại sau.",
                        code = "TOO_MANY_REVIEWS"
                    });
                }

                // ⭐ 4. Kiểm tra cooldown cho sản phẩm cụ thể
                if (!await _antiSpam.CanUserReview(userId, dto.ProductId))
                {
                    var remainingTime = await _antiSpam.GetRemainingCooldown(userId, dto.ProductId);
                    return StatusCode(429, new
                    {
                        error = $"Vui lòng đợi {remainingTime?.Minutes} phút {remainingTime?.Seconds} giây trước khi đánh giá lại sản phẩm này.",
                        code = "REVIEW_COOLDOWN",
                        remainingSeconds = (int?)remainingTime?.TotalSeconds
                    });
                }

                // Kiểm tra sản phẩm tồn tại
                var productExists = await _db.Products.AnyAsync(p => p.IdProduct == dto.ProductId);
                if (!productExists)
                {
                    return NotFound(new { error = "Sản phẩm không tồn tại" });
                }

                // Kiểm tra user đã review sản phẩm này chưa
                var existingReview = await _db.Reviews
                    .AnyAsync(r => r.UserId == userId
                               && r.ProductId == dto.ProductId
                               && r.ParentReviewId == null);

                if (existingReview)
                {
                    return BadRequest(new { error = "Bạn đã đánh giá sản phẩm này rồi" });
                }

                // Tạo review mới
                var review = new Review
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    ImageUrls = dto.ImageUrls != null && dto.ImageUrls.Any()
                        ? string.Join(",", dto.ImageUrls)
                        : null,
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = true
                };

                _db.Reviews.Add(review);
                await _db.SaveChangesAsync();

                // ⭐ 5. Ghi nhận review thành công
                ((AntiSpamService)_antiSpam).RecordSuccessfulReview(userId, dto.ProductId);

                // Load lại review với thông tin user
                var createdReview = await _db.Reviews
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == review.Id);

                if (createdReview == null)
                {
                    return StatusCode(500, new { error = "Không thể tải lại review sau khi tạo" });
                }

                var reviewDto = new ReviewDto
                {
                    Id = createdReview.Id,
                    UserId = createdReview.UserId,
                    UserName = createdReview.User.FullName,
                    UserAvatar = createdReview.User.AvatarUrl,
                    ProductId = createdReview.ProductId,
                    Rating = createdReview.Rating,
                    Comment = createdReview.Comment,
                    CreatedAt = createdReview.CreatedAt,
                    ImageUrls = dto.ImageUrls ?? new List<string>()
                };

                return Ok(new
                {
                    success = true,
                    message = "Đánh giá thành công",
                    data = reviewDto
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo review");
                return StatusCode(500, new { error = "Lỗi khi tạo đánh giá.", detail = ex.Message });
            }
        }

        // ⭐ Cập nhật review với kiểm tra từ thô tục
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(
            int id,
            [FromBody] CreateReviewDto? dto,
            [FromQuery] int? devUserId)
        {
            if (dto is null)
            {
                return BadRequest(new { error = "Body JSON rỗng hoặc sai Content-Type: application/json." });
            }

            // Lấy UserId
            int userId;
            var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out var uid))
            {
                userId = uid;
            }
            else if (devUserId.HasValue)
            {
                userId = devUserId.Value;
            }
            else
            {
                return Unauthorized(new { error = "Thiếu token hoặc devUserId để test." });
            }

            try
            {
                // ⭐ Kiểm tra từ thô tục
                if (_antiSpam.ContainsProfanity(dto.Comment))
                {
                    _antiSpam.BanUser(userId, TimeSpan.FromMinutes(15));

                    _logger.LogWarning("User {UserId} đã sử dụng từ thô tục khi cập nhật review và bị ban 15 phút", userId);

                    return StatusCode(403, new
                    {
                        error = "Nội dung đánh giá chứa từ ngữ không phù hợp. Bạn đã bị cấm đánh giá trong 15 phút.",
                        code = "PROFANITY_DETECTED",
                        bannedDuration = 15
                    });
                }

                var review = await _db.Reviews.FindAsync(id);

                if (review == null)
                {
                    return NotFound(new { error = "Không tìm thấy đánh giá" });
                }

                if (review.UserId != userId)
                {
                    return Forbid();
                }

                // Không cho phép sửa sau 7 ngày
                if ((DateTime.UtcNow - review.CreatedAt).TotalDays > 7)
                {
                    return BadRequest(new { error = "Không thể chỉnh sửa đánh giá sau 7 ngày" });
                }

                review.Rating = dto.Rating;
                review.Comment = dto.Comment;
                review.ImageUrls = dto.ImageUrls != null && dto.ImageUrls.Any()
                    ? string.Join(",", dto.ImageUrls)
                    : null;
                review.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật đánh giá thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật review {ReviewId}", id);
                return StatusCode(500, new { error = "Lỗi khi cập nhật đánh giá.", detail = ex.Message });
            }
        }

        // ⭐ Reply với kiểm tra từ thô tục
        [HttpPost("{reviewId}/reply")]
        public async Task<ActionResult<ReviewDto>> ReplyReview(
            int reviewId,
            [FromBody] ReplyReviewDto? dto,
            [FromQuery] int? devUserId)
        {
            if (dto is null)
            {
                return BadRequest(new { error = "Body JSON rỗng hoặc sai Content-Type: application/json." });
            }

            // Lấy UserId
            int userId;
            var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out var uid))
            {
                userId = uid;
            }
            else if (devUserId.HasValue)
            {
                userId = devUserId.Value;
            }
            else
            {
                return Unauthorized(new { error = "Thiếu token hoặc devUserId để test." });
            }

            try
            {
                // ⭐ Kiểm tra từ thô tục
                if (_antiSpam.ContainsProfanity(dto.Comment))
                {
                    _antiSpam.BanUser(userId, TimeSpan.FromMinutes(15));

                    _logger.LogWarning("User {UserId} đã sử dụng từ thô tục khi reply và bị ban 15 phút", userId);

                    return StatusCode(403, new
                    {
                        error = "Nội dung trả lời chứa từ ngữ không phù hợp. Bạn đã bị cấm đánh giá trong 15 phút.",
                        code = "PROFANITY_DETECTED",
                        bannedDuration = 15
                    });
                }

                // Kiểm tra spam
                if (await _antiSpam.IsSpamming(userId))
                {
                    var banTime = _antiSpam.GetBanRemainingTime(userId);

                    if (banTime.HasValue)
                    {
                        return StatusCode(429, new
                        {
                            error = $"Bạn đang trả lời quá nhanh và đã bị cấm tạm thời. Thời gian còn lại: {banTime?.Minutes} phút {banTime?.Seconds} giây.",
                            code = "TOO_MANY_REPLIES_BANNED",
                            remainingSeconds = (int?)banTime?.TotalSeconds
                        });
                    }

                    return StatusCode(429, new
                    {
                        error = "Bạn đang trả lời quá nhanh. Vui lòng thử lại sau.",
                        code = "TOO_MANY_REPLIES"
                    });
                }

                var parentReview = await _db.Reviews.FindAsync(reviewId);
                if (parentReview == null)
                {
                    return NotFound(new { error = "Không tìm thấy đánh giá" });
                }

                var reply = new Review
                {
                    UserId = userId,
                    ProductId = parentReview.ProductId,
                    Rating = 0,
                    Comment = dto.Comment,
                    ParentReviewId = reviewId,
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = true
                };

                _db.Reviews.Add(reply);
                await _db.SaveChangesAsync();

                // Ghi nhận reply
                _antiSpam.RecordReviewAttempt(userId);

                // Load lại reply với thông tin user
                var createdReply = await _db.Reviews
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == reply.Id);

                var replyDto = new ReviewDto
                {
                    Id = createdReply!.Id,
                    UserId = createdReply.UserId,
                    UserName = createdReply.User.FullName,
                    UserAvatar = createdReply.User.AvatarUrl,
                    Comment = createdReply.Comment,
                    CreatedAt = createdReply.CreatedAt
                };

                return Ok(new
                {
                    success = true,
                    message = "Trả lời thành công",
                    data = replyDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi reply review");
                return StatusCode(500, new { error = "Lỗi khi trả lời đánh giá.", detail = ex.Message });
            }
        }

        // ⭐ Endpoint kiểm tra trạng thái ban
        [HttpGet("ban-status")]
        public ActionResult GetBanStatus([FromQuery] int? devUserId)
        {
            // Lấy UserId
            int userId;
            var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out var uid))
            {
                userId = uid;
            }
            else if (devUserId.HasValue)
            {
                userId = devUserId.Value;
            }
            else
            {
                return Unauthorized(new { error = "Thiếu token hoặc devUserId để test." });
            }

            var isBanned = _antiSpam.IsUserBanned(userId);
            var remainingTime = _antiSpam.GetBanRemainingTime(userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId,
                    isBanned,
                    remainingSeconds = (int?)remainingTime?.TotalSeconds,
                    remainingMinutes = remainingTime?.Minutes,
                    bannedUntil = isBanned ? DateTime.UtcNow.Add(remainingTime ?? TimeSpan.Zero) : (DateTime?)null
                }
            });
        }

        // Cập nhật review
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateReview(
        //    int id,
        //    [FromBody] CreateReviewDto? dto,
        //    [FromQuery] int? devUserId)
        //{
        //    if (dto is null)
        //    {
        //        return BadRequest(new { error = "Body JSON rỗng hoặc sai Content-Type: application/json." });
        //    }

        //    // Lấy UserId
        //    int userId;
        //    var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

        //    if (claim != null && int.TryParse(claim.Value, out var uid))
        //    {
        //        userId = uid;
        //    }
        //    else if (devUserId.HasValue)
        //    {
        //        userId = devUserId.Value;
        //    }
        //    else
        //    {
        //        return Unauthorized(new { error = "Thiếu token hoặc devUserId để test." });
        //    }

        //    try
        //    {
        //        // ⭐ Bỏ CancellationToken trong FindAsync
        //        var review = await _db.Reviews.FindAsync(id);

        //        if (review == null)
        //        {
        //            return NotFound(new { error = "Không tìm thấy đánh giá" });
        //        }

        //        if (review.UserId != userId)
        //        {
        //            return Forbid();
        //        }

        //        // Không cho phép sửa sau 7 ngày
        //        if ((DateTime.UtcNow - review.CreatedAt).TotalDays > 7)
        //        {
        //            return BadRequest(new { error = "Không thể chỉnh sửa đánh giá sau 7 ngày" });
        //        }

        //        review.Rating = dto.Rating;
        //        review.Comment = dto.Comment;
        //        review.ImageUrls = dto.ImageUrls != null && dto.ImageUrls.Any()
        //            ? string.Join(",", dto.ImageUrls)
        //            : null;
        //        review.UpdatedAt = DateTime.UtcNow;

        //        await _db.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Cập nhật đánh giá thành công"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi cập nhật review {ReviewId}", id);
        //        return StatusCode(500, new { error = "Lỗi khi cập nhật đánh giá.", detail = ex.Message });
        //    }
        //}

        // Xóa review
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(
            int id,
            [FromQuery] int? devUserId)
        {
            // Lấy UserId
            int userId;
            var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out var uid))
            {
                userId = uid;
            }
            else if (devUserId.HasValue)
            {
                userId = devUserId.Value;
            }
            else
            {
                return Unauthorized(new { error = "Thiếu token hoặc devUserId để test." });
            }

            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value
                            ?? User.FindFirst("Role")?.Value;

                var review = await _db.Reviews
                    .Include(r => r.Replies)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (review == null)
                {
                    return NotFound(new { error = "Không tìm thấy đánh giá" });
                }

                // Chỉ cho phép chủ review hoặc Admin xóa
                if (review.UserId != userId && userRole != "Admin")
                {
                    return Forbid();
                }

                // Xóa cả replies
                if (review.Replies.Any())
                {
                    _db.Reviews.RemoveRange(review.Replies);
                }

                _db.Reviews.Remove(review);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Xóa đánh giá thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa review {ReviewId}", id);
                return StatusCode(500, new { error = "Lỗi khi xóa đánh giá.", detail = ex.Message });
            }
        }



        // Lấy review của user hiện tại
        [HttpGet("my-reviews")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetMyReviews(
            [FromQuery] int? devUserId)
        {
            // Lấy UserId
            int userId;
            var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out var uid))
            {
                userId = uid;
            }
            else if (devUserId.HasValue)
            {
                userId = devUserId.Value;
            }
            else
            {
                return Unauthorized(new { error = "Thiếu token hoặc devUserId để test." });
            }

            try
            {
                var reviews = await _db.Reviews
                    .Where(r => r.UserId == userId && r.ParentReviewId == null)
                    .Include(r => r.Product)
                    .Include(r => r.Replies)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(); // ⭐ Lấy data trước

                // ⭐ Map sau để tránh lỗi expression tree
                var reviewDtos = reviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    ProductId = r.ProductId,
                    ProductName = r.Product.Name,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    LikeCount = r.LikeCount,
                    IsApproved = r.IsApproved,
                    ImageUrls = !string.IsNullOrEmpty(r.ImageUrls)
                        ? r.ImageUrls.Split(',').ToList()
                        : new List<string>()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = reviewDtos,
                    total = reviewDtos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách review của user");
                return StatusCode(500, new { error = "Lỗi khi lấy danh sách đánh giá.", detail = ex.Message });
            }
        }
    }
}
