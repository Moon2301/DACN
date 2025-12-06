using System.ComponentModel.DataAnnotations;

namespace DACN.Dtos
{
    // DTO cho /register
    public class RegisterDto
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }

    // DTO cho /login
    public class LoginDto
    {
        [Required]
        public string Username { get; set; } // User có thể login bằng email hoặc username

        [Required]
        public string Password { get; set; }
    }

    // DTO trả về cho client sau khi login/register thành công
    public class AuthResponseDto
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public int Money { get; set; }
        public int ActivePoint { get; set; }
        public int Ticket { get; set; }
        public string AvatarUrl { get; set; }
    }
    public class GoogleLoginDto
    {
        [Required]
        public string IdToken { get; set; } 
    }

    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string DateOfBirth { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public int Money { get; set; }
        public int ActivePoint { get; set; }
        public string Role { get; set; }

        public int TotalChaptersRead { get; set; }     // Tổng số chương đã đọc
        public int TotalStoriesUploaded { get; set; }  // Tổng số truyện đã đăng
    }

    public class UpdateProfileDto
    {
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
    }
}