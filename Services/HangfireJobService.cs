using DACN.Data;
using DACN.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

// Dịch vụ này sẽ được Hangfire gọi
public class HangfireJobService
{
    private readonly AppDbContext _context;
    private const int TimeZoneOffset = 7; // Múi giờ VN (UTC+7)

    public HangfireJobService(AppDbContext context)
    {
        _context = context;
    }

    // --- HÀM HELPER: Lấy Thứ Hai Đầu Tuần (Giống DailyReward) ---
    private DateTime GetStartOfWeek(DateTime today)
    {
        int diff = (today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1);
        return today.AddDays(-diff).Date;
    }

    // --- HÀM HELPER: Lưu BXH (Tối ưu) ---
    private async Task SaveRankings(List<(int StoryId, int Score)> ranks, RankingType type, int? genreId)
    {
        if (!ranks.Any()) return;

        // Tối ưu: Lấy thông tin 100 truyện 1 lúc (thay vì N+1 query)
        var storyIds = ranks.Select(r => r.StoryId).ToList();
        var storiesData = await _context.Stories
            .Where(s => storyIds.Contains(s.StoryId))
            .Select(s => new { s.StoryId, s.Title, s.Author, s.CoverImage, GenreName = s.Genre.Name })
            .ToDictionaryAsync(s => s.StoryId);

        var rankPosition = 1;
        var generatedAt = DateTime.UtcNow;

        foreach (var rank in ranks)
        {
            if (storiesData.TryGetValue(rank.StoryId, out var story))
            {
                var newRanking = new StoryRanking
                {
                    Type = type,
                    GenreId = genreId,
                    Rank = rankPosition++,
                    StoryId = story.StoryId,
                    StoryTitle = story.Title,
                    Author = story.Author,
                    GenreName = story.GenreName,
                    CoverImage = story.CoverImage,
                    Score = rank.Score,
                    GeneratedAt = generatedAt
                };
                _context.StoryRankings.Add(newRanking);
            }
        }
    }


