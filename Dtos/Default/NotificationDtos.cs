using System.ComponentModel.DataAnnotations;

namespace DACN.Dtos
{
    // DTO để hiển thị thông báo
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO để trả về số lượng chưa đọc
    public class UnreadNotificationCountDto
    {
        public int UnreadCount { get; set; }
    }
}