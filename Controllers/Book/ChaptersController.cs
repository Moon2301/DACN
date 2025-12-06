using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using DACN.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using System.Security.Claims;

namespace DACN.Controllers
{
    [Route("api/chapters")] // Route CƠ BẢN cho C-R-U-D theo ID
    [ApiController]
    public class ChaptersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChaptersController(AppDbContext context )
        {
            _context = context;
        }

        // GET: api/stories/5/chapters
        [HttpGet("/api/stories/{storyId}/chapters")]
        public async Task<ActionResult<IEnumerable<ChapterListItemDto>>> GetChaptersForStory(int storyId)
        {
            // 1. Lấy thông tin người dùng hiện tại
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int currentUserId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            bool isAdmin = User.IsInRole("Admin");

            // 2. Lấy truyện để check chủ sở hữu
            var story = await _context.Stories.FindAsync(storyId);
            if (story == null || story.IsDeleted) return NotFound("Không tìm thấy truyện.");

            bool isOwner = (currentUserId != 0 && story.UploadedByUserId == currentUserId);

            // 3. Query cơ bản
            var query = _context.Chapters
                .Where(c => c.StoryId == storyId && !c.IsDeleted);

            // 4. LOGIC LỌC (QUAN TRỌNG)
            // Nếu KHÔNG phải Admin VÀ KHÔNG phải Chủ truyện -> Lọc bỏ Vi phạm
            if (!isAdmin && !isOwner)
            {
                // Chỉ cho xem Safe và Unchecked (tùy chính sách của bạn)
                query = query.Where(c => c.Status != ChapterStatus.Violation);
            }
            // Nếu là Admin/Owner -> Code sẽ chạy thẳng xuống dưới (Lấy tất cả)

            var chapters = await query
                .OrderBy(c => c.ChapterNumber)
                .Select(c => new ChapterListItemDto
                {
                    ChapterId = c.ChapterId,
                    ChapterNumber = c.ChapterNumber,
                    Title = c.Title,
                    IsVip = c.IsVip,
                    CreatedAt = c.CreatedAt,
                    // Bạn nên thêm field Status vào DTO này để Frontend hiển thị màu đỏ nếu là Violation
                    // Status = c.Status 
                })
                .ToListAsync();

            return Ok(chapters);
        }