    // --- JOB 1 & 2: BXH Lượt Đọc (All & Genre) ---
    // (Cập nhật hàng giờ/ngày - CẨN THẬN HIỆU NĂNG!)
    public async Task UpdateReadRankings()
    {
        var now = DateTime.UtcNow.AddHours(TimeZoneOffset);
        var today = now.Date;
        var startOfWeek = GetStartOfWeek(today);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        // Xóa BXH lượt đọc cũ
        await _context.StoryRankings
            .Where(r => r.Type == RankingType.READS_DAY_ALL ||
                        r.Type == RankingType.READS_WEEK_ALL ||
                        r.Type == RankingType.READS_MONTH_ALL ||
                        r.Type == RankingType.READS_DAY_GENRE ||
                        r.Type == RankingType.READS_WEEK_GENRE ||
                        r.Type == RankingType.READS_MONTH_GENRE)
            .ExecuteDeleteAsync();

        // --- 1. Tính BXH Ngày (All) ---
        var dayReads = await _context.ChapterReadedByUsers
            .Where(r => r.ReadAt >= today)
            .GroupBy(r => r.Chapter.StoryId)
            .Select(g => new { StoryId = g.Key, Reads = g.Count() })
            .OrderByDescending(x => x.Reads)
            .Take(100) // Lấy Top 100
            .Select(x => new ValueTuple<int, int>(x.StoryId, x.Reads))
            .ToListAsync();
        await SaveRankings(dayReads, RankingType.READS_DAY_ALL, null);

        // --- 2. Tính BXH Tuần (All) ---
        var weekReads = await _context.ChapterReadedByUsers
            .Where(r => r.ReadAt >= startOfWeek)
            .GroupBy(r => r.Chapter.StoryId)
            .Select(g => new { StoryId = g.Key, Reads = g.Count() })
            .OrderByDescending(x => x.Reads)
            .Take(100)
            .Select(x => new ValueTuple<int, int>(x.StoryId, x.Reads))
            .ToListAsync();
        await SaveRankings(weekReads, RankingType.READS_WEEK_ALL, null);

        // --- 3. Tính BXH Tháng (All) ---
        var monthReads = await _context.ChapterReadedByUsers
            .Where(r => r.ReadAt >= startOfMonth)
            .GroupBy(r => r.Chapter.StoryId)
            .Select(g => new { StoryId = g.Key, Reads = g.Count() })
            .OrderByDescending(x => x.Reads)
            .Take(100)
            .Select(x => new ValueTuple<int, int>(x.StoryId, x.Reads))
            .ToListAsync();
        await SaveRankings(monthReads, RankingType.READS_MONTH_ALL, null);

        // --- 4. Tính BXH Theo Genre (Tối ưu: 1 query) ---
        //ngày
        var allDayGenreReads = await _context.ChapterReadedByUsers
            .Where(r => r.ReadAt >= today && r.Chapter.Story.GenreId != null) // Lấy hết
            .GroupBy(r => new { r.Chapter.StoryId, r.Chapter.Story.GenreId }) // Nhóm 2 chiều
            .Select(g => new
            {
                StoryId = g.Key.StoryId,
                GenreId = g.Key.GenreId, // Lấy GenreId
                Reads = g.Count()
            })
            .ToListAsync();

        // Giờ nhóm lại bằng C# (cực nhanh)
        var groupedByGenre = allDayGenreReads
            .GroupBy(x => x.GenreId)
            .Select(g => new
            {
                GenreId = g.Key,
                Ranks = g.OrderByDescending(x => x.Reads)
                         .Take(100)
                         .Select(x => new ValueTuple<int, int>(x.StoryId, x.Reads))
                         .ToList()
            });

        // Lặp qua kết quả đã xử lý (không query DB nữa)
        foreach (var genreRank in groupedByGenre)
        {
            await SaveRankings(genreRank.Ranks, RankingType.READS_DAY_GENRE, genreRank.GenreId);
        }
        // tuần
        var allWeekGenreReads = await _context.ChapterReadedByUsers
            .Where(r => r.ReadAt >= startOfWeek && r.Chapter.Story.GenreId != null)
            .GroupBy(r => new { r.Chapter.StoryId, r.Chapter.Story.GenreId })
            .Select(g => new
            {
                StoryId = g.Key.StoryId,
                GenreId = g.Key.GenreId,
                Reads = g.Count()
            })
            .ToListAsync();

        var groupedWeekByGenre = allWeekGenreReads
            .GroupBy(x => x.GenreId)
            .Select(g => new
            {
                GenreId = g.Key,
                Ranks = g.OrderByDescending(x => x.Reads)
                         .Take(100)
                         .Select(x => new ValueTuple<int, int>(x.StoryId, x.Reads))
                         .ToList()
            });

        foreach (var genreRank in groupedWeekByGenre)
            {
            await SaveRankings(genreRank.Ranks, RankingType.READS_WEEK_GENRE, genreRank.GenreId);
        }
        // tháng

        var allMonthGenreReads = await _context.ChapterReadedByUsers
            .Where(r => r.ReadAt >= startOfMonth && r.Chapter.Story.GenreId != null)
            .GroupBy(r => new { r.Chapter.StoryId, r.Chapter.Story.GenreId })
            .Select(g => new
            {
                StoryId = g.Key.StoryId,
                GenreId = g.Key.GenreId,
                Reads = g.Count()
            })
            .ToListAsync();

        var groupedMonthByGenre = allMonthGenreReads
            .GroupBy(x => x.GenreId)
            .Select(g => new
            {
                GenreId = g.Key,
                Ranks = g.OrderByDescending(x => x.Reads)
                         .Take(100)
                         .Select(x => new ValueTuple<int, int>(x.StoryId, x.Reads))
                         .ToList()
            });
        foreach (var genreRank in groupedMonthByGenre)
            {
            await SaveRankings(genreRank.Ranks, RankingType.READS_MONTH_GENRE, genreRank.GenreId);
        }


        await _context.SaveChangesAsync();
    }


    // --- JOB 3: BXH Phiếu Đề Cử (Tháng) ---
    // (Cập nhật theo ngày)
    public async Task UpdateTicketRankings()
    {
        var now = DateTime.UtcNow.AddHours(TimeZoneOffset);
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        await _context.StoryRankings
            .Where(r => r.Type == RankingType.TICKETS_MONTH_ALL)
            .ExecuteDeleteAsync();

        var ticketRanks = await _context.TicketTransactions
            .Where(t => t.CreatedAt >= startOfMonth &&
                        t.Type == TransactionType.Spending &&
                        t.StoryId != null)
            .GroupBy(t => t.StoryId.Value)
            .Select(g => new
            {
                StoryId = g.Key,
                Tickets = g.Sum(x => -x.Amount) // Amount là số âm
            })
            .OrderByDescending(x => x.Tickets)
            .Take(100) // Lấy Top 100
            .Select(x => new ValueTuple<int, int>(x.StoryId, x.Tickets))
            .ToListAsync();

        await SaveRankings(ticketRanks, RankingType.TICKETS_MONTH_ALL, null);
        await _context.SaveChangesAsync();
    }


    // --- JOB 4: Hết Hạn VIP ---
    // (Cập nhật theo ngày - siêu nhẹ)
    public async Task CheckVipChapterExpiry()
    {
        var now = DateTime.UtcNow; // Dùng UTC cho cái này

        await _context.Chapters
            .Where(c => c.IsVip == true &&
                        c.VipUnlockAt != null &&
                        c.VipUnlockAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsVip, false));
    }


    // --- JOB 5: Phát Vé Hàng Tuần ---
    // (Cập nhật Thứ 2 hàng tuần)
    public async Task GrantWeeklyTickets()
    {
        int amountToGrant = 5; // Số vé phát hàng tuần

        var userIds = await _context.Users
            .Where(u => !u.IsDeleted && !u.IsBanned)
            .Select(u => u.UserId)
            .ToListAsync();

        if (!userIds.Any()) return;

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Cộng vé hàng loạt
            await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Ticket, u => amountToGrant));

            // 2. Ghi log hàng loạt
            var logs = userIds.Select(id => new TicketTransaction
            {
                UserId = id,
                StoryId = null,
                Amount = amountToGrant,
                Type = TransactionType.Earning,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.TicketTransactions.AddRange(logs);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            // (Thêm log lỗi ở đây nếu cần)
        }

    }

    // --- JOB 6: BXH Toàn Thời Gian (Rating, Follow, Reads) ---
    // (Chạy 1 lần/ngày là đủ)
    public async Task UpdateGlobalRankings()
    {
        // 1. Xóa BXH cũ
        await _context.StoryRankings
            .Where(r => r.Type == RankingType.RATING_ALL ||
                        r.Type == RankingType.FOLLOWERS_ALL ||
                        r.Type == RankingType.READS_ALL_TIME_ALL)
            .ExecuteDeleteAsync();

        // 2. Tính Top Rating
        var ratingRanks = await _context.Stories
            .Where(s => !s.IsDeleted && s.TotalRatings > 10) // Lọc nhiễu (ví dụ: > 10 đánh giá)
            .OrderByDescending(s => s.AverageRating)
            .Take(100)
            .Select(s => new ValueTuple<int, int>(s.StoryId, (int)(s.AverageRating * 100))) // Score là 4.50 -> 450
            .ToListAsync();
        await SaveRankings(ratingRanks, RankingType.RATING_ALL, null);

        // 3. Tính Top Followers
        var followerRanks = await _context.Stories
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.TotalFollowers)
            .Take(100)
            .Select(s => new ValueTuple<int, int>(s.StoryId, s.TotalFollowers)) // Score là TotalFollowers
            .ToListAsync();
        await SaveRankings(followerRanks, RankingType.FOLLOWERS_ALL, null);

        // 4. Tính Top All-Time Reads
        var allTimeReadsRanks = await _context.Stories
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.TotalReads)
            .Take(100)
            .Select(s => new ValueTuple<int, int>(s.StoryId, s.TotalReads)) // Score là TotalReads
            .ToListAsync();
        await SaveRankings(allTimeReadsRanks, RankingType.READS_ALL_TIME_ALL, null);

        await _context.SaveChangesAsync();
    }
}