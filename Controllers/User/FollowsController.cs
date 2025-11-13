using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/follows")] // Route mới: api/follows
    [ApiController]
    public class FollowsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FollowsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/follows/user/5
        // Lấy danh sách truyện user đang theo dõi VÀ tiến độ đọc
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<FollowDto>>> GetFollowedStoriesByUser(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted))
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var follows = await _context.FollowedStoryByUsers
                .Where(f => f.UserId == userId)
                .Include(f => f.Story) // Lấy thông tin Story
                .Include(f => f.CurrentChapter) // Lấy thông tin Chapter (tiến độ)
                .Select(f => new FollowDto
                {
                    FollowId = f.FollowId,
                    UserId = f.UserId,
                    StoryId = f.StoryId,
                    StoryTitle = f.Story.Title,
                    StoryCoverImage = f.Story.CoverImage,
                    StoryStatus = f.Story.Status,
                    FollowedAt = f.FollowedAt,
                    TotalStoryChapters = f.Story.TotalChapters,
                    // --- Đây là logic vàng ---
                    CurrentChapterId = f.CurrentChapterId,
                    // Nếu CurrentChapter là null (chưa đọc) thì trả về 0
                    CurrentChapterNumber = f.CurrentChapter != null ? f.CurrentChapter.ChapterNumber : 0,
                    CurrentChapterTitle = f.CurrentChapter != null ? f.CurrentChapter.Title : "Chưa đọc"
                })
                .ToListAsync();

            return Ok(follows);
        }

        // POST: api/follows
        // Hành động: THEO DÕI
        [HttpPost]
        public async Task<IActionResult> FollowStory(FollowCreateDto followDto)
        {
            // Kiểm tra
            var story = await _context.Stories.FindAsync(followDto.StoryId);
            if (story == null || story.IsDeleted)
                return BadRequest("StoryId không hợp lệ.");

            if (!await _context.Users.AnyAsync(u => u.UserId == followDto.UserId && !u.IsDeleted))
                return BadRequest("UserId không hợp lệ.");

            // Kiểm tra xem đã follow chưa
            var alreadyExists = await _context.FollowedStoryByUsers
                .AnyAsync(f => f.UserId == followDto.UserId && f.StoryId == followDto.StoryId);

            if (alreadyExists)
            {
                return BadRequest("Bạn đã theo dõi truyện này rồi.");
            }

            var newFollow = new FollowedStoryByUser
            {
                UserId = followDto.UserId,
                StoryId = followDto.StoryId,
                CurrentChapterId = null, // Mới follow, chưa có tiến độ
                FollowedAt = DateTime.UtcNow
            };

            // Tăng số lượng follower của truyện
            story.TotalFollowers += 1;

            _context.FollowedStoryByUsers.Add(newFollow);
            await _context.SaveChangesAsync();

            return Ok("Theo dõi thành công."); // Trả về 200 OK đơn giản
        }

        // DELETE: api/follows/unfollow
        // Hành động: BỎ THEO DÕI
        [HttpDelete("unfollow")]
        public async Task<IActionResult> UnfollowStory(UnfollowDto unfollowDto)
        {
            var follow = await _context.FollowedStoryByUsers
                .FirstOrDefaultAsync(f => f.UserId == unfollowDto.UserId && f.StoryId == unfollowDto.StoryId);

            if (follow == null)
            {
                return NotFound("Bạn chưa theo dõi truyện này.");
            }

            // Giảm số lượng follower của truyện
            var story = await _context.Stories.FindAsync(unfollowDto.StoryId);
            if (story != null)
            {
                story.TotalFollowers = Math.Max(0, story.TotalFollowers - 1); // Không để số âm
            }

            _context.FollowedStoryByUsers.Remove(follow);
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }


        // PUT: api/follows/progress
        // Hành động: CẬP NHẬT TIẾN ĐỘ ĐỌC
        [HttpPut("progress")]
        public async Task<IActionResult> UpdateReadProgress(FollowProgressUpdateDto progressDto)
        {
            // Kiểm tra xem chương này có thật không
            if (!await _context.Chapters.AnyAsync(c => c.ChapterId == progressDto.ChapterId && c.StoryId == progressDto.StoryId))
            {
                return BadRequest("Chương hoặc Truyện không khớp.");
            }

            // Tìm bản ghi follow
            var follow = await _context.FollowedStoryByUsers
                .FirstOrDefaultAsync(f => f.UserId == progressDto.UserId && f.StoryId == progressDto.StoryId);

            if (follow == null)
            {
                // User đọc mà chưa follow? -> Tự động follow cho họ
                var story = await _context.Stories.FindAsync(progressDto.StoryId);
                if (story == null || story.IsDeleted) return BadRequest("Truyện không tồn tại.");

                follow = new FollowedStoryByUser
                {
                    UserId = progressDto.UserId,
                    StoryId = progressDto.StoryId,
                    FollowedAt = DateTime.UtcNow,
                    CurrentChapterId = progressDto.ChapterId // Cập nhật tiến độ luôn
                };

                story.TotalFollowers += 1;
                _context.FollowedStoryByUsers.Add(follow);
            }
            else
            {
                // Đã follow -> Chỉ cập nhật tiến độ
                follow.CurrentChapterId = progressDto.ChapterId;
            }

            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }
    }
}