        // POST: api/stories/5/chapters
        [Authorize]
        [HttpPost("/api/stories/{storyId}/chapters")]
        public async Task<ActionResult<ChapterDetailDto>> PostChapter(int storyId, ChapterCreateUpdateDto chapterDto)
        {
            var story = await _context.Stories.FindAsync(storyId);
            if (story == null || story.IsDeleted) return NotFound("Không tìm thấy truyện.");

            // 1. Check quyền Admin
            bool isAdmin = User.IsInRole("Admin");

            // Xử lý xuống dòng
            string finalContent = chapterDto.Content;
            if (!string.IsNullOrEmpty(finalContent))
            {
                finalContent = finalContent.Replace("/n", "\n").Replace("\\n", "\n");
            }

            var newChapter = new Chapter
            {
                StoryId = storyId,
                ChapterNumber = chapterDto.ChapterNumber,
                Title = chapterDto.Title,
                Content = finalContent,

                // Logic trạng thái: Admin -> Duyệt luôn; User -> Chờ duyệt
                Status = isAdmin ? ChapterStatus.Safe : ChapterStatus.Unchecked,

                IsVip = chapterDto.IsVip,
                VipUnlockAt = chapterDto.VipUnlockAt,
                UnlockPriceMoney = chapterDto.UnlockPriceMoney,
                UnlockPriceActivePoint = chapterDto.UnlockPriceActivePoint,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Cập nhật thông tin truyện
            story.UpdatedAt = DateTime.UtcNow;
            story.TotalChapters += 1;

            _context.Chapters.Add(newChapter);
            await _context.SaveChangesAsync();

            // Tạo DTO trả về (Sửa lỗi ở đây: khai báo rõ ràng biến resultDto)
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

            // Trả về kết quả
            return CreatedAtAction(nameof(GetChapter), new { id = newChapter.ChapterId }, resultDto);
        }

        // --- CÁC ROUTE CRUD THEO ID CHƯƠNG ---

        // GET: api/chapters/5
        // Lấy chi tiết 1 chương (CÓ CONTENT)
        [HttpGet("{id}")]
        public async Task<ActionResult<ChapterDetailDto>> GetChapter(int id)
        {
            var currentChapter = await _context.Chapters
                .AsNoTracking()
                .Include(c => c.Story) // Include để check Owner
                .FirstOrDefaultAsync(c => c.ChapterId == id && !c.IsDeleted);

            if (currentChapter == null) return NotFound("Không tìm thấy chương.");

            // 1. Check quyền
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int currentUserId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = (currentUserId != 0 && currentChapter.Story.UploadedByUserId == currentUserId);

            // 2. CHECK TRẠNG THÁI
            if (currentChapter.Status == ChapterStatus.Violation)
            {
                // Nếu là Vi phạm, chỉ Admin hoặc Chủ truyện mới được xem
                if (!isAdmin && !isOwner)
                {
                    return NotFound("Nội dung này đã bị gỡ do vi phạm tiêu chuẩn cộng đồng.");
                }
            }

            var prevQuery = _context.Chapters.Where(c => c.StoryId == currentChapter.StoryId && c.ChapterNumber < currentChapter.ChapterNumber && !c.IsDeleted);
            var nextQuery = _context.Chapters.Where(c => c.StoryId == currentChapter.StoryId && c.ChapterNumber > currentChapter.ChapterNumber && !c.IsDeleted);

            if (!isAdmin && !isOwner)
            {
                prevQuery = prevQuery.Where(c => c.Status != ChapterStatus.Violation);
                nextQuery = nextQuery.Where(c => c.Status != ChapterStatus.Violation);
            }

            var prevChapterId = await prevQuery.OrderByDescending(c => c.ChapterNumber).Select(c => c.ChapterId).FirstOrDefaultAsync();
            var nextChapterId = await nextQuery.OrderBy(c => c.ChapterNumber).Select(c => c.ChapterId).FirstOrDefaultAsync();

            // 4. Trả về DTO
            var chapterDto = new ChapterDetailDto
            {
                ChapterId = currentChapter.ChapterId,
                StoryId = currentChapter.StoryId,
                Title = currentChapter.Title,
                Content = currentChapter.Content,
                ChapterNumber = currentChapter.ChapterNumber,
                CreatedAt = currentChapter.CreatedAt,
                PreviousChapterId = (prevChapterId == 0) ? null : prevChapterId,
                NextChapterId = (nextChapterId == 0) ? null : nextChapterId
            };

            return Ok(chapterDto);
        }

        // Cập nhật 1 chương
        // PUT: api/chapters/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChapter(int id, ChapterCreateUpdateDto chapterDto)
        {
            // Include Story để lấy UploadedByUserId kiểm tra quyền
            var chapter = await _context.Chapters
                .Include(c => c.Story)
                .FirstOrDefaultAsync(c => c.ChapterId == id);

            if (chapter == null || chapter.IsDeleted) return NotFound();

            // 1. Lấy User ID hiện tại
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int currentUserId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            bool isAdmin = User.IsInRole("Admin");

            //SECURITY: Chỉ cho phép Chủ truyện hoặc Admin sửa
            if (!isAdmin && chapter.Story.UploadedByUserId != currentUserId)
            {
                return Forbid(); // Trả về 403 Forbidden
            }

            // 2. FIX TEXT
            string finalContent = chapterDto.Content;
            if (!string.IsNullOrEmpty(finalContent))
            {
                finalContent = finalContent.Replace("/n", "\n").Replace("\\n", "\n");
            }

            // Cập nhật
            chapter.ChapterNumber = chapterDto.ChapterNumber;
            chapter.Title = chapterDto.Title;
            chapter.Content = finalContent; 
            chapter.IsVip = chapterDto.IsVip;
            chapter.VipUnlockAt = chapterDto.VipUnlockAt;
            chapter.UnlockPriceMoney = chapterDto.UnlockPriceMoney;
            chapter.UnlockPriceActivePoint = chapterDto.UnlockPriceActivePoint;

            // Logic trạng thái
            if (isAdmin)
            {
                chapter.Status = ChapterStatus.Safe;
            }
            else
            {
                chapter.Status = ChapterStatus.Unchecked;
            }

            // Update Story time
            var story = chapter.Story; 
            story.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/chapters/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Story) // Include để check chủ sở hữu
                .FirstOrDefaultAsync(c => c.ChapterId == id);

            if (chapter == null) return NotFound("Không tìm thấy chương.");
            if (chapter.IsDeleted) return BadRequest("Chương này đã bị xóa rồi.");

            // 1. Lấy User ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int currentUserId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            bool isAdmin = User.IsInRole("Admin");

            // FIX SECURITY: Check quyền sở hữu
            if (!isAdmin && chapter.Story.UploadedByUserId != currentUserId)
            {
                return Forbid();
            }

            chapter.IsDeleted = true;

            // Update Story
            var story = chapter.Story;
            story.TotalChapters = Math.Max(0, story.TotalChapters - 1);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/chapters/admin/filter
        // API lấy danh sách chương có lọc (Dành cho Admin)
        [HttpGet("admin/filter")]
        [Authorize(Roles = "Admin")] // Chỉ Admin được gọi
        public async Task<ActionResult> GetChaptersForAdmin(
            [FromQuery] int? storyId,           // Lọc theo truyện (nếu cần)
            [FromQuery] int? status,  // Lọc theo trạng thái (Unchecked, Safe, Violation)
            [FromQuery] bool? isDeleted,        // Lọc theo đã xóa hay chưa
            [FromQuery] int page = 1,           // Phân trang
            [FromQuery] int pageSize = 20)      // Số lượng mỗi trang
        {
            // 1. Khởi tạo Query
            var query = _context.Chapters
                .AsNoTracking()
                .Include(c => c.Story) // Join bảng Story để lấy tên truyện
                .AsQueryable();

            // 2. Áp dụng các bộ lọc (Filter)

            // Lọc theo StoryId (nếu có truyền lên)
            if (storyId.HasValue && storyId != 0)
            {
                query = query.Where(c => c.StoryId == storyId.Value);
            }

            // Lọc theo Status (nếu có truyền lên)
            if (status.HasValue && status <=2 && status >= 0)
            {               
                var enumStatus = (ChapterStatus)status.Value;
                query = query.Where(c => c.Status == enumStatus);
            }

            // Lọc theo IsDeleted (nếu có truyền lên)
            if (isDeleted.HasValue)
            {
                query = query.Where(c => c.IsDeleted == isDeleted.Value);
            }

            // 3. Tính toán phân trang
            int totalRecords = await query.CountAsync();

            var chapters = await query
                .OrderByDescending(c => c.CreatedAt) 
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.ChapterId,
                    c.ChapterNumber,
                    c.Title,
                    c.Status,    
                    c.IsDeleted,   
                    c.CreatedAt,
                    c.StoryId,
                    storyTitle = c.Story.Title,
                    uploaderId = c.Story.UploadedByUserId,
                    c.IsVip,
                    c.UnlockPriceMoney,
                    c.UnlockPriceActivePoint

                })
                .ToListAsync();

