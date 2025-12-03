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
        // GET: api/follows/user/5
        // Lấy danh sách truyện user đang theo dõi VÀ tiến độ đọc (Lấy từ Lịch sử đọc mới nhất)
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<FollowDto>>> GetFollowedStoriesByUser(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted))
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            // 1. Lấy thông tin FollowedStoryByUser và Story
            var follows = await _context.FollowedStoryByUsers
                .Where(f => f.UserId == userId)
                .Include(f => f.Story)
                .Select(f => new
                {
                    // Các trường cơ bản (Cần thiết cho DTO)
                    Follow = f,
                    Story = f.Story,

                    // 2. SUBQUERY: Lấy chương ĐÃ ĐỌC có ChapterNumber LỚN NHẤT cho Story này
                    LatestChapterRead = _context.ChapterReadedByUsers
                        .Where(r => r.UserId == userId && r.Chapter.StoryId == f.StoryId)
                        .OrderByDescending(r => r.Chapter.ChapterNumber) // Sắp xếp theo số chương giảm dần
                        .Select(r => r.Chapter) // Chỉ cần lấy object Chapter
                        .FirstOrDefault() // Lấy cái lớn nhất
                })
                .ToListAsync();

            // 3. Mapping sang DTO cuối cùng (Trong bộ nhớ)
            var dtos = follows.Select(f =>
            {
                // Ưu tiên: Chương mới nhất trong Lịch sử đọc (f.LatestChapterRead)
                // Fallback: Nếu lịch sử đọc trống, dùng tiến độ cũ đã lưu trong bảng Follow (f.Follow.CurrentChapter)
                var latestProgress = f.LatestChapterRead;

                // Nếu không có lịch sử đọc cho truyện này, ta lấy CurrentChapter từ DB (cần phải join/include)
                // Tuy nhiên, để tránh lỗi N+1, ta sẽ chỉ fallback về ID cũ đã lưu
                if (latestProgress == null && f.Follow.CurrentChapterId.HasValue)
                {
                    // Nếu không có lịch sử, ta thử lấy thông tin của Chapter đã lưu
                    // Lưu ý: Cần thêm logic để lấy Chapter object từ ID nếu muốn hiển thị Title/Number chính xác
                    // Tuy nhiên, để giữ query trong 1 lần gọi DB, ta dùng null và 0 như logic cũ
                }

                // Do CurrentChapter không còn được Include trực tiếp, ta chỉ dựa vào kết quả Subquery
                return new FollowDto
                {
                    FollowId = f.Follow.FollowId,
                    UserId = f.Follow.UserId,
                    StoryId = f.Story.StoryId,
                    StoryTitle = f.Story.Title,
                    StoryCoverImage = f.Story.CoverImage,
                    StoryStatus = f.Story.Status,
                    FollowedAt = f.Follow.FollowedAt,
                    TotalStoryChapters = f.Story.TotalChapters,

                    // --- LOGIC MỚI ÁP DỤNG ---
                    // Nếu tìm thấy lịch sử đọc mới nhất, dùng nó để hiển thị
                    CurrentChapterId = latestProgress?.ChapterId ?? f.Follow.CurrentChapterId,
                    CurrentChapterNumber = latestProgress?.ChapterNumber ?? 0,
                    CurrentChapterTitle = latestProgress?.Title ?? "Chưa đọc"
                };
            }).ToList();

            return Ok(dtos);
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