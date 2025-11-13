// Trong /Dtos/TransactionDtos.cs
using DACN.Models;
using System.ComponentModel.DataAnnotations;

namespace DACN.Dtos
{
    // DTO để HIỂN THỊ (lấy danh sách đã mở khóa)
    public class UnlockedChapterDto
    {
        public int UnlockId { get; set; }
        public int UserId { get; set; }
        public int ChapterId { get; set; }
        public int StoryId { get; set; } // Thêm cái này để client dễ lọc
        public DateTime UnlockedAt { get; set; }
        public int UsedMoney { get; set; }
        public int UsedActivePoint { get; set; }
    }

    // DTO để TẠO MỚI (yêu cầu mở khóa)
    public class UnlockChapterCreateDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ token
        [Required]
        public int ChapterId { get; set; }
        [Required]
        public UnlockMethod Method { get; set; } // "Money" hoặc "ActivePoint"
    }

    public enum UnlockMethod
    {
        Money,
        ActivePoint
    }

    // --- DTOs cho TIỀN (Money) ---

    // DTO để HIỂN THỊ Lịch sử Giao dịch Tiền
    public class MoneyTransactionDto
        {
            public int MoneyTransactionId { get; set; }
            public int UserId { get; set; }
            public string Username { get; set; } // Hiển thị cho Admin

            public int? ChapterId { get; set; }
            public string TargetName { get; set; } = "N/A"; // "Chapter 10" hoặc "Admin nạp"

            public int Amount { get; set; } // Âm/Dương
            public TransactionType Type { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // DTO cho Admin nạp tiền
        public class AdminGrantMoneyDto
        {
            [Required]
            public int TargetUserId { get; set; } // Người nhận

            [Required]
            [Range(1, 10000000)] // Phải nạp số dương
            public int Amount { get; set; }

            [Required]
            public string Reason { get; set; } // Ghi chú (vd: "Quà event")
        }


        // --- DTOs cho ĐIỂM (ActivePoint) ---

        public class ActivePointTransactionDto
        {
            public int ActivePointTransactionId { get; set; }
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Reason { get; set; } // Lý do (Điểm danh, Mở khóa...)
            public int Amount { get; set; }
            public TransactionType Type { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // DTO cho Admin nạp điểm
        public class AdminGrantActivePointDto
        {
            [Required]
            public int TargetUserId { get; set; }
            [Required]
            [Range(1, 1000000)]
            public int Amount { get; set; }
            [Required]
            public string Reason { get; set; } // Ghi chú (vd: "Quà event")
        }

        // DTO để HIỂN THỊ Lịch sử
        public class TicketTransactionDto
        {
            public int TicketTransactionId { get; set; }
            public int UserId { get; set; }
            public string Username { get; set; }

            public int? StoryId { get; set; }
            public string TargetName { get; set; } = "N/A"; // "Truyện ABC" hoặc "Admin nạp"

            public int Amount { get; set; } // Âm/Dương
            public TransactionType Type { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // DTO cho User ĐỀ CỬ
        public class UserNominateDto
        {
            [Required]
            public int UserId { get; set; } // Sau này lấy từ Token
            [Required]
            public int StoryId { get; set; }

            [Range(1, 100)] // Cho phép đề cử nhiều vé 1 lúc, tối đa 100
            public int Amount { get; set; } = 1; // Mặc định là 1 vé
        }

        // DTO cho Admin NẠP VÉ
        public class AdminGrantTicketDto
        {
            [Required]
            public int TargetUserId { get; set; }
            [Required]
            [Range(1, 1000)]
            public int Amount { get; set; }
            [Required]
            public string Reason { get; set; } // Vd: "Quà sự kiện"
        }
}