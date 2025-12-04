namespace DACN.Services
{
    public interface IContentModerationService
    {
        /// <summary>
        /// Kiểm tra nội dung có an toàn (safe) để đăng tải hay không.
        /// </summary>
        /// <param name="content">Nội dung (văn bản) cần kiểm duyệt.</param>
        /// <returns>Trả về true nếu nội dung an toàn, false nếu bị chặn.</returns>
        Task<bool> IsContentSafe(string content);
    }
}