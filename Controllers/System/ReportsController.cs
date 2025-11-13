using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // --- DÀNH CHO USER ---

        // POST: api/reports
        // User tạo một báo cáo mới
        [HttpPost]
        public async Task<IActionResult> CreateReport(ReportCreateDto reportDto)
        {
            // --- Xác thực ---
            if (!await _context.Users.AnyAsync(u => u.UserId == reportDto.UserId && !u.IsDeleted))
                return BadRequest("User không hợp lệ.");

            // Kiểm tra TargetType và TargetId
            var targetType = reportDto.TargetType.ToLower();
            bool targetExists = false;

            if (targetType == "story")
            {
                targetExists = await _context.Stories.AnyAsync(s => s.StoryId == reportDto.TargetId && !s.IsDeleted);
            }
            else if (targetType == "chapter")
            {
                targetExists = await _context.Chapters.AnyAsync(c => c.ChapterId == reportDto.TargetId && !c.IsDeleted);
            }
            else if (targetType == "comment")
            {
                // Giả định comment bị "ẩn danh" (sửa content) chứ không bị xóa
                targetExists = await _context.Comments.AnyAsync(c => c.CommentId == reportDto.TargetId);
            }
            else
            {
                return BadRequest("TargetType không hợp lệ. Chỉ chấp nhận 'Story', 'Chapter', 'Comment'.");
            }

            if (!targetExists)
                return NotFound("Đối tượng bạn báo cáo không tồn tại.");

            // --- Tạo Report ---
            var newReport = new Report
            {
                UserId = reportDto.UserId,
                TargetType = reportDto.TargetType, // Lưu lại dạng gốc (vd: "Story")
                TargetId = reportDto.TargetId,
                Reason = reportDto.Reason,
                IsResolved = false,
                ReportedAt = DateTime.UtcNow
            };

            _context.Reports.Add(newReport);
            await _context.SaveChangesAsync();

            // 201 Created là tốt nhất, nhưng 200 OK cũng được
            return Ok("Báo cáo của bạn đã được gửi. Cảm ơn bạn.");
        }

        // --- DÀNH CHO ADMIN ---
        // (Sau này, các API này cần được bảo vệ bằng [Authorize(Roles = "Admin")])

        // GET: api/reports?resolved=false
        // Lấy danh sách báo cáo (lọc theo trạng thái, phân trang)
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ReportDto>>> GetReports(
            [FromQuery] bool? resolved, // Lọc: (true/false/null = all)
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Reports
                .Include(r => r.User) // Lấy thông tin người báo cáo
                .AsQueryable(); // Để xây dựng query

            // Thêm bộ lọc
            if (resolved.HasValue)
            {
                query = query.Where(r => r.IsResolved == resolved.Value);
            }

            var totalCount = await query.CountAsync();

            var reports = await query
                .OrderByDescending(r => r.ReportedAt) // Mới nhất lên trước
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReportDto
                {
                    ReportId = r.ReportId,
                    UserId = r.UserId,
                    Username = r.User.Username, // Lấy từ User
                    TargetType = r.TargetType,
                    TargetId = r.TargetId,
                    Reason = r.Reason,
                    IsResolved = r.IsResolved,
                    ResolvedAt = r.ResolvedAt,
                    ReportedAt = r.ReportedAt
                })
                .ToListAsync();

            // Dùng DTO phân trang mà chúng ta đã tạo (PaginatedResult<T>)
            var result = new PaginatedResult<ReportDto>
            {
                Items = reports,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        // PUT: api/reports/5/resolve
        // Admin đánh dấu là "Đã giải quyết"
        [HttpPut("{id}/resolve")]
        public async Task<IActionResult> ResolveReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null)
                return NotFound("Không tìm thấy báo cáo.");

            if (report.IsResolved)
                return BadRequest("Báo cáo này đã được giải quyết từ trước.");

            report.IsResolved = true;
            report.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }
    }
}