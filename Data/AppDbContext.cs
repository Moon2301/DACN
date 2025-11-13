using DACN.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using DACN.Helpers;

namespace DACN.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSet cho toàn bộ model
        public DbSet<User> Users { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<StoryTag> StoryTags { get; set; }
        public DbSet<UnlockedChapter> UnlockedChapters { get; set; }
        public DbSet<ChapterReadedByUser> ChapterReadedByUsers { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<FollowedStoryByUser> FollowedStoryByUsers { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<DailyReward> DailyRewards { get; set; }
        public DbSet<MoneyTopUpBill> MoneyTopUpBills { get; set; }
        public DbSet<MoneyTransaction> MoneyTransactions { get; set; }
        public DbSet<TicketTransaction> TicketTransactions { get; set; }
        public DbSet<ActivePointTransaction> ActivePointTransactions { get; set; }
        public DbSet<StoryRanking> StoryRankings { get; set; }

        // Fluent API Configurations
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- CẤU HÌNH ENUM-TO-STRING ---
            // Lưu trữ các Enum dưới dạng chuỗi trong DB để dễ đọc

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Story>()
                .Property(s => s.Status)
                .HasConversion<string>();

            modelBuilder.Entity<MoneyTransaction>()
                .Property(t => t.Type)
                .HasConversion<string>();

            modelBuilder.Entity<TicketTransaction>()
                .Property(t => t.Type)
                .HasConversion<string>();

            modelBuilder.Entity<ActivePointTransaction>()
                .Property(t => t.Type)
                .HasConversion<string>();

            modelBuilder.Entity<StoryRanking>()
                .Property(r => r.Type)
                .HasConversion<string>();
            // --- CẤU HÌNH GIÁ TRỊ MẶC ĐỊNH CHO DATETIME ---
            // Đặt giá trị mặc định là thời gian UTC hiện tại của server DB
            // Đặt tên hàm SQL vào một biến để dễ thay đổi
            string utcNowFunction = "GETUTCDATE()"; // Thay đổi hàm này tùy theo DB của bạn

            modelBuilder.Entity<User>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<Story>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<Story>()
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql(utcNowFunction); // Cần trigger hoặc xử lý C# để tự động cập nhật

            modelBuilder.Entity<Chapter>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<Bookmark>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<Comment>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<Rating>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<UnlockedChapter>()
                .Property(e => e.UnlockedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<ChapterReadedByUser>()
                .Property(e => e.ReadAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<Notification>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<FollowedStoryByUser>()
                .Property(e => e.FollowedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<Report>()
                .Property(e => e.ReportedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<MoneyTopUpBill>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<MoneyTransaction>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<TicketTransaction>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            modelBuilder.Entity<ActivePointTransaction>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql(utcNowFunction);

            // --- CẤU HÌNH QUAN HỆ VÀ HÀNH VI XÓA ---

            // Cấu hình quan hệ tự tham chiếu cho Comment (Replies)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.ClientSetNull); // Khi xóa comment cha, các comment con bị set ParentCommentId = null

            // Cấu hình quan hệ giữa Story và User (UploadedBy)
            modelBuilder.Entity<Story>()
                .HasOne(s => s.UploadedBy)
                .WithMany() // User không có ICollection<Story> đã đăng
                .HasForeignKey(s => s.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict); // Ngăn không cho xóa User nếu User đó đã đăng Story

            // Cấu hình quan hệ giữa FollowedStoryByUser và Chapter (CurrentChapter)
            modelBuilder.Entity<FollowedStoryByUser>()
                .HasOne(f => f.CurrentChapter)
                .WithMany() // Chapter không cần biết nó được "follow"
                .HasForeignKey(f => f.CurrentChapterId)
                .OnDelete(DeleteBehavior.ClientSetNull); // Nếu chương bị xóa, set ID này = null
                                                         // --- CẤU HÌNH HÀNH VI XÓA (DELETE BEHAVIOR) ---
                                                         // Thêm vào OnModelCreating trong AppDbContext.cs

            // User → Bookmarks
            modelBuilder.Entity<Bookmark>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookmarks)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa bookmarks của user

            // Story → Bookmarks
            modelBuilder.Entity<Bookmark>()
                .HasOne(b => b.Story)
                .WithMany(s => s.Bookmarks)
                .HasForeignKey(b => b.StoryId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa story → xóa bookmarks của story

            // Chapter → Bookmarks
            modelBuilder.Entity<Bookmark>()
                .HasOne(b => b.Chapter)
                .WithMany()
                .HasForeignKey(b => b.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Comments
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Xóa user → xóa comments của user

            // Story → Comments
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Story)
                .WithMany(s => s.Comments)
                .HasForeignKey(c => c.StoryId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho xóa story nếu còn comments

            // Chapter → Comments
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Chapter)
                .WithMany(c => c.Comments)
                .HasForeignKey(c => c.ChapterId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa chapter → xóa comments

            // User → Ratings
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa ratings

            // Story → Ratings
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Story)
                .WithMany(s => s.Ratings)
                .HasForeignKey(r => r.StoryId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho xóa story nếu còn ratings

            // Genre → Stories
            modelBuilder.Entity<Story>()
                .HasOne(s => s.Genre)
                .WithMany(g => g.Stories)
                .HasForeignKey(s => s.GenreId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho xóa genre nếu còn stories

            // User (UploadedBy) → Stories
            modelBuilder.Entity<Story>()
                .HasOne(s => s.UploadedBy)
                .WithMany()
                .HasForeignKey(s => s.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho xóa user nếu user đó đã upload stories

            // Story → Chapters
            modelBuilder.Entity<Chapter>()
                .HasOne(c => c.Story)
                .WithMany(s => s.Chapters)
                .HasForeignKey(c => c.StoryId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa story → xóa chapters

            // Story → Tags (through StoryTag)
            modelBuilder.Entity<StoryTag>()
                .HasOne(st => st.Story)
                .WithMany(s => s.StoryTags)
                .HasForeignKey(st => st.StoryId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa story → xóa story-tags

            // Tag → StoryTags
            modelBuilder.Entity<StoryTag>()
                .HasOne(st => st.Tag)
                .WithMany(t => t.StoryTags)
                .HasForeignKey(st => st.TagId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa tag → xóa story-tags

            // User → UnlockedChapters
            modelBuilder.Entity<UnlockedChapter>()
                .HasOne(u => u.User)
                .WithMany(usr => usr.UnlockedChapters)
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa unlocked chapters

            // Chapter → UnlockedChapters
            modelBuilder.Entity<UnlockedChapter>()
                .HasOne(u => u.Chapter)
                .WithMany(c => c.Unlocks)
                .HasForeignKey(u => u.ChapterId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa chapter → xóa unlock records

            // User → ChapterReadedByUsers
            modelBuilder.Entity<ChapterReadedByUser>()
                .HasOne(c => c.User)
                .WithMany(u => u.ChapterReadedByUsers)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa read history

            // Chapter → ChapterReadedByUsers
            modelBuilder.Entity<ChapterReadedByUser>()
                .HasOne(c => c.Chapter)
                .WithMany(ch => ch.ChapterReadedByUsers)
                .HasForeignKey(c => c.ChapterId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa chapter → xóa read history

            // User → Notifications
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa notifications

            // User → FollowedStoryByUsers
            modelBuilder.Entity<FollowedStoryByUser>()
                .HasOne(f => f.User)
                .WithMany(u => u.FollowedStoryByUsers)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa follows

            // Story → FollowedStoryByUsers
            modelBuilder.Entity<FollowedStoryByUser>()
                .HasOne(f => f.Story)
                .WithMany(s => s.FollowedStoryByUsers)
                .HasForeignKey(f => f.StoryId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa story → xóa follows

            // User → Reports
            modelBuilder.Entity<Report>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa reports

            // User → DailyRewards
            modelBuilder.Entity<DailyReward>()
                .HasOne(d => d.User)
                .WithMany(u => u.DailyRewards)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa daily rewards

            // User → MoneyTopUpBills
            modelBuilder.Entity<MoneyTopUpBill>()
                .HasOne(m => m.User)
                .WithMany(u => u.MoneyTopUpBills)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa top-up bills

            // User → MoneyTransactions
            modelBuilder.Entity<MoneyTransaction>()
                .HasOne(m => m.User)
                .WithMany(u => u.MoneyTransactions)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa transactions

            // Chapter → MoneyTransactions
            modelBuilder.Entity<MoneyTransaction>()
                .HasOne(m => m.Chapter)
                .WithMany()
                .HasForeignKey(m => m.ChapterId)
                .OnDelete(DeleteBehavior.SetNull); // Xóa chapter → set transaction.ChapterId = null

            // User → TicketTransactions
            modelBuilder.Entity<TicketTransaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.TicketTransactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa transactions

            // Story → TicketTransactions
            modelBuilder.Entity<TicketTransaction>()
                .HasOne(t => t.Story)
                .WithMany()
                .HasForeignKey(t => t.StoryId)
                .OnDelete(DeleteBehavior.SetNull); // Xóa story → set transaction.StoryId = null

            // User → ActivePointTransactions
            modelBuilder.Entity<ActivePointTransaction>()
                .HasOne(a => a.User)
                .WithMany(u => u.ActivePointTransactions)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa transactions

            // --- CẤU HÌNH ĐỘ CHÍNH XÁC (PRECISION) CHO SỐ THỰC ---

            modelBuilder.Entity<Story>()
                .Property(s => s.AverageRating)
                .HasPrecision(3, 2); // Ví dụ: 3 chữ số tổng, 2 chữ số sau dấu phẩy (vd: 4.75)

            modelBuilder.Entity<Rating>()
                .Property(r => r.Score)
                .HasPrecision(3, 2); // Ví dụ: 3.50

            // --- Seed admin account ---
            var adminPassword = PasswordHelper.HashPassword("Admin@123");
            modelBuilder.Entity<User>().HasData(new User
            {
                UserId = 1,
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = adminPassword,
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                IsBanned = false,
                IsDeleted = false,
                Money = 999999,
                ActivePoint = 999999,
                Ticket = 999999,
                AvatarUrl = "/images/defaulAvatar.png",
                Bio = "Default admin account"
            });
        }

    }
}
