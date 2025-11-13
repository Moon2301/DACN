using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DACN.Controllers
{
    [Route("api/daily-reward")]
    [ApiController]
    public class DailyRewardController : ControllerBase
    {
        private readonly AppDbContext _context;

        // --- CẤU HÌNH LOGIC CỦA BẠN ---

        // Múi giờ Việt Nam (UTC+7)
        private const int TimeZoneOffset = 7;

        // Quà tuần (ngày 1 -> 7)
        private static readonly Dictionary<int, int> WeeklyRewards = new Dictionary<int, int>
        {
            { 1, 5 },    // Ngày 1
            { 2, 10 },   // Ngày 2
            { 3, 15 },   // Ngày 3
            { 4, 20 },   // ...
            { 5, 25 },
            { 6, 30 },
            { 7, 50 }    // Ngày 7 thưởng to
        };

        // Quà mốc tháng
        // (Bạn ghi 100-150-200, tôi giả định mốc 30 là 300)
        private static readonly Dictionary<int, int> MilestoneRewards = new Dictionary<int, int>
        {
            { 7, 100 },
            { 14, 150 },
            { 21, 200 },
            { 30, 300 }
        };

        // --- HÀM HELPER ---

        // Lấy ngày giờ hiện tại theo Múi Giờ VN
        private DateTime GetCurrentServerTime() => DateTime.UtcNow.AddHours(TimeZoneOffset);

        // Lấy ngày Thứ 2 đầu tuần
        private DateTime GetStartOfWeek(DateTime today)
        {
            // DayOfWeek của .NET: Sunday = 0, Monday = 1... Saturday = 6
            // Chúng ta muốn Monday = 0... Sunday = 6
            int diff = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;
            return today.AddDays(-diff).Date;
        }

        public DailyRewardController(AppDbContext context)
        {
            _context = context;
        }

        // --- API 1: LẤY TRẠNG THÁI ---
        [HttpGet("status/{userId}")]
        public async Task<ActionResult<DailyRewardStatusDto>> GetRewardStatus(int userId)
        {
            var today = GetCurrentServerTime();
            var startOfWeek = GetStartOfWeek(today);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Lấy TẤT CẢ bản ghi điểm danh trong tuần này
            var weeklyCheckIns = await _context.DailyRewards
                .Where(r => r.UserId == userId && r.RewardDate >= startOfWeek && r.RewardDate <= today)
                .Select(r => r.RewardDate) // Chỉ cần ngày
                .ToListAsync();

            // Xây dựng mảng 7 ngày [true, false, ...]
            var weeklyProgress = new List<bool>(7);
            for (int i = 0; i < 7; i++)
            {
                // Ngày Thứ 2 là startOfWeek.AddDays(0), ...
                bool hasCheckedIn = weeklyCheckIns.Any(d => d.Date == startOfWeek.AddDays(i).Date);
                weeklyProgress.Add(hasCheckedIn);
            }

            // Lấy TẤT CẢ bản ghi trong tháng
            var monthlyCheckIns = await _context.DailyRewards
                .Where(r => r.UserId == userId && r.RewardDate >= startOfMonth && r.RewardDate <= today)
                .Select(r => new { r.RewardDate, r.ActivePointAmount })
                .ToListAsync();

            var achievedMilestones = MilestoneRewards
                .Where(milestone => monthlyCheckIns.Any(reward => reward.ActivePointAmount == milestone.Value))
                .Select(milestone => milestone.Key)
                .ToList();

            var status = new DailyRewardStatusDto
            {
                WeeklyProgress = weeklyProgress,
                HasCheckedInToday = weeklyCheckIns.Any(d => d.Date == today.Date),
                MonthlyTotal = monthlyCheckIns.Count,
                MilestonesAchieved = achievedMilestones
            };

            return Ok(status);
        }

        // --- API 2: ĐIỂM DANH ---
        [HttpPost("check-in")]
        public async Task<ActionResult<DailyRewardCheckInResultDto>> CheckIn(DailyRewardCheckInDto checkInDto)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var today = GetCurrentServerTime();

                var user = await _context.Users.FindAsync(checkInDto.UserId);
                if (user == null || user.IsDeleted)
                    return NotFound("Không tìm thấy người dùng.");

                // 1. Kiểm tra đã điểm danh hôm nay chưa
                bool hasCheckedInToday = await _context.DailyRewards
                    .AnyAsync(r => r.UserId == checkInDto.UserId && r.RewardDate.Date == today.Date);

                if (hasCheckedInToday)
                    return BadRequest("Hôm nay bạn đã điểm danh rồi.");

                int totalReward = 0;
                int weeklyReward = 0;
                int milestoneReward = 0;

                // 2. Xử lý Quà Tuần
                var startOfWeek = GetStartOfWeek(today);

                // Đếm số ngày đã điểm danh TRƯỚC HÔM NAY trong tuần
                int weeklyStreak = await _context.DailyRewards
                    .CountAsync(r => r.UserId == checkInDto.UserId && r.RewardDate >= startOfWeek && r.RewardDate < today.Date);

                // Ngày điểm danh của tuần này = streak + 1 (vì hôm nay là ngày mới)
                int currentStreakDay = weeklyStreak + 1;

                weeklyReward = WeeklyRewards[currentStreakDay];
                totalReward += weeklyReward;

                // Ghi log cho quà tuần
                var weeklyLog = new DailyReward
                {
                    UserId = checkInDto.UserId,
                    RewardDate = today.Date,
                    ActivePointAmount = weeklyReward
                };
                _context.DailyRewards.Add(weeklyLog);

                // 3. Xử lý Quà Tháng
                var startOfMonth = new DateTime(today.Year, today.Month, 1);

                // Đếm số ngày đã điểm danh TRƯỚC HÔM NAY trong tháng
                int monthlyCount = await _context.DailyRewards
                    .CountAsync(r => r.UserId == checkInDto.UserId && r.RewardDate >= startOfMonth && r.RewardDate < today.Date);

                // Tổng số ngày (tính cả hôm nay)
                int currentMonthlyTotal = monthlyCount + 1;

                // Kiểm tra xem có trúng mốc nào không (7, 14, 21, 30)
                if (MilestoneRewards.ContainsKey(currentMonthlyTotal))
                {
                    milestoneReward = MilestoneRewards[currentMonthlyTotal];
                    totalReward += milestoneReward;

                    // Ghi log *riêng* cho quà mốc (để API Status nhận diện)
                    var milestoneLog = new DailyReward
                    {
                        UserId = checkInDto.UserId,
                        RewardDate = today.Date, // Vẫn là ngày hôm nay
                        ActivePointAmount = milestoneReward
                    };
                    _context.DailyRewards.Add(milestoneLog);
                }

                // 4. Cộng điểm cho User
                user.ActivePoint += totalReward;

                // 5. Lưu tất cả
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok(new DailyRewardCheckInResultDto
                {
                    Message = "Điểm danh thành công!",
                    WeeklyReward = weeklyReward,
                    MilestoneReward = milestoneReward,
                    TotalReward = totalReward
                });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return StatusCode(500, "Đã xảy ra lỗi, vui lòng thử lại." + ex.Message);
            }
        }
    }
}