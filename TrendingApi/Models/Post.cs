namespace TrendingApi.Models
{
    public class Post
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public long Time { get; set; } // Unix timestamp
    }
}