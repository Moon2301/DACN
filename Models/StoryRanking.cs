using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN.Models
{
    // Model này sẽ lưu TẤT CẢ các loại bảng xếp hạng
    public class StoryRanking
    {
        [Key]
        public int StoryRankingId { get; set; }

        // Loại BXH (quan trọng nhất)
        public RankingType Type { get; set; }

        // Dùng cho BXH thể loại, = null nếu là "Tất cả"
        public int? GenreId { get; set; }

        // Vị trí 1, 2, 3...
        public int Rank { get; set; }

        // Truyện nào?
        public int StoryId { get; set; }
        [ForeignKey("StoryId")]
        public Story Story { get; set; }

        // Dữ liệu "phi chuẩn hóa" (denormalized) để API gọi cho nhanh
        // (Không cần JOIN bảng Story khi lấy BXH)
        public string StoryTitle { get; set; }
        public string Author { get; set; }
        public string GenreName { get; set; }
        public string CoverImage { get; set; }

        // Điểm số (số lượt đọc, số vé)
        public int Score { get; set; }

        // Job này chạy lúc nào
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    // Enum để định nghĩa các loại BXH
    public enum RankingType
    {
        READS_DAY_ALL,
        READS_WEEK_ALL,
        READS_MONTH_ALL,

        READS_DAY_GENRE,
        READS_WEEK_GENRE,
        READS_MONTH_GENRE,

        TICKETS_MONTH_ALL,
        RATING_ALL,         
        FOLLOWERS_ALL,   
        READS_ALL_TIME_ALL  
    }
}