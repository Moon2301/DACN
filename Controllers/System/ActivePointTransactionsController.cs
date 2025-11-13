using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/transactions/active-point")]
    [ApiController]
    public class ActivePointTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ActivePointTransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // HÀM HELPER: Map DTO
        // (ActivePointTransaction không có trường Reason, tôi sẽ tạm dùng Type)
        private static ActivePointTransactionDto MapActivePointDto(ActivePointTransaction t)
        {
            string reason = t.Type.ToString(); // "Earning" hoặc "Spending"
            if (t.Amount > 0 && t.Type == TransactionType.Earning) reason = "Điểm danh/Admin nạp";
            if (t.Amount < 0 && t.Type == TransactionType.Spending) reason = "Mở khóa chương";

            return new ActivePointTransactionDto
            {
                ActivePointTransactionId = t.ActivePointTransactionId,
                UserId = t.UserId,
                Username = t.User?.Username ?? "N/A",
                Reason = reason,
                Amount = t.Amount,
                Type = t.Type,
                CreatedAt = t.CreatedAt
            };
        }

        // GET: api/transactions/active-point (Lấy ALL - cho Admin)
        // [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ActivePointTransactionDto>>> GetAllActivePointTransactions(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.ActivePointTransactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => MapActivePointDto(t))
                .ToListAsync();

            return Ok(new PaginatedResult<ActivePointTransactionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // GET: api/transactions/active-point/user/5
        // Lấy lịch sử của 1 User
        // [Authorize]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<PaginatedResult<ActivePointTransactionDto>>> GetActivePointTransactionsForUser(
            int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.ActivePointTransactions
                .Where(t => t.UserId == userId)
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => MapActivePointDto(t))
                .ToListAsync();

            return Ok(new PaginatedResult<ActivePointTransactionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // POST: api/transactions/active-point/admin-grant
        // Admin nạp điểm cho User
        // [Authorize(Roles = "Admin")]
        [HttpPost("admin-grant")]
        public async Task<IActionResult> AdminGrantActivePoint(AdminGrantActivePointDto grantDto)
        {
            var targetUser = await _context.Users.FindAsync(grantDto.TargetUserId);
            if (targetUser == null || targetUser.IsDeleted)
                return NotFound("Không tìm thấy người dùng (TargetUser).");

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Cộng điểm cho User
                targetUser.ActivePoint += grantDto.Amount;

                // 2. Ghi vào sổ kế toán
                var newTransaction = new ActivePointTransaction
                {
                    UserId = grantDto.TargetUserId,
                    Amount = grantDto.Amount,
                    Type = TransactionType.Earning,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ActivePointTransactions.Add(newTransaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok($"Đã nạp {grantDto.Amount} ActivePoint cho User {targetUser.Username}");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return StatusCode(500, "Giao dịch thất bại: " + ex.Message);
            }
        }
    }
}