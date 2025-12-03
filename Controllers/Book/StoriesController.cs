using DACN.Data;
using DACN.Dtos;
using DACN.Helpers;
using DACN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public StoriesController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Vì query Story DTO rất phức tạp, đưa nó vào 1 IQueryable
        // HÀM HELPER ĐỂ DÙNG CHUNG
        private IQueryable<Story> GetStoriesAsQueryable(
        string? searchTerm = null,
        int? genreId = null,
        int? tagId = null,
        StoryStatus? status = null)
        {
            // Bắt đầu với các truyện chưa bị xóa
            var query = _context.Stories
                                .Where(s => s.IsDeleted == false);

            // 1. Lọc theo searchTerm
            // Kiểm tra tính null hoặc chuỗi rỗng
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(s => s.Title.ToLower().Contains(lowerSearchTerm) ||
                                         s.Author.ToLower().Contains(lowerSearchTerm));
            }

            // 2. Lọc theo GenreId
            // Kiểm tra xem có giá trị (không null) không
            if (genreId.HasValue)
            {
                query = query.Where(s => s.GenreId == genreId.Value);
            }

            // 3. Lọc theo TagId
            // Kiểm tra xem có giá trị (không null) không
            if (tagId.HasValue)
            {
                query = query.Where(s => s.StoryTags.Any(st => st.TagId == tagId.Value));
            }

            // 4. Lọc theo Status
            // Kiểm tra xem có giá trị (không null) không
            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            // 5. TRẢ VỀ QUERY GỐC (Đã Include, nhưng KHÔNG Select)
            return query
                .Include(s => s.Genre)
                .Include(s => s.UploadedBy)
                .Include(s => s.StoryTags)
                    .ThenInclude(st => st.Tag);
        }

        // HÀM MAP DÙNG CHUNG (C#)
        private StoryDto MapToDto(Story s)
        {
            // ... (logic MapToDto giữ nguyên) ...
            // Phần này không cần thay đổi
            return new StoryDto
            {
                StoryId = s.StoryId,
                Title = s.Title,
                Author = s.Author,
                Description = s.Description,
                CoverImage = UrlHelper.ResolveImageUrl(s.CoverImage),
                Status = s.Status,
                GenreId = s.GenreId,
                GenreName = s.Genre?.Name,
                UploadedByUserId = s.UploadedByUserId,
                UploadedByUsername = s.UploadedBy?.Username,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                Tags = s.StoryTags.Select(st => st.Tag.Name).ToList(),
                TotalReads = s.TotalReads,
                AverageRating = s.AverageRating,
                TotalFollowers = s.TotalFollowers,
                TotalComments = s.TotalComments,
                TotalChapters = s.TotalChapters
            };
        }
        // Hết helper

        // GET: api/stories
        // Lấy tất cả truyện (có lọc, tìm kiếm, sắp xếp, phân trang)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<StoryDto>>> GetStories(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? genreId = null,
            [FromQuery] string? tagId = null,
            [FromQuery] StoryStatus? status = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            int? finalGenreId = null;
            int? finalTagId = null;

            // Xử lý GenreId
            if (!string.IsNullOrEmpty(genreId) && genreId.ToLower() != "null")
            {
                if (int.TryParse(genreId, out int gId))
                {
                    finalGenreId = gId;
                }
            }

            // Xử lý TagId
            if (!string.IsNullOrEmpty(tagId) && tagId.ToLower() != "null")
            {
                if (int.TryParse(tagId, out int tId))
                {
                    finalTagId = tId;
                }
            }

            //status
            if (status == StoryStatus.TatCa) status = null;

            // 1. Validate phân trang
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 20;
            if (limit > 100) limit = 100;

            try
            {                
                var query = GetStoriesAsQueryable(searchTerm, finalGenreId, finalTagId, status);

                // 2. Áp dụng SẮP XẾP
                switch (sortBy?.ToLower())
                {
                    case "latest":
                        query = query.OrderByDescending(s => s.UpdatedAt);
                        break;
                    case "top_reads":
                        query = query.OrderByDescending(s => s.TotalReads);
                        break;
                    case "avg_rating":
                        query = query.OrderByDescending(s => s.AverageRating);
                        break;
                    // ... các case khác ...
                    default:
                        query = query.OrderByDescending(s => s.UpdatedAt);
                        break;
                }

                // 3. Phân trang & Map DTO (Giữ nguyên)
                var offset = (page - 1) * limit;
                var totalStories = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalStories / (double)limit);

                var stories_from_db = await query
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                var dtos = stories_from_db.Select(MapToDto).ToList();

                return Ok(new
                {
                    success = true,
                    currentPage = page,
                    totalPages = totalPages,
                    data = dtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ nội bộ." });
            }
        }

        // GET: api/stories/5
        // Lấy 1 truyện theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<StoryDto>> GetStory(int id)
        {
            var story = await GetStoriesAsQueryable() // 1. Lấy query gốc
                .FirstOrDefaultAsync(s => s.StoryId == id); // 2. Rút data

            if (story == null)
            {
                return NotFound("Không tìm thấy truyện.");
            }

            return Ok(MapToDto(story)); // 3. Map (dùng C#)
        }

        // GET: api/stories/user/5
        // Lấy tất cả truyện theo USER ID
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<StoryDto>>> GetStoriesByUser(int userId)
        {
            // Kiểm tra user có tồn tại không
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted);
            if (!userExists)
            {
                return NotFound("Không tìm thấy người dùng này.");
            }

            var stories = await GetStoriesAsQueryable() // 1. Lấy query gốc
                .Where(s => s.UploadedByUserId == userId)
                .ToListAsync(); // 2. Rút data

            return Ok(stories.Select(MapToDto).ToList()); // 3. Map (dùng C#)
        }

        // POST: api/stories
        // Tạo truyện mới
        [HttpPost]
        public async Task<ActionResult<StoryDto>> PostStory(StoryCreateDto storyDto)
        {
            // Kiểm tra các ID
            if (!await _context.Genres.AnyAsync(g => g.GenreId == storyDto.GenreId && !g.IsDeleted))
                return BadRequest("GenreId không hợp lệ.");

            if (!await _context.Users.AnyAsync(u => u.UserId == storyDto.UploadedByUserId && !u.IsDeleted))
                return BadRequest("UploadedByUserId không hợp lệ.");

            var newStory = new Story
            {
                Title = storyDto.Title,
                Author = storyDto.Author,
                Description = storyDto.Description,
                CoverImage = storyDto.CoverImage,
                Status = storyDto.Status,
                GenreId = storyDto.GenreId,
                UploadedByUserId = storyDto.UploadedByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Xử lý Tag
            if (storyDto.TagIds != null && storyDto.TagIds.Any())
            {
                newStory.StoryTags = new List<StoryTag>();
                foreach (var tagId in storyDto.TagIds.Distinct()) // Dùng Distinct để tránh trùng lặp
                {
                    // Đảm bảo tagId tồn tại
                    if (await _context.Tags.AnyAsync(t => t.TagId == tagId && !t.IsDeleted))
                    {
                        newStory.StoryTags.Add(new StoryTag { TagId = tagId });
                    }
                }
            }

            _context.Stories.Add(newStory);
            await _context.SaveChangesAsync();

            // Lấy lại DTO để trả về (cách an toàn)
            var resultStory = await GetStoriesAsQueryable() // 1. Lấy query gốc
                .FirstOrDefaultAsync(s => s.StoryId == newStory.StoryId); // 2. Rút data

            var resultDto = MapToDto(resultStory); // 3. Map (dùng C#)

            return CreatedAtAction(nameof(GetStory), new { id = newStory.StoryId }, resultDto);
        }

        // PUT: api/stories/5
        // Cập nhật truyện
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStory(int id, StoryUpdateDto storyDto)
        {
            // Lấy truyện VÀ các tag hiện tại của nó
            var story = await _context.Stories
                .Include(s => s.StoryTags) // QUAN TRỌNG: Lấy các tag cũ
                .FirstOrDefaultAsync(s => s.StoryId == id);

            if (story == null || story.IsDeleted)
            {
                return NotFound("Không tìm thấy truyện.");
            }

            // Kiểm tra GenreId
            if (storyDto.GenreId != story.GenreId &&
                !await _context.Genres.AnyAsync(g => g.GenreId == storyDto.GenreId && !g.IsDeleted))
            {
                return BadRequest("GenreId không hợp lệ.");
            }

            // Cập nhật các trường
            story.Title = storyDto.Title;
            story.Author = storyDto.Author;
            story.Description = storyDto.Description;
            story.CoverImage = storyDto.CoverImage;
            story.Status = storyDto.Status;
            story.GenreId = storyDto.GenreId;
            story.UpdatedAt = DateTime.UtcNow;

            // Xử lý cập nhật Tags (Logic: Xóa hết tag cũ, thêm tag mới)
            _context.StoryTags.RemoveRange(story.StoryTags); // Xóa hết tag cũ

            if (storyDto.TagIds != null && storyDto.TagIds.Any())
            {
                var newStoryTags = new List<StoryTag>();
                foreach (var tagId in storyDto.TagIds.Distinct())
                {
                    if (await _context.Tags.AnyAsync(t => t.TagId == tagId && !t.IsDeleted))
                    {
                        newStoryTags.Add(new StoryTag { StoryId = id, TagId = tagId });
                    }
                }
                _context.StoryTags.AddRange(newStoryTags); // Thêm list tag mới
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Stories.Any(e => e.StoryId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // 204
        }

        // DELETE: api/stories/5
        // Xóa (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStory(int id)
        {
            var story = await _context.Stories.FindAsync(id);
            if (story == null)
            {
                return NotFound("Không tìm thấy truyện.");
            }

            if (story.IsDeleted)
            {
                return BadRequest("Truyện này đã bị xóa rồi.");
            }

            story.IsDeleted = true;
            _context.Entry(story).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        // GET: api/stories/latest
        // Lấy truyện mới cập nhật (có phân trang)
        [HttpGet("latest")]
        public async Task<ActionResult<IEnumerable<StoryDto>>> GetLatestUpdatedStories(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 20;
            if (limit > 100) limit = 100; // Bảo vệ server

            var offset = (page - 1) * limit;

            try
            {
                var query = GetStoriesAsQueryable(); // 1. Lấy query gốc
                var stories = await query
                    .OrderByDescending(s => s.UpdatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync(); // 2. Rút data

                return Ok(stories.Select(MapToDto).ToList()); // 3. Map (dùng C#)
            }
            catch (Exception ex)
            {
                // (Log lỗi 'ex' nếu cần)
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ nội bộ." });
            }
        }
    }
}