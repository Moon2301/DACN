using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Cần cho .Average()

namespace DACN.Controllers
{
    [Route("api/ratings")]
    [ApiController]
    public class RatingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RatingsController(AppDbContext context)
        {
            _context = context;
        }

        // HÀM HELPER: Tính toán lại rating của Story
        // (Rất quan trọng, phải gọi sau mỗi lần thay đổi)
        private async Task RecalculateStoryRating(int storyId)
        {
            var story = await _context.Stories.FindAsync(storyId);
            if (story == null) return;

            // Lấy tất cả rating của truyện này
            var ratings = await _context.Ratings
                .Where(r => r.StoryId == storyId)
                .ToListAsync();

            if (ratings.Any())
            {
                story.TotalRatings = ratings.Count;
                story.AverageRating = ratings.Average(r => r.Score);
            }
            else
            {
                story.TotalRatings = 0;
                story.AverageRating = 0.0m;
            }

            // SaveChangesAsync() sẽ được gọi ở hàm cha
        }

        // GET: api/stories/{storyId}/ratings
        // Lấy danh sách rating của 1 truyện (phân trang)
        [HttpGet("/api/stories/{storyId}/ratings")]
        public async Task<ActionResult<PaginatedResult<RatingDto>>> GetRatingsForStory(
            int storyId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!await _context.Stories.AnyAsync(s => s.StoryId == storyId && !s.IsDeleted))
                return NotFound("Không tìm thấy truyện.");

            var query = _context.Ratings
                .Where(r => r.StoryId == storyId)
                .Include(r => r.User); // Lấy thông tin User

            var totalCount = await query.CountAsync();

            var ratings = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RatingDto // Map sang DTO
                {
                    RatingId = r.RatingId,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    AvatarUrl = r.User.AvatarUrl,
                    StoryId = r.StoryId,
                    Score = r.Score,
                    Review = r.Review,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var result = new PaginatedResult<RatingDto>
            {
                Items = ratings,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
            return Ok(result);
        }

        // GET: api/ratings/user/{userId}
        // Lấy tất cả đánh giá của 1 user
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<RatingDto>>> GetRatingsByUser(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted))
                return NotFound("Không tìm thấy người dùng.");

            var ratings = await _context.Ratings
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RatingDto
                {
                    RatingId = r.RatingId,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    AvatarUrl = r.User.AvatarUrl,
                    StoryId = r.StoryId,
                    Score = r.Score,
                    Review = r.Review,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(ratings);
        }

        // POST: api/ratings
        // Tạo mới (hoặc cập nhật) 1 rating
        [HttpPost]
        public async Task<ActionResult<RatingDto>> PostRating(RatingCreateUpdateDto ratingDto)
        {
            // Kiểm tra
            if (!await _context.Users.AnyAsync(u => u.UserId == ratingDto.UserId && !u.IsDeleted))
                return BadRequest("User không hợp lệ.");

            if (!await _context.Stories.AnyAsync(s => s.StoryId == ratingDto.StoryId && !s.IsDeleted))
                return BadRequest("Truyện không hợp lệ.");

            // --- LOGIC: THÊM MỚI HOẶC CẬP NHẬT ---
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserId == ratingDto.UserId && r.StoryId == ratingDto.StoryId);

            if (existingRating != null)
            {
                // Đã có -> Cập nhật
                existingRating.Score = ratingDto.Score;
                existingRating.Review = ratingDto.Review;
                existingRating.CreatedAt = DateTime.UtcNow; // Cập nhật thời gian
            }
            else
            {
                // Chưa có -> Tạo mới
                existingRating = new Rating
                {
                    UserId = ratingDto.UserId,
                    StoryId = ratingDto.StoryId,
                    Score = ratingDto.Score,
                    Review = ratingDto.Review,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Ratings.Add(existingRating);
            }

            // Tính toán lại rating của Story
            await RecalculateStoryRating(ratingDto.StoryId);

            await _context.SaveChangesAsync();

            // Lấy lại DTO đầy đủ để trả về (cùng với User)
            var result = await _context.Ratings
                .Include(r => r.User)
                .Where(r => r.RatingId == existingRating.RatingId)
                .Select(r => new RatingDto
                {
                    RatingId = r.RatingId,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    AvatarUrl = r.User.AvatarUrl,
                    StoryId = r.StoryId,
                    Score = r.Score,
                    Review = r.Review,
                    CreatedAt = r.CreatedAt
                })
                .FirstAsync();

            return Ok(result); // Trả về 200 OK (vì có thể là tạo mới hoặc update)
        }

        // DELETE: api/ratings/5
        // Xóa 1 rating
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRating(int id)
        {
            var rating = await _context.Ratings.FindAsync(id);
            if (rating == null)
                return NotFound();

            // SAU NÀY: Kiểm tra "chính chủ"
            // if (rating.UserId != GetUserIdFromToken()) return Forbid();

            _context.Ratings.Remove(rating);

            // Tính toán lại rating SAU KHI XÓA
            await RecalculateStoryRating(rating.StoryId);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}