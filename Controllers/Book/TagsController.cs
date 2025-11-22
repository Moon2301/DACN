using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TagsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tags
        // Lấy tất cả tag (chưa bị xóa)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetTags()
        {
            var tags = await _context.Tags
                .Where(t => t.IsDeleted == false)
                .Select(t => new TagDto
                {
                    TagId = t.TagId,
                    Name = t.Name,
                    BookCount = t.StoryTags.Count(b => b.Story.IsDeleted == false)
                })
                .ToListAsync();

            return Ok(tags);
        }

        // GET: api/tags/5
        // Lấy một tag theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<TagDto>> GetTag(int id)
        {
            var tag = await _context.Tags
                .Where(t => t.IsDeleted == false && t.TagId == id)
                .Select(t => new TagDto
                {
                    TagId = t.TagId,
                    Name = t.Name
                })
                .FirstOrDefaultAsync();

            if (tag == null)
            {
                return NotFound("Không tìm thấy tag.");
            }

            return Ok(tag);
        }

        // POST: api/tags
        // Tạo một tag mới
        [HttpPost]
        public async Task<ActionResult<TagDto>> PostTag(TagCreateUpdateDto tagDto)
        {
            var newTag = new Tag
            {
                Name = tagDto.Name,
                IsDeleted = false
            };

            _context.Tags.Add(newTag);
            await _context.SaveChangesAsync();

            var resultDto = new TagDto
            {
                TagId = newTag.TagId,
                Name = newTag.Name
            };

            return CreatedAtAction(nameof(GetTag), new { id = resultDto.TagId }, resultDto);
        }

        // PUT: api/tags/5
        // Cập nhật một tag
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTag(int id, TagCreateUpdateDto tagDto)
        {
            var tag = await _context.Tags.FindAsync(id);

            if (tag == null || tag.IsDeleted)
            {
                return NotFound("Không tìm thấy tag.");
            }

            tag.Name = tagDto.Name;
            _context.Entry(tag).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tags.Any(e => e.TagId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // 204 No Content
        }

        // DELETE: api/tags/5
        // Xóa (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return NotFound("Không tìm thấy tag.");
            }

            if (tag.IsDeleted)
            {
                return BadRequest("Tag này đã bị xóa rồi.");
            }

            tag.IsDeleted = true;
            _context.Entry(tag).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
    }
}