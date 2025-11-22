using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using DACN.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/read-history")]
    [ApiController]
    public class ReadHistoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReadHistoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/read-history/user/5
        // Lấy TẤT CẢ lịch sử đọc của User (để làm trang Lịch sử)
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ChapterReadDto>>> GetReadHistoryByUser(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted))
                return NotFound("Không tìm thấy người dùng.");

            var history = await _context.ChapterReadedByUsers
                .Where(r => r.UserId == userId)
                .Include(r => r.Chapter) // Lấy thông tin Chapter
                .OrderByDescending(r => r.ReadAt) // Lần đọc cuối cùng lên trước
                .Select(r => new ChapterReadDto
                {
                    ReadId = r.ReadId,
                    ReadAt = r.ReadAt,
                    ChapterId = r.ChapterId,
                    ChapterNumber = r.Chapter.ChapterNumber,
                    ChapterTitle = r.Chapter.Title,
                    StoryId = r.Chapter.StoryId // Lấy StoryId từ Chapter
                })
                .ToListAsync();

            return Ok(history);
        }

        // GET: api/read-history/user/5/story/10
        // Lấy lịch sử đọc CỦA 1 TRUYỆN (để tick V vào danh sách chương)
        [HttpGet("user/{userId}/story/{storyId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetReadChapterIdsForStory(int userId, int storyId)
        {
            // Trả về một danh sách các ID của chương đã đọc
            // [101, 102, 105]
            // Rất nhẹ và hiệu quả cho client
            var readChapterIds = await _context.ChapterReadedByUsers
                .Where(r => r.UserId == userId && r.Chapter.StoryId == storyId)
                .Select(r => r.ChapterId)
                .ToListAsync();

            return Ok(readChapterIds);
        }


        // POST: api/read-history
        // Đánh dấu đã đọc (Logic: Thêm Mới hoặc Cập Nhật)
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(ChapterReadCreateDto readDto)
        {
            // Kiểm tra
            var chapter = await _context.Chapters
                .FindAsync(readDto.ChapterId);

            if (chapter == null || chapter.IsDeleted)
                return BadRequest("Chương không tồn tại.");

            if (!await _context.Users.AnyAsync(u => u.UserId == readDto.UserId && !u.IsDeleted))
                return BadRequest("User không tồn tại.");

            // Tìm xem đã đọc chương này bao giờ chưa
            var existingRecord = await _context.ChapterReadedByUsers
                .FirstOrDefaultAsync(r => r.UserId == readDto.UserId && r.ChapterId == readDto.ChapterId);

            if (existingRecord != null)
            {
                // 1. ĐÃ ĐỌC RỒI -> Chỉ cập nhật thời gian
                existingRecord.ReadAt = DateTime.UtcNow;
            }
            else
            {
                // 2. ĐỌC LẦN ĐẦU -> Tạo mới
                var newRecord = new ChapterReadedByUser
                {
                    UserId = readDto.UserId,
                    ChapterId = readDto.ChapterId,
                    ReadAt = DateTime.UtcNow
                };
                _context.ChapterReadedByUsers.Add(newRecord);

                // 3. Cập nhật TotalReads của Story (chỉ cho lần đầu)
                var story = await _context.Stories.FindAsync(chapter.StoryId);
                if (story != null)
                {
                    story.TotalReads += 1;
                }
            }

            await _context.SaveChangesAsync();

            return Ok("Đã đánh dấu đã đọc.");
        }


        // GET: api/read-history/user/5/summary
        // Lấy lịch sử TÓM TẮT (mỗi truyện 1 dòng, là chương đọc cuối cùng)
        [HttpGet("user/{userId}/summary")]
        public async Task<ActionResult<IEnumerable<ReadHistorySummaryDto>>> GetReadHistorySummary(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted))
                return NotFound("Không tìm thấy người dùng.");

            // 1. XÂY DỰNG CÂU QUERY (Chưa chạy)
            // Lấy tất cả record, include sẵn Chapter và Story
            var query = _context.ChapterReadedByUsers
                .Where(r => r.UserId == userId)
                .Include(r => r.Chapter)
                    .ThenInclude(c => c.Story);

            // 2. THỰC THI QUERY VÀ TẢI VÀO BỘ NHỚ (Client Evaluation)
            var allHistoryForUser = await query.ToListAsync();

            // 3. XỬ LÝ TRONG BỘ NHỚ (Dùng LINQ-to-Objects, không còn là SQL)
            var historySummary = allHistoryForUser
                .GroupBy(r => r.Chapter.StoryId) // Nhóm theo StoryId
                .Select(g => g.OrderByDescending(r => r.ReadAt).FirstOrDefault()) // Với mỗi nhóm, lấy cái mới nhất
                .OrderByDescending(r => r.ReadAt) // Sắp xếp tổng thể
                .Select(r => new ReadHistorySummaryDto // Map sang DTO
                {
                    StoryId = r.Chapter.Story.StoryId,
                    StoryTitle = r.Chapter.Story.Title,
                    StoryCoverImage = UrlHelper.ResolveImageUrl( r.Chapter.Story.CoverImage),
                    LastReadChapterId = r.ChapterId,
                    LastReadChapterNumber = r.Chapter.ChapterNumber,
                    LastReadChapterTitle = r.Chapter.Title,
                    ReadAt = r.ReadAt,
                    TotalChapters = r.Chapter.Story.TotalChapters
                })
                .ToList(); // Dùng .ToList() vì đang chạy trong C#, không phải .ToListAsync()

            return Ok(historySummary);
        }
    }
}