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
        [HttpPost("/api/stories/{storyId}/chapters")]
        public async Task<ActionResult<ChapterDetailDto>> PostChapter(int storyId, ChapterCreateUpdateDto chapterDto)
        {
            var story = await _context.Stories.FindAsync(storyId);
            if (story == null || story.IsDeleted)
            {
                return NotFound("Không tìm thấy truyện.");
            }

            // --- BẮT ĐẦU XỬ LÝ LOGIC CONTENT ---
            string finalContent = chapterDto.Content;

            if (!string.IsNullOrEmpty(finalContent))
            {
                // Cách 1: Nếu bạn muốn lưu xuống DB là dấu xuống dòng văn bản thuần túy (Plain Text)
                // Dùng "\n" để xuống dòng. Flutter/Web sẽ hiểu là xuống dòng text.
                finalContent = finalContent.Replace("/n", "\n").Replace("\\n", "\n");
            }
            // --- KẾT THÚC XỬ LÝ ---

            var newChapter = new Chapter
            {
                StoryId = storyId,
                ChapterNumber = chapterDto.ChapterNumber,
                Title = chapterDto.Title,

                Content = finalContent, 

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
                Content = newChapter.Content,
                CreatedAt = newChapter.CreatedAt,
                IsVip = newChapter.IsVip,
                VipUnlockAt = newChapter.VipUnlockAt,
                UnlockPriceMoney = newChapter.UnlockPriceMoney,
                UnlockPriceActivePoint = newChapter.UnlockPriceActivePoint
            };

            return CreatedAtAction(nameof(GetChapter), new { id = newChapter.ChapterId }, resultDto);
        }

        // --- CÁC ROUTE CRUD THEO ID CHƯƠNG ---

        // GET: api/chapters/5
        // Lấy chi tiết 1 chương (CÓ CONTENT)
        // Trong ChaptersController.cs

        [HttpGet("{id}")]
        public async Task<ActionResult<ChapterDetailDto>> GetChapter(int id)
        {
            // 1. Lấy chapter hiện tại (nhớ Include cả Story)
            var currentChapter = await _context.Chapters
                .AsNoTracking() // Dùng AsNoTracking cho nhanh vì chỉ đọc
                .FirstOrDefaultAsync(c => c.ChapterId == id && !c.IsDeleted);

            if (currentChapter == null)
            {
                return NotFound("Không tìm thấy chương.");
            }

            // 2. Lấy StoryId và ChapterNumber của nó
            var storyId = currentChapter.StoryId;
            var currentNumber = currentChapter.ChapterNumber;

            // 3. TÌM CHƯƠNG TRƯỚC (Previous)
            // Là chương CÙNG StoryId, số chương NHỎ HƠN, và LỚN NHẤT
            var prevChapter = await _context.Chapters
                .Where(c => c.StoryId == storyId &&
                            c.ChapterNumber < currentNumber &&
                            !c.IsDeleted)
                .OrderByDescending(c => c.ChapterNumber) // Sắp xếp giảm dần
                .Select(c => new { c.ChapterId }) // Chỉ cần lấy ID
                .FirstOrDefaultAsync(); // Lấy cái đầu tiên (là cái lớn nhất)

            // 4. TÌM CHƯƠNG SAU (Next)
            // Là chương CÙNG StoryId, số chương LỚN HƠN, và NHỎ NHẤT
            var nextChapter = await _context.Chapters
                .Where(c => c.StoryId == storyId &&
                            c.ChapterNumber > currentNumber &&
                            !c.IsDeleted)
                .OrderBy(c => c.ChapterNumber) // Sắp xếp tăng dần
                .Select(c => new { c.ChapterId }) // Chỉ cần lấy ID
                .FirstOrDefaultAsync(); // Lấy cái đầu tiên (là cái nhỏ nhất)

            // 5. Tạo DTO trả về
            var chapterDto = new ChapterDetailDto
            {
                ChapterId = currentChapter.ChapterId,
                StoryId = currentChapter.StoryId,
                Title = currentChapter.Title,
                Content = currentChapter.Content, // Giả sử bồ có cột Content
                ChapterNumber = currentChapter.ChapterNumber,
                CreatedAt = currentChapter.CreatedAt,

                // Gán 2 ID tìm được vào
                // Dùng ?.ChapterId để nếu prevChapter là null thì nó trả về null
                PreviousChapterId = prevChapter?.ChapterId,
                NextChapterId = nextChapter?.ChapterId
            };

            return Ok(chapterDto);
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