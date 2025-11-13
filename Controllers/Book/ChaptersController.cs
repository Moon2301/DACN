using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/chapters")] // Route CƠ BẢN cho C-R-U-D theo ID
    [ApiController]
    public class ChaptersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChaptersController(AppDbContext context)
        {
            _context = context;
        }

        // --- CÁC ROUTE LỒNG NHAU (NESTED ROUTES) ---

        // GET: api/stories/5/chapters
        // Lấy danh sách chương của 1 truyện (KHÔNG CÓ CONTENT)
        [HttpGet("/api/stories/{storyId}/chapters")] // Ghi đè route
        public async Task<ActionResult<IEnumerable<ChapterListItemDto>>> GetChaptersForStory(int storyId)
        {
            // Kiểm tra truyện có tồn tại không
            if (!await _context.Stories.AnyAsync(s => s.StoryId == storyId && !s.IsDeleted))
            {
                return NotFound("Không tìm thấy truyện.");
            }

            var chapters = await _context.Chapters
                .Where(c => c.StoryId == storyId && c.IsDeleted == false)
                .OrderBy(c => c.ChapterNumber) // Luôn sắp xếp theo số chương
                .Select(c => new ChapterListItemDto
                {
                    ChapterId = c.ChapterId,
                    ChapterNumber = c.ChapterNumber,
                    Title = c.Title,
                    IsVip = c.IsVip,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(chapters);
        }

        // POST: api/stories/5/chapters
        // Tạo một chương mới cho truyện
        [HttpPost("/api/stories/{storyId}/chapters")] // Ghi đè route
        public async Task<ActionResult<ChapterDetailDto>> PostChapter(int storyId, ChapterCreateUpdateDto chapterDto)
        {
            var story = await _context.Stories.FindAsync(storyId);
            if (story == null || story.IsDeleted)
            {
                return NotFound("Không tìm thấy truyện.");
            }

            var newChapter = new Chapter
            {
                StoryId = storyId,
                ChapterNumber = chapterDto.ChapterNumber,
                Title = chapterDto.Title,
                Content = chapterDto.Content,
                IsVip = chapterDto.IsVip,
                VipUnlockAt = chapterDto.VipUnlockAt,
                UnlockPriceMoney = chapterDto.UnlockPriceMoney,
                UnlockPriceActivePoint = chapterDto.UnlockPriceActivePoint,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Khi thêm chương mới, cập nhật thời gian "UpdatedAt" của truyện
            story.UpdatedAt = DateTime.UtcNow;
            story.TotalChapters += 1;

            _context.Chapters.Add(newChapter);
            await _context.SaveChangesAsync();

            // Map sang DTO chi tiết để trả về
            var resultDto = new ChapterDetailDto
            {
                ChapterId = newChapter.ChapterId,
                StoryId = newChapter.StoryId,
                ChapterNumber = newChapter.ChapterNumber,
                Title = newChapter.Title,
                Content = newChapter.Content, // Trả về content cho lần đầu tạo
                CreatedAt = newChapter.CreatedAt,
                IsVip = newChapter.IsVip,
                VipUnlockAt = newChapter.VipUnlockAt,
                UnlockPriceMoney = newChapter.UnlockPriceMoney,
                UnlockPriceActivePoint = newChapter.UnlockPriceActivePoint
            };

            // Trả về route của API "GetChapter" (bên dưới)
            return CreatedAtAction(nameof(GetChapter), new { id = newChapter.ChapterId }, resultDto);
        }

        // --- CÁC ROUTE CRUD THEO ID CHƯƠNG ---

        // GET: api/chapters/5
        // Lấy chi tiết 1 chương (CÓ CONTENT)
        [HttpGet("{id}")]
        public async Task<ActionResult<ChapterDetailDto>> GetChapter(int id)
        {
            var chapter = await _context.Chapters
                .Where(c => c.ChapterId == id && c.IsDeleted == false)
                .Select(c => new ChapterDetailDto
                {
                    ChapterId = c.ChapterId,
                    StoryId = c.StoryId,
                    ChapterNumber = c.ChapterNumber,
                    Title = c.Title,
                    Content = c.Content, // Lấy content
                    CreatedAt = c.CreatedAt,
                    IsVip = c.IsVip,
                    VipUnlockAt = c.VipUnlockAt,
                    UnlockPriceMoney = c.UnlockPriceMoney,
                    UnlockPriceActivePoint = c.UnlockPriceActivePoint
                })
                .FirstOrDefaultAsync();

            if (chapter == null)
            {
                return NotFound("Không tìm thấy chương.");
            }

            return Ok(chapter);
        }

        // PUT: api/chapters/5
        // Cập nhật 1 chương
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChapter(int id, ChapterCreateUpdateDto chapterDto)
        {
            var chapter = await _context.Chapters.FindAsync(id);

            if (chapter == null || chapter.IsDeleted)
            {
                return NotFound("Không tìm thấy chương.");
            }

            // Cập nhật
            chapter.ChapterNumber = chapterDto.ChapterNumber;
            chapter.Title = chapterDto.Title;
            chapter.Content = chapterDto.Content;
            chapter.IsVip = chapterDto.IsVip;
            chapter.VipUnlockAt = chapterDto.VipUnlockAt;
            chapter.UnlockPriceMoney = chapterDto.UnlockPriceMoney;
            chapter.UnlockPriceActivePoint = chapterDto.UnlockPriceActivePoint;

            // Cập nhật thời gian của truyện
            var story = await _context.Stories.FindAsync(chapter.StoryId);
            if (story != null)
            {
                story.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Chapters.Any(e => e.ChapterId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/chapters/5
        // Xóa 1 chương (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter == null)
            {
                return NotFound("Không tìm thấy chương.");
            }

            if (chapter.IsDeleted)
            {
                return BadRequest("Chương này đã bị xóa rồi.");
            }

            chapter.IsDeleted = true;
            // Cập nhật lại Story
            var story = await _context.Stories.FindAsync(chapter.StoryId);
            if (story != null)
            {
                story.TotalChapters = Math.Max(0, story.TotalChapters - 1); // Tránh bị âm
            }
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}