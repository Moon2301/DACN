// Quan trọng: using BCrypt
using DACN.Data;
using DACN.Dtos;
using DACN.Helpers;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DACN.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            // 1. Kiểm tra User/Email tồn tại
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
                return BadRequest("Username đã tồn tại.");

            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                return BadRequest("Email đã tồn tại.");

            // 2. Băm mật khẩu (Dùng BCrypt)
            string hashedPassword = PasswordHelper.HashPassword(registerDto.Password);

            // 3. Tạo User mới
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = hashedPassword, // Lưu hash, không lưu pass
                Role = UserRole.User, // Mặc định
                // (Các trường khác sẽ lấy Default)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Đăng ký thành công. Vui lòng đăng nhập.");
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            // 1. Tìm User (có thể là Username hoặc Email)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username || u.Email == loginDto.Username);

            if (user == null || user.IsDeleted)
                return Unauthorized("Tài khoản hoặc mật khẩu không chính xác."); // 401

            // 2. Kiểm tra mật khẩu (Dùng BCrypt)
            bool isValidPassword = PasswordHelper.VerifyPassword(loginDto.Password, user.PasswordHash);

            if (!isValidPassword)
                return Unauthorized("Tài khoản hoặc mật khẩu không chính xác."); // 401

            if (user.IsBanned)
                return StatusCode(403, "Tài khoản của bạn đã bị khóa."); // 403 Forbidden

            // 3. Mật khẩu đúng -> Tạo Token
            var authResponse = GenerateJwtToken(user);

            return Ok(authResponse);
        }

        // --- HÀM HELPER TẠO TOKEN ---
        [NonAction] // Để Swagger không thấy
        private AuthResponseDto GenerateJwtToken(User user)
        {
            // Lấy thông tin từ appsettings
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expireDays = int.Parse(jwtSettings["ExpireDays"]);

            // 1. Tạo "Claims" (Thông tin trong token)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()), // Subject (ID của user)
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // ID (dùng cho [Authorize])
                new Claim(ClaimTypes.Name, user.Username), // Username
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()), // Quyền (User, Admin)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ID của riêng token này
            };

            // 2. Tạo Token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(expireDays),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            // 3. "Viết" token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            // 4. Trả về
            return new AuthResponseDto
            {
                Token = jwtToken,
                Expiration = token.ValidTo,
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role.ToString(),
                Money = user.Money,
                ActivePoint = user.ActivePoint,
                Ticket = user.Ticket,
                AvatarUrl = UrlHelper.ResolveImageUrl(user.AvatarUrl)
            };
        }
    }
}