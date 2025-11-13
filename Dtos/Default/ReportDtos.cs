// Trong /Dtos/ReportDtos.cs
using System.ComponentModel.DataAnnotations;

namespace DACN.Dtos
{
    // DTO để TẠO MỚI (User gửi)
    public class ReportCreateDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ Token

        [Required]
        public string TargetType { get; set; } // "Story", "Chapter", hoặc "Comment"

        [Required]
        public int TargetId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; }
    }

    // DTO để HIỂN THỊ (Admin xem)
    // Cần hiển thị cả thông tin người gửi
    public class ReportDto
    {
        public int ReportId { get; set; }

        // Người báo cáo
        public int UserId { get; set; }
        public string Username { get; set; } // Cần biết ai báo cáo

        // Mục tiêu bị báo cáo
        public string TargetType { get; set; }
        public int TargetId { get; set; }

        public string Reason { get; set; }

        // Tình trạng
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        public DateTime ReportedAt { get; set; }
    }
}