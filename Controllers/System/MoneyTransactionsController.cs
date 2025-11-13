using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DACN.Controllers
{
    [Route("api/transactions/money")]
    [ApiController]
    public class MoneyTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MoneyTransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // HÀM HELPER: Map DTO
        private static MoneyTransactionDto MapMoneyDto(MoneyTransaction t)
        {
            return new MoneyTransactionDto
            {
                MoneyTransactionId = t.MoneyTransactionId,
                UserId = t.UserId,
                Username = t.User?.Username ?? "N/A",
                ChapterId = t.ChapterId,
                // Nếu có ChapterId, lấy tên Chapter. Nếu không, lấy lý do (Admin)
                TargetName = t.Chapter != null ? t.Chapter.Title : t.Type == TransactionType.Earning ? "Admin nạp tiền" : "Admin trừ tiền",
                Amount = t.Amount,
                Type = t.Type,
                CreatedAt = t.CreatedAt
            };
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<MoneyTransactionDto>>> GetAllMoneyTransactions(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.MoneyTransactions
                .Include(t => t.User)
                .Include(t => t.Chapter)
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => MapMoneyDto(t))
                .ToListAsync();

            return Ok(new PaginatedResult<MoneyTransactionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // GET: api/transactions/money/user/5
        // Lấy lịch sử của 1 User (cho Admin xem hoặc "lịch sử giao dịch bản thân")
        [Authorize]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<PaginatedResult<MoneyTransactionDto>>> GetMoneyTransactionsForUser(
            int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // SAU NÀY: Kiểm tra Auth
            // var currentUserId = GetUserIdFromToken();
            // if (currentUserId != userId && !User.IsInRole("Admin")) return Forbid();

            var query = _context.MoneyTransactions
                .Where(t => t.UserId == userId)
                .Include(t => t.User)
                .Include(t => t.Chapter)
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => MapMoneyDto(t))
                .ToListAsync();

            return Ok(new PaginatedResult<MoneyTransactionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // POST: api/transactions/money/admin-grant
        // Admin nạp tiền cho User (CẦN TRANSACTION)
        [Authorize(Roles = "Admin")]
        [HttpPost("admin-grant")]
        public async Task<IActionResult> AdminGrantMoney(AdminGrantMoneyDto grantDto)
        {
            var targetUser = await _context.Users.FindAsync(grantDto.TargetUserId);
            if (targetUser == null || targetUser.IsDeleted)
                return NotFound("Không tìm thấy người dùng (TargetUser).");

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Cộng tiền cho User
                targetUser.Money += grantDto.Amount;

                // 2. Ghi vào sổ kế toán
                var newTransaction = new MoneyTransaction
                {
                    UserId = grantDto.TargetUserId,
                    ChapterId = null, // Admin nạp nên không có ChapterId
                    Amount = grantDto.Amount, // Số dương
                    Type = TransactionType.Earning,
                    CreatedAt = DateTime.UtcNow
                };
                _context.MoneyTransactions.Add(newTransaction);

                // Lưu cả 2 thay đổi
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok($"Đã nạp {grantDto.Amount} Money cho User {targetUser.Username}");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return StatusCode(500, "Giao dịch thất bại: " + ex.Message);
            }
        }
    }
}