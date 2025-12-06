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
                AvatarUrl = UrlHelper.ResolveImageUrl( user.AvatarUrl),
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

        // API KHÓA / MỞ KHÓA TÀI KHOẢN
        // PUT: api/users/5/lock-status
        [Authorize(Roles = "Admin")] 
        [HttpPut("{id}/lock-status")]
        public async Task<IActionResult> ChangeLockStatus(int id, [FromBody] LockUserDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound("Không tìm thấy người dùng.");

            user.IsBanned = request.IsBanned;

            if (request.IsBanned)
            {
                // Nếu khóa
                if (request.DurationDays.HasValue && request.DurationDays > 0)
                {
                    user.BannedUntil = DateTime.UtcNow.AddDays(request.DurationDays.Value);
                }
                else
                {
                    user.BannedUntil = null; 
                }
            }
            else
            {
                // Nếu mở khóa
                user.BannedUntil = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = request.IsBanned ? "Đã khóa tài khoản thành công." : "Đã mở khóa tài khoản." });
        }

        // API CỘNG/TRỪ SỐ DƯ (MONEY, POINT, TICKET)
        // POST: api/users/5/balance
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/balance")]
        public async Task<IActionResult> AddUserBalance(int id, [FromBody] AddBalanceDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound("Không tìm thấy người dùng.");

            if (request.Amount == 0) return BadRequest("Số lượng phải khác 0.");

            // Xác định loại giao dịch (Cộng hay Trừ)
            var transType = request.Amount > 0 ? TransactionType.Earning : TransactionType.Spending;

            switch (request.Type)
            {
                case BalanceType.Money:
                    // Cập nhật User
                    user.Money += request.Amount;
                    // Tạo lịch sử giao dịch
                    _context.MoneyTransactions.Add(new MoneyTransaction
                    {
                        UserId = id,
                        Amount = Math.Abs(request.Amount), // Lưu số dương
                        Type = transType,
                        CreatedAt = DateTime.UtcNow
                    });
                    break;

                case BalanceType.ActivePoint:
                    user.ActivePoint += request.Amount;
                    _context.ActivePointTransactions.Add(new ActivePointTransaction
                    {
                        UserId = id,
                        Amount = Math.Abs(request.Amount),
                        Type = transType,
                        CreatedAt = DateTime.UtcNow
                    });
                    break;

                case BalanceType.Ticket:
                    user.Ticket += request.Amount;
                    _context.TicketTransactions.Add(new TicketTransaction
                    {
                        UserId = id,
                        Amount = Math.Abs(request.Amount),
                        Type = transType,
                        CreatedAt = DateTime.UtcNow
                    });
                    break;

                default:
                    return BadRequest("Loại tài sản không hợp lệ.");
            }

            if (user.Money < 0) user.Money = 0;
            if (user.ActivePoint < 0) user.ActivePoint = 0;
            if (user.Ticket < 0) user.Ticket = 0;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật số dư thành công!",
                currentBalance = new { user.Money, user.ActivePoint, user.Ticket }
            });
        }

    }
}