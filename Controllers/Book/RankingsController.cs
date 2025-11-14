using DACN.Data;
using DACN.Models; // Cần để dùng StoryRanking và RankingType
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Cần cho Enum.TryParse
using System.Linq;

namespace DACN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RankingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RankingsController(AppDbContext context)
        {
            _context = context;
        }

        // API DUY NHẤT ĐỂ LẤY TẤT CẢ BXH
        // GET: api/rankings?type=READS_DAY_ALL
        // GET: api/rankings?type=READS_DAY_GENRE&genreId=5
        // GET: api/rankings?type=TICKETS_MONTH_ALL&page=2&limit=10
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StoryRanking>>> GetRankings(
            [FromQuery] string type, // Bắt buộc, ví dụ: "READS_DAY_ALL"
            [FromQuery] int? genreId = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            // --- 1. Validate tham số 'type' ---
            if (string.IsNullOrEmpty(type))
            {
                return BadRequest(new { success = false, message = "Tham số 'type' là bắt buộc." });
            }

            // Chuyển string "READS_DAY_ALL" thành enum RankingType.READS_DAY_ALL
            // 'true' là để nó case-insensitive (chấp nhận "reads_day_all")
            if (!Enum.TryParse<RankingType>(type, true, out var rankingType))
            {
                return BadRequest(new { success = false, message = $"Giá trị 'type' không hợp lệ: {type}" });
            }

            // --- 2. Validate 'genreId' (Rất quan trọng) ---
            bool isGenreRanking = rankingType.ToString().EndsWith("_GENRE");

            if (isGenreRanking && (!genreId.HasValue || genreId.Value == 0))
            {
                // Nếu là BXH Genre, BẮT BUỘC phải có genreId
                return BadRequest(new { success = false, message = "Cần cung cấp 'genreId' cho loại BXH này." });
            }
            else if (!isGenreRanking)
            {
                // Nếu là BXH "ALL", ép genreId phải là null
                genreId = null;
            }

            // --- 3. Validate phân trang ---
            if (page <= 0) page = 1;
            // Job lấy Top 100, nên limit tối đa là 100
            if (limit <= 0 || limit > 100) limit = 20;

            // --- 4. Tính toán Rank (thay vì Skip/Take) ---
            // Page 1, Limit 20 -> Rank 1 đến 20
            // Page 2, Limit 20 -> Rank 21 đến 40
            var rankStart = (page - 1) * limit + 1;
            var rankEnd = page * limit;

            // --- 5. Query ---
            try
            {
                var query = _context.StoryRankings
                    .AsNoTracking() // Chỉ đọc -> thêm AsNoTracking() cho nhanh
                    .Where(r => r.Type == rankingType &&
                                r.GenreId == genreId && // Lọc theo genreId (hoặc null)
                                r.Rank >= rankStart &&   // Lọc theo Rank
                                r.Rank <= rankEnd)
                    .OrderBy(r => r.Rank); // Sắp xếp theo Rank đã tính sẵn

                var results = await query.ToListAsync();

                // (Optional: Trả về 1 object thống kê phân trang)
                // var totalRanks = 100; // Hoặc có thể Count()
                // var totalPages = (int)Math.Ceiling(totalRanks / (double)limit);

                return Ok(new
                {
                    success = true,
                    // currentPage = page,
                    // totalPages = totalPages,
                    data = results
                });
            }
            catch (Exception ex)
            {
                // (Nên log lỗi 'ex' ra)
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ nội bộ." });
            }
        }
    }
}