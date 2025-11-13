using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/notifications/user/5
        // Lấy danh sách thông báo (phân trang)
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<PaginatedResult<NotificationDto>>> GetNotificationsForUser(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15) // Thông báo thường load nhiều hơn (15)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted))
                return NotFound("Không tìm thấy người dùng.");

            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            var totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt) // Mới nhất lên đầu
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            // Dùng DTO phân trang mà chúng ta đã tạo ở phần Comment
            var result = new PaginatedResult<NotificationDto>
            {
                Items = notifications,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        // GET: api/notifications/user/5/unread-count
        // Lấy số lượng chưa đọc (cho cái chuông)
        [HttpGet("user/{userId}/unread-count")]
        public async Task<ActionResult<UnreadNotificationCountDto>> GetUnreadNotificationCount(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted))
                return NotFound("Không tìm thấy người dùng.");

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.IsRead == false);

            return Ok(new UnreadNotificationCountDto { UnreadCount = unreadCount });
        }

        // PUT: api/notifications/10/read
        // Đánh dấu 1 thông báo là đã đọc
        [HttpPut("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            // SAU NÀY: Cần Auth để check user này có sở hữu notificationId này không

            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return NotFound();

            if (notification.IsRead)
                return Ok("Đã đọc từ trước.");

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }

        // PUT: api/notifications/user/5/read-all
        // Đánh dấu TẤT CẢ là đã đọc
        [HttpPut("user/{userId}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted))
                return NotFound("Không tìm thấy người dùng.");

            // Dùng ExecuteUpdateAsync để cập nhật hàng loạt (siêu nhanh)
            // Thay vì load hết vào RAM rồi C# loop
            await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));

            return NoContent(); // 204
        }

        // DELETE: api/notifications/10
        // Xóa 1 thông báo
        [HttpDelete("{notificationId}")]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            // SAU NÀY: Cần Auth để check "chính chủ"

            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return NotFound();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}