            // 4. Trả về kết quả kèm thông tin phân trang
            return Ok(new
            {
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = chapters
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("approve/{id}")] 
        public async Task<IActionResult> ApproveChapter(int id)
        {

            var chapter = await _context.Chapters
                .Include(c => c.Story)
                .FirstOrDefaultAsync(c => c.ChapterId == id);

            if (chapter == null || chapter.IsDeleted) return NotFound("Không tìm thấy chương.");

            
            chapter.Status = ChapterStatus.Safe;
            chapter.IsDeleted = false;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("reject/{id}")] 
        public async Task<IActionResult> RejectChapter(int id)
        {

            var chapter = await _context.Chapters
                .Include(c => c.Story)
                .FirstOrDefaultAsync(c => c.ChapterId == id);

            if (chapter == null || chapter.IsDeleted) return NotFound("Không tìm thấy chương.");


            chapter.Status = ChapterStatus.Violation;
            chapter.IsDeleted = true;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("restore/{id}")]
        public async Task<IActionResult> RestoreChapter(int id)
        {

            var chapter = await _context.Chapters
                .Include(c => c.Story)
                .FirstOrDefaultAsync(c => c.ChapterId == id);

            if (chapter == null) return NotFound("Không tìm thấy chương.");


            chapter.IsDeleted = false;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}