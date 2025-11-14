using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting; // Thư viện "biết" wwwroot ở đâu
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.Authorization; // << BẮT BUỘC
using System.Security.Claims; // << BẮT BUỘC (để đọc UserId từ token)

namespace DACN.Controllers.System
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public UploadController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        // POST: api/upload/image
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            // 1. Kiểm tra file cơ bản
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Không có file nào được gửi." });
            }

            // 2. Kiểm tra loại file (chỉ cho phép ảnh)
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
            {
                return BadRequest(new { success = false, message = "Loại file không hợp lệ. Chỉ chấp nhận JPG, PNG, GIF." });
            }

            try
            {
                // --- LOGIC MỚI CỦA BỒ ---

                // 3. Lấy UserId từ Token (Nhờ [Authorize])
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc không tìm thấy UserId." });
                }

                // 4. Lấy đường dẫn "gốc" của wwwroot
                // Ví dụ: "C:/project/DACN/wwwroot"
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                // 5. Tạo thư mục lưu (theo logic MỚI)
                // Ví dụ: "wwwroot/images/2" (với UserId = 2)
                string savePath = Path.Combine(wwwRootPath, "images", userIdString);

                // 6. Nếu thư mục "images/2" chưa tồn tại, tạo nó
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                // 7. Tạo tên file ĐỘC NHẤT
                string fileExtension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}"; // Ví dụ: "a1b2c3d4-....jpg"

                // 8. Đường dẫn đầy đủ để lưu file
                // Ví dụ: "C:/project/DACN/wwwroot/images/2/a1b2c3d4-....jpg"
                string fullPath = Path.Combine(savePath, uniqueFileName);

                // 9. Lưu file vào ổ đĩa server
                await using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // 10. Trả về ĐƯỜNG DẪN TƯƠNG ĐỐI (cho client lưu)
                // Cái này sẽ được UrlHelper ghép với BaseUrl khi LẤY RA
                var relativePath = $"/images/{userIdString}/{uniqueFileName}";

                return Ok(new { success = true, relativePath = relativePath });
            }
            catch (Exception ex)
            {
                // (Nên log lỗi 'ex' ra)
                return StatusCode(500, new { success = false, message = "Lỗi server khi upload file." });
            }
        }
    }
}