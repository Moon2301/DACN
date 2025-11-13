using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookmarksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BookmarksController(AppDbContext context)
        {
            _context = context;
        }

        // Hàm helper MapBookmarkToDto (giữ nguyên)
        private static BookmarkDto MapBookmarkToDto(Bookmark bookmark)
        {
            return new BookmarkDto
            {
                BookmarkId = bookmark.BookmarkId,
                UserId = bookmark.UserId,
                StoryId = bookmark.StoryId,
                StoryTitle = bookmark.Story?.Title ?? "N/A",
                StoryCoverImage = bookmark.Story?.CoverImage ?? "N/A",
                ChapterId = bookmark.ChapterId,
                ChapterNumber = bookmark.Chapter?.ChapterNumber ?? 0,
                ChapterTitle = bookmark.Chapter?.Title ?? "N/A",
                CreatedAt = bookmark.CreatedAt
            };
        }

        // GET: api/bookmarks/user/5 
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<BookmarkDto>>> GetBookmarksByUser(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted))
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var bookmarks = await _context.Bookmarks
                .Where(b => b.UserId == userId)
                .Include(b => b.Story)
                .Include(b => b.Chapter)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => MapBookmarkToDto(b))
                .ToListAsync();

            return Ok(bookmarks);
        }

        // POST: api/bookmarks
        [HttpPost]
        public async Task<ActionResult<BookmarkDto>> PostBookmark(BookmarkCreateDto bookmarkDto)
        {
            // Kiểm tra các ID
            if (!await _context.Users.AnyAsync(u => u.UserId == bookmarkDto.UserId && !u.IsDeleted))
                return BadRequest("UserId không hợp lệ.");

            if (!await _context.Stories.AnyAsync(s => s.StoryId == bookmarkDto.StoryId && !s.IsDeleted))
                return BadRequest("StoryId không hợp lệ.");

            if (!await _context.Chapters.AnyAsync(c => c.ChapterId == bookmarkDto.ChapterId && !c.IsDeleted))
                return BadRequest("ChapterId không hợp lệ.");

            // --- LOGIC MỚI: Luôn tạo mới ---
            var newBookmark = new Bookmark
            {
                UserId = bookmarkDto.UserId,
                StoryId = bookmarkDto.StoryId,
                ChapterId = bookmarkDto.ChapterId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookmarks.Add(newBookmark);
            await _context.SaveChangesAsync();

            // Lấy lại DTO đầy đủ để trả về
            var createdBookmark = await _context.Bookmarks
                .Include(b => b.Story)
                .Include(b => b.Chapter)
                .FirstAsync(b => b.BookmarkId == newBookmark.BookmarkId);

            // Trả về 201 Created
            return CreatedAtAction(nameof(GetBookmarksByUser),
                                     new { userId = newBookmark.UserId },
                                     MapBookmarkToDto(createdBookmark));
        }

        // DELETE: api/bookmarks/5 
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBookmark(int id)
        {            
            var bookmark = await _context.Bookmarks.FindAsync(id);
            if (bookmark == null)
            {
                return NotFound("Không tìm thấy bookmark.");
            }

            _context.Bookmarks.Remove(bookmark);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}