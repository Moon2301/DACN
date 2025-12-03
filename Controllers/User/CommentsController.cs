using DACN.Data;
using DACN.Dtos;
using DACN.Helpers;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/comments")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        // HÀM HELPER (Đã sửa): Không còn map đệ quy
        private static CommentDto MapCommentToDto(Comment comment)
        {
            return new CommentDto
            {
                CommentId = comment.CommentId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId,
                Username = comment.User?.Username ?? "Tài khoản đã xóa",
                AvatarUrl = UrlHelper.ResolveImageUrl(comment.User?.AvatarUrl),
                ParentCommentId = comment.ParentCommentId,
                RepliesCount = comment.RepliesCount // Lấy từ DB
            };
        }

        // GET: api/stories/{storyId}/comments
        // Lấy comment GỐC (phân trang)
        [HttpGet("/api/stories/{storyId}/comments")]
        public async Task<ActionResult<PaginatedResult<CommentDto>>> GetStoryComments(
            int storyId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8) // Logic 8 comment/page
        {
            var query = _context.Comments
                .Where(c => c.StoryId == storyId && c.ParentCommentId == null); // Chỉ lấy gốc

            // Lấy tổng số (để phân trang)
            var totalCount = await query.CountAsync();

            var comments = await query
                .Include(c => c.User) // Vẫn cần User
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize) // Bỏ qua các trang trước
                .Take(pageSize) // Lấy đúng 8 cái
                .ToListAsync();

            var dtos = comments.Select(MapCommentToDto).ToList();

            var result = new PaginatedResult<CommentDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
            return Ok(result);
        }

        // GET: api/chapters/{chapterId}/comments
        // Lấy comment GỐC của chương (phân trang)
        [HttpGet("/api/chapters/{chapterId}/comments")]
        public async Task<ActionResult<PaginatedResult<CommentDto>>> GetChapterComments(
            int chapterId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8) // 8 comment/page
        {
            var query = _context.Comments
                .Where(c => c.ChapterId == chapterId && c.ParentCommentId == null); // Chỉ lấy comment gốc

            var totalCount = await query.CountAsync();

            var comments = await query
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = comments.Select(MapCommentToDto).ToList();

            var result = new PaginatedResult<CommentDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        // --- API MỚI (CHO LOGIC CỦA BẠN) ---
        // GET: api/comments/5/replies
        // Lấy replies (phân trang) khi user "bấm vào"
        [HttpGet("{parentId}/replies")]
        public async Task<ActionResult<PaginatedResult<CommentDto>>> GetCommentReplies(
            int parentId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5) // Lấy 5 replies 1 lần
        {
            var query = _context.Comments
                .Where(c => c.ParentCommentId == parentId); // Lấy con

            var totalCount = await query.CountAsync();

            var replies = await query
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt) // Replies thì nên xếp từ cũ -> mới
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = replies.Select(MapCommentToDto).ToList();

            var result = new PaginatedResult<CommentDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
            return Ok(result);
        }

        // POST: api/comments
        [HttpPost]
        public async Task<ActionResult<CommentDto>> PostComment(CommentCreateDto commentDto)
        {
            // 👇👇👇 [LOGIC MỚI THÊM VÀO] 👇👇👇
            // Nếu Client gửi ParentCommentId = 0, ta coi như là null (Comment gốc)
            if (commentDto.ParentCommentId == 0)
            {
                commentDto.ParentCommentId = null;
            }
            // 👆👆👆 [KẾT THÚC] 👆👆👆

            // ... (Logic validation giữ nguyên) ...
            if (!await _context.Users.AnyAsync(u => u.UserId == commentDto.UserId && !u.IsDeleted))
                return BadRequest("User không tồn tại.");

            // ... (Phần tạo object newComment giữ nguyên) ...
            var newComment = new Comment
            {
                UserId = commentDto.UserId,
                Content = commentDto.Content,
                CreatedAt = DateTime.UtcNow
            };

            Story storyToUpdate = null;

            // Lúc này ParentCommentId đã chuẩn (null hoặc ID thật > 0)
            if (commentDto.ParentCommentId.HasValue)
            {
                // Đây là 1 REPLY
                var parent = await _context.Comments.FindAsync(commentDto.ParentCommentId.Value);
                if (parent == null)
                    return BadRequest("Bình luận cha không tồn tại.");

                newComment.ParentCommentId = parent.CommentId;
                newComment.StoryId = parent.StoryId;
                newComment.ChapterId = parent.ChapterId;

                parent.RepliesCount += 1; // Tăng số lượng con của cha

                // ... (Logic thông báo nếu có) ...
            }
            else
            {
                // Đây là Comment GỐC
                // Chỉ tăng TotalComments của Story khi là comment GỐC
                if (commentDto.StoryId.HasValue)
                {
                    storyToUpdate = await _context.Stories.FindAsync(commentDto.StoryId.Value);
                    if (storyToUpdate != null) storyToUpdate.TotalComments += 1;

                    // Gán StoryId cho comment mới
                    newComment.StoryId = commentDto.StoryId.Value;
                }
                else if (commentDto.ChapterId.HasValue)
                {
                    var chapter = await _context.Chapters.FindAsync(commentDto.ChapterId.Value);
                    if (chapter != null)
                    {
                        storyToUpdate = await _context.Stories.FindAsync(chapter.StoryId);
                        if (storyToUpdate != null) storyToUpdate.TotalComments += 1;

                        // Gán ChapterId và StoryId (lấy từ chapter) cho comment mới
                        newComment.ChapterId = commentDto.ChapterId.Value;
                        newComment.StoryId = chapter.StoryId;
                    }
                }
            }

            _context.Comments.Add(newComment);
            await _context.SaveChangesAsync();

            // Trả về DTO
            var result = await _context.Comments
                .Include(c => c.User)
                .FirstAsync(c => c.CommentId == newComment.CommentId);

            // Lưu ý: Nếu post vào Chapter thì có thể không có storyId trong result để tạo link GetStoryComments chính xác
            // nhưng thường GetStoryComments chỉ cần storyId là được.
            // Nếu newComment.StoryId có giá trị thì dùng nó.
            return CreatedAtAction(nameof(GetStoryComments), new { storyId = result.StoryId ?? 0 }, MapCommentToDto(result));
        }

        // DELETE: api/comments/5
        // (Đã cập nhật: Giảm RepliesCount)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound();

            // --- LOGIC MỚI: Cập nhật Count ---
            if (comment.ParentCommentId.HasValue)
            {
                // Đây là 1 REPLY
                var parent = await _context.Comments.FindAsync(comment.ParentCommentId.Value);
                if (parent != null)
                {
                    parent.RepliesCount = Math.Max(0, parent.RepliesCount - 1);
                }
            }
            else if (comment.StoryId.HasValue)
            {
                // Đây là comment GỐC
                var story = await _context.Stories.FindAsync(comment.StoryId.Value);
                if (story != null)
                {
                    story.TotalComments = Math.Max(0, story.TotalComments - 1);
                }
            }

            // --- LOGIC ẨN DANH (Giữ nguyên) ---
            comment.Content = "[Bình luận đã bị xóa]";
            _context.Entry(comment).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}