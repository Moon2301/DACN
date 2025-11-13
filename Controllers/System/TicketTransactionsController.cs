using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/transactions/ticket")]
    [ApiController]
    public class TicketTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketTransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // HÀM HELPER: Map DTO
        private static TicketTransactionDto MapTicketDto(TicketTransaction t)
        {
            return new TicketTransactionDto
            {
                TicketTransactionId = t.TicketTransactionId,
                UserId = t.UserId,
                Username = t.User?.Username ?? "N/A",
                StoryId = t.StoryId,
                // Nếu có StoryId, lấy tên Story. Nếu không, lấy lý do (Admin)
                TargetName = t.Story != null ? t.Story.Title : (t.Type == TransactionType.Earning ? "Nạp từ hệ thống" : "Lỗi"),
                Amount = t.Amount,
                Type = t.Type,
                CreatedAt = t.CreatedAt
            };
        }

        // --- CÁC API `GET` LỊCH SỬ ---

        // GET: api/transactions/ticket (Lấy ALL - cho Admin)
        // [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<TicketTransactionDto>>> GetAllTicketTransactions(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // (Copy y hệt code từ MoneyTransactionsController, chỉ đổi model)
            var query = _context.TicketTransactions
                .Include(t => t.User)
                .Include(t => t.Story)
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(t => MapTicketDto(t)).ToListAsync();

            return Ok(new PaginatedResult<TicketTransactionDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
        }

        // GET: api/transactions/ticket/user/5 (Lấy của 1 User)
        // [Authorize]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<PaginatedResult<TicketTransactionDto>>> GetTicketTransactionsForUser(
            int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // (Copy y hệt code từ MoneyTransactionsController, chỉ đổi model)
            var query = _context.TicketTransactions
                .Where(t => t.UserId == userId)
                .Include(t => t.User)
                .Include(t => t.Story)
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(t => MapTicketDto(t)).ToListAsync();

            return Ok(new PaginatedResult<TicketTransactionDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
        }


        // --- CÁC API `POST` (THEO YÊU CẦU CỦA BẠN) ---

        // POST: api/transactions/ticket/nominate
        // YÊU CẦU 1: USER ĐỀ CỬ (TIÊU VÉ)
        // [Authorize]
        [HttpPost("nominate")]
        public async Task<IActionResult> NominateStory(UserNominateDto nominateDto)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FindAsync(nominateDto.UserId);
                if (user == null) return NotFound("Không tìm thấy người dùng.");

                var story = await _context.Stories.FindAsync(nominateDto.StoryId);
                if (story == null || story.IsDeleted) return NotFound("Không tìm thấy truyện.");

                int amountToSpend = nominateDto.Amount;
                if (user.Ticket < amountToSpend)
                    return StatusCode(402, $"Không đủ vé. Bạn có {user.Ticket} vé, cần {amountToSpend} vé."); // 402 Payment Required

                // 1. Trừ vé của User
                user.Ticket -= amountToSpend;

                // 2. Cộng "thành tích" cho Truyện
                story.TotalTicketsEarned += amountToSpend;

                // 3. Ghi sổ kế toán
                var newTransaction = new TicketTransaction
                {
                    UserId = user.UserId,
                    StoryId = story.StoryId,
                    Amount = -amountToSpend, // SỐ ÂM (vì tiêu)
                    Type = TransactionType.Spending,
                };
                _context.TicketTransactions.Add(newTransaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok($"Đề cử thành công {amountToSpend} vé cho truyện '{story.Title}'. Bạn còn {user.Ticket} vé.");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return StatusCode(500, "Giao dịch thất bại: " + ex.Message);
            }
        }

        // POST: api/transactions/ticket/admin-grant
        // YÊU CẦU 2: ADMIN NẠP VÉ
        // [Authorize(Roles = "Admin")]
        [HttpPost("admin-grant")]
        public async Task<IActionResult> AdminGrantTicket(AdminGrantTicketDto grantDto)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var targetUser = await _context.Users.FindAsync(grantDto.TargetUserId);
                if (targetUser == null) return NotFound("Không tìm thấy người dùng.");

                // 1. Cộng vé cho User
                targetUser.Ticket += grantDto.Amount;

                // 2. Ghi sổ kế toán
                var newTransaction = new TicketTransaction
                {
                    UserId = targetUser.UserId,
                    StoryId = null, // Admin nạp, không có StoryId
                    Amount = grantDto.Amount, // SỐ DƯƠNG
                    Type = TransactionType.Earning,
                };
                _context.TicketTransactions.Add(newTransaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok($"Đã nạp {grantDto.Amount} vé cho User {targetUser.Username}.");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return StatusCode(500, "Giao dịch thất bại: " + ex.Message);
            }
        }
    }
}