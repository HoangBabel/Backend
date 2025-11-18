namespace Backend.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    namespace Backend.Models
    {
        public class Review
        {
            [Key]
            public int Id { get; set; }

            // 🔗 Liên kết với User
            [Required]
            public int UserId { get; set; }

            [ForeignKey("UserId")]
            [JsonIgnore]
            public User User { get; set; } = null!;

            // 🔗 Liên kết với Product (giả sử bạn có model Product)
            [Required]
            public int ProductId { get; set; }

            [ForeignKey("ProductId")]
            [JsonIgnore]
            public Product Product { get; set; } = null!;

            // ⭐ Đánh giá (1-5 sao)
            [Required]
            [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1-5 sao")]
            public int Rating { get; set; }

            // 💬 Nội dung comment
            [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
            [StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung đánh giá phải từ 10-1000 ký tự")]
            public string Comment { get; set; } = null!;

            // 📅 Thời gian tạo
            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            // 📝 Thời gian cập nhật
            public DateTime? UpdatedAt { get; set; }

            // ✅ Trạng thái duyệt (nếu cần admin duyệt)
            public bool IsApproved { get; set; } = true;

            // 🖼️ Hình ảnh đính kèm (nếu có)
            [StringLength(500)]
            public string? ImageUrls { get; set; } // Lưu dạng JSON array hoặc cách nhau bởi dấu phẩy

            // 👍 Số lượt thích
            public int LikeCount { get; set; } = 0;

            // 🔗 Reply to (nếu muốn reply comment)
            public int? ParentReviewId { get; set; }

            [ForeignKey("ParentReviewId")]
            [JsonIgnore]
            public Review? ParentReview { get; set; }

            // Danh sách reply
            [JsonIgnore]
            public ICollection<Review> Replies { get; set; } = new List<Review>();
        }
    }

}
