using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoriesController(AppDbContext context)
        {
            _context = context;
        }

        // HÀM HELPER ĐỂ DÙNG CHUNG
        // Vì query Story DTO rất phức tạp, chúng ta đưa nó vào 1 IQueryable
        private IQueryable<StoryDto> GetStoriesAsDto()
        {
            return _context.Stories
                .Where(s => s.IsDeleted == false)
                .Include(s => s.Genre) // Lấy Genre
                .Include(s => s.UploadedBy) // Lấy User
                .Include(s => s.StoryTags) // Lấy bảng nối
                    .ThenInclude(st => st.Tag) // Từ bảng nối lấy Tag
                .Select(s => new StoryDto
                {
                    StoryId = s.StoryId,
                    Title = s.Title,
                    Author = s.Author,
                    Description = s.Description,
                    CoverImage = s.CoverImage,
                    Status = s.Status,
                    GenreId = s.GenreId,
                    GenreName = s.Genre.Name, // Lấy tên từ Genre
                    UploadedByUserId = s.UploadedByUserId,
                    UploadedByUsername = s.UploadedBy.Username, // Lấy tên từ User
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    // Map danh sách StoryTag -> List<string> Tags
                    Tags = s.StoryTags.Select(st => st.Tag.Name).ToList(),
                    // Lấy các trường thống kê
                    TotalReads = s.TotalReads,
                    AverageRating = s.AverageRating,
                    TotalFollowers = s.TotalFollowers,
                    TotalComments = s.TotalComments
                });
        }


        // GET: api/stories
        // Lấy tất cả truyện
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StoryDto>>> GetStories()
        {
            var stories = await GetStoriesAsDto().ToListAsync();
            return Ok(stories);
        }

        // GET: api/stories/5
        // Lấy 1 truyện theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<StoryDto>> GetStory(int id)
        {
            var story = await GetStoriesAsDto()
                .FirstOrDefaultAsync(s => s.StoryId == id);

            if (story == null)
            {
                return NotFound("Không tìm thấy truyện.");
            }

            return Ok(story);
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

            var stories = await GetStoriesAsDto()
                .Where(s => s.UploadedByUserId == userId)
                .ToListAsync();

            return Ok(stories);
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
            var resultDto = await GetStoriesAsDto().FirstOrDefaultAsync(s => s.StoryId == newStory.StoryId);

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
            // 1. Đảm bảo tham số hợp lệ
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 20;
            if (limit > 100) limit = 100;

            // 2. Tính toán offset (bỏ qua bao nhiêu bản ghi)
            var offset = (page - 1) * limit;

            try
            {
                
                var query = GetStoriesAsDto();

                var stories = await query
                    .OrderByDescending(s => s.UpdatedAt) // Sắp xếp theo UpdatedAt (mới nhất)
                    .Skip(offset)                      // Bỏ qua các trang trước
                    .Take(limit)                       // Lấy số lượng "limit"
                    .ToListAsync();                    // Thực thi query

                // 5. Trả về kết quả
                return Ok(stories);
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
                return StatusCode(500, "Lỗi máy chủ nội bộ. Xin thử lại sau.");
            }
        }
    }
}