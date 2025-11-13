// Trong /Dtos/DailyRewardDtos.cs
using System.ComponentModel.DataAnnotations;

namespace DACN.Dtos
{
    // DTO để HIỂN THỊ TRẠNG THÁI (User vừa mở app)
    public class DailyRewardStatusDto
    {
        // Danh sách 7 ngày, [true, true, false, ...]
        // Tương ứng (Thứ 2, Thứ 3, Thứ 4, ...)
        public List<bool> WeeklyProgress { get; set; } = new List<bool>();

        // Hôm nay đã điểm danh chưa?
        public bool HasCheckedInToday { get; set; }

        // Tổng số ngày đã điểm danh trong tháng này
        public int MonthlyTotal { get; set; }

        // Các mốc tháng này đã nhận (7, 14, ...)
        public List<int> MilestonesAchieved { get; set; } = new List<int>();
    }

    // DTO để YÊU CẦU điểm danh
    public class DailyRewardCheckInDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ Token
    }

    // DTO KẾT QUẢ trả về sau khi điểm danh
    public class DailyRewardCheckInResultDto
    {
        public string Message { get; set; }
        public int WeeklyReward { get; set; } // Quà chuỗi tuần
        public int MilestoneReward { get; set; } // Quà mốc tháng
        public int TotalReward { get; set; }
    }
}