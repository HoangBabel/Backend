using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    // DTO để tạo review mới
    public class CreateReviewDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public string Comment { get; set; } = null!;

        public List<string>? ImageUrls { get; set; }
    }

    public class ReplyReviewDto
    {
        public string Comment { get; set; } = null!;
    }
    // DTO để hiển thị review
    public class ReviewDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? UserAvatar { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int LikeCount { get; set; }
        public bool IsApproved { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public List<ReviewDto> Replies { get; set; } = new();
    }
}
