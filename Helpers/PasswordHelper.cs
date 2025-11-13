using System.Security.Cryptography;
using System.Text;
using BCrypt.Net; 

namespace DACN.Helpers
{
    public static class PasswordHelper
    {
        // Hash password bằng BCrypt (đã tự động bao gồm salt)
        public static string HashPassword(string password)
        {
            // BCrypt tự tạo salt ngẫu nhiên và nhúng vào hash
            return BCrypt.Net.BCrypt.HashPassword(password); 
        }

        // Hàm kiểm tra khớp password
        public static bool VerifyPassword(string password, string storedHash)
        {
            // BCrypt tự đọc salt từ storedHash và so sánh
            return BCrypt.Net.BCrypt.Verify(password, storedHash); 
        }
    }
}
