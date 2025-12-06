namespace DACN.Services
{
    public interface IContentModerationService
    {
        Task<bool> IsContentSafe(string content);
    }
}