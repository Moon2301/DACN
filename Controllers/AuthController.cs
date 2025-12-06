using DACN.Data;
using DACN.Dtos;
using DACN.Helpers;
using DACN.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("google-login")]
        public async Task<ActionResult<AuthResponseDto>> GoogleLogin(GoogleLoginDto googleLoginDto)
        {
            GoogleJsonWebSignature.Payload payload;

            try
            {
                // 1. Xác thực ID Token Google
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    // Lấy ClientId từ appsettings.json
                    Audience = new[] { _configuration["Google:ClientId"] }
                };

                // Hàm ValidateAsync sẽ kiểm tra tính hợp lệ, chữ ký và thời hạn của Token
                payload = await GoogleJsonWebSignature.ValidateAsync(googleLoginDto.IdToken, validationSettings);
            }
            catch (InvalidJwtException)
            {
                // Trả về lỗi nếu Token không hợp lệ
                return BadRequest("ID Token Google không hợp lệ hoặc đã hết hạn.");
            }

            // 2. Tìm kiếm User theo Email (được trích xuất từ Payload)
            var email = payload.Email;
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            // 3. Xử lý User: Đăng ký (nếu chưa có) hoặc Đăng nhập
            if (user == null)
            {
                // Tạo Username ngẫu nhiên và kiểm tra trùng lặp
                var random = new Random();
                string baseUsername = payload.Name.Replace(" ", "").ToLower();
                string uniqueUsername = baseUsername;

                // Vòng lặp đảm bảo Username là duy nhất
                while (await _context.Users.AnyAsync(u => u.Username == uniqueUsername))
                {
                    uniqueUsername = baseUsername + random.Next(100, 999);
                }

                // Tạo User mới
                user = new User
                {
                    Username = uniqueUsername,
                    Email = email,
                    // Đặt mật khẩu giả vì Google Login không dùng mật khẩu
                    PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()),
                    AvatarUrl = payload.Picture, // Lấy ảnh đại diện từ Google
                    Role = UserRole.User,
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else if (user.IsBanned)
            {
                return StatusCode(403, "Tài khoản của bạn đã bị khóa.");
            }

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

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized("Không tìm thấy thông tin xác thực.");

            int userId = int.Parse(userIdClaim.Value);

            // In ra tất cả các claim đang có trong User
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Không tìm thấy người dùng.");
            // 1. Đếm số chương đã đọc (trong bảng lịch sử đọc)
            int readCount = await _context.ChapterReadedByUsers
                                    .CountAsync(x => x.UserId == userId);

            // 2. Đếm số truyện đã đăng (do user này upload và chưa bị xóa)
            int uploadedCount = await _context.Stories
                                    .CountAsync(s => s.UploadedByUserId == userId && !s.IsDeleted);

            var profileDto = new UserProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                Money = user.Money,
                ActivePoint = user.ActivePoint,
                Role = user.Role.ToString(),

                TotalChaptersRead = readCount,
                TotalStoriesUploaded = uploadedCount
            };

            return Ok(profileDto);
        }

        // 2. API SỬA THÔNG TIN CÁ NHÂN (PUT)
        [HttpPut("profile")]
        [Authorize] 
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            // Lấy UserID từ Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized("Không tìm thấy thông tin xác thực.");

            int userId = int.Parse(userIdClaim.Value);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            if (request.Email != null) user.Email = request.Email;
            if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
            if (request.DateOfBirth != null) user.DateOfBirth = request.DateOfBirth;
            if (request.Bio != null) user.Bio = request.Bio;
            if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;

            // Lưu vào DB
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật thông tin thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lưu dữ liệu: " + ex.Message });
            }
        }

    }
}