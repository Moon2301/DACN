using DACN.Data;
using DACN.Dtos;
using DACN.Helpers;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // Hàm helper để map User -> UserDto
        private static UserDto MapUserToDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Bio = user.Bio,
                Role = user.Role,
                AvatarUrl = user.AvatarUrl,
                Money = user.Money,
                ActivePoint = user.ActivePoint,
                Ticket = user.Ticket,
                CreatedAt = user.CreatedAt,
                IsBanned = user.IsBanned,
                BannedUntil = user.BannedUntil
            };
        }

        // GET: api/users
        // Lấy tất cả user (chưa bị xóa)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users
                .Where(u => u.IsDeleted == false)
                .Select(u => MapUserToDto(u)) // Dùng hàm map
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/5
        // Lấy một user theo ID (đây chính là "lấy theo userid")
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Where(u => u.IsDeleted == false && u.UserId == id)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            return Ok(MapUserToDto(user)); // Trả về DTO
        }

        // POST: api/users
        // Tạo một user mới
        [HttpPost]
        public async Task<ActionResult<UserDto>> PostUser(UserCreateDto userDto)
        {
            // Kiểm tra trùng lặp
            if (await _context.Users.AnyAsync(u => u.Username == userDto.Username))
            {
                return BadRequest("Username đã tồn tại.");
            }
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                return BadRequest("Email đã tồn tại.");
            }

            var newUser = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PasswordHash = PasswordHelper.HashPassword(userDto.Password), 
                Role = userDto.Role,
                // Các trường khác sẽ dùng giá trị mặc định trong model
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var resultDto = MapUserToDto(newUser);

            return CreatedAtAction(nameof(GetUser), new { id = resultDto.UserId }, resultDto);
        }

        // PUT: api/users/5
        // Cập nhật thông tin user
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UserUpdateDto userDto)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null || user.IsDeleted)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            // Cập nhật các trường
            user.Email = userDto.Email;
            user.PhoneNumber = userDto.PhoneNumber;
            user.DateOfBirth = userDto.DateOfBirth;
            user.Bio = userDto.Bio;
            user.Role = userDto.Role;
            user.AvatarUrl = userDto.AvatarUrl;
            user.IsBanned = userDto.IsBanned;
            user.BannedUntil = userDto.BannedUntil;
            // Không cập nhật Username hoặc Password ở đây

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == id))
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

        // DELETE: api/users/5
        // Xóa (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            if (user.IsDeleted)
            {
                return BadRequest("Người dùng này đã bị xóa rồi.");
            }

            // Soft Delete
            user.IsDeleted = true;
            _context.Entry(user).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}