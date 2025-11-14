namespace DACN.Helpers
{
    public static class UrlHelper
    {
        private static IConfiguration _config;

        public static void Initialize(IConfiguration config)
        {
            _config = config;
        }

        public static string ResolveImageUrl(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || relativePath.StartsWith("http"))
            {
                return relativePath;
            }

            var baseUrl = _config["BaseUrl"];
            return $"{baseUrl}{relativePath}";
        }
    }
}
