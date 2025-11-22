using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenresController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Tiêm (Inject) AppDbContext vào
        public GenresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/genres
        // Lấy tất cả thể loại (chưa bị xóa)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GenreDto>>> GetGenres()
        {
            var genres = await _context.Genres
                .Where(g => g.IsDeleted == false)
                .Select(g => new GenreDto
                {
                    GenreId = g.GenreId,
                    Name = g.Name,
                    // THÊM LOGIC TÍNH BOOKCOUNT VÀO ĐÂY:
                    BookCount = g.Stories.Count(b => b.IsDeleted == false)
                })
                .ToListAsync();

            return Ok(genres);
        }

        [HttpGet("deleted")]
        public async Task<ActionResult<IEnumerable<GenreDto>>> GetDeletedGenres()
        {
            var genres = await _context.Genres
                // 🚨 CHỈ LẤY NHỮNG GENRE ĐÃ BỊ XÓA
                .Where(g => g.IsDeleted == true)
                .Select(g => new GenreDto
                {
                    GenreId = g.GenreId,
                    Name = g.Name,
                    BookCount = g.Stories.Count(b => b.IsDeleted == false)
                })
                .ToListAsync();

            return Ok(genres);
        }

        // GET: api/genres/5
        // Lấy một thể loại theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<GenreDto>> GetGenre(int id)
        {
            var genre = await _context.Genres
                .Where(g => g.IsDeleted == false && g.GenreId == id)
                .Select(g => new GenreDto
                {
                    GenreId = g.GenreId,
                    Name = g.Name
                })
                .FirstOrDefaultAsync();

            if (genre == null)
            {
                return NotFound("Không tìm thấy thể loại.");
            }

            return Ok(genre);
        }

        // POST: api/genres
        // Tạo một thể loại mới
        [HttpPost]
        public async Task<ActionResult<GenreDto>> PostGenre(GenreCreateDto genreDto)
        {
            var newGenre = new Genre
            {
                Name = genreDto.Name,
                IsDeleted = false // Mặc định khi tạo
            };

            _context.Genres.Add(newGenre);
            await _context.SaveChangesAsync();

            // Trả về DTO của đối tượng vừa tạo
            var resultDto = new GenreDto
            {
                GenreId = newGenre.GenreId,
                Name = newGenre.Name
            };

            // Trả về 201 Created cùng với đối tượng vừa tạo
            return CreatedAtAction(nameof(GetGenre), new { id = resultDto.GenreId }, resultDto);
        }

        // PUT: api/genres/5
        // Cập nhật một thể loại
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGenre(int id, GenreCreateDto genreDto)
        {
            var genre = await _context.Genres.FindAsync(id);

            if (genre == null || genre.IsDeleted)
            {
                return NotFound("Không tìm thấy thể loại.");
            }

            genre.Name = genreDto.Name;

            _context.Entry(genre).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Genres.Any(e => e.GenreId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // 204 No Content - Báo thành công
        }

        // DELETE: api/genres/5
        // Xóa (thực ra là "Soft Delete")
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                return NotFound("Không tìm thấy thể loại.");
            }

            if (genre.IsDeleted)
            {
                return BadRequest("Thể loại này đã bị xóa rồi.");
            }

            // Đây là Soft Delete
            genre.IsDeleted = true;
            _context.Entry(genre).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
    }
}