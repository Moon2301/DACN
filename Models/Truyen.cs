using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Bio { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        public string? AvatarUrl { get; set; } = "/images/defaul/defaulAvatar.png";
        public int Money { get; set; } = 0;
        public int ActivePoint { get; set; } = 0;
        public int Ticket { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsBanned { get; set; } = false;
        public DateTime? BannedUntil { get; set; }
        public bool IsDeleted { get; set; } = false;
        // Navigation Properties
        public ICollection<Bookmark> Bookmarks { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Rating> Ratings { get; set; }
        public ICollection<ChapterReadedByUser> ChapterReadedByUsers { get; set; }
        public ICollection<UnlockedChapter> UnlockedChapters { get; set; }
        public ICollection<FollowedStoryByUser> FollowedStoryByUsers { get; set; }
        public ICollection<MoneyTransaction> MoneyTransactions { get; set; }
        public ICollection<TicketTransaction> TicketTransactions { get; set; }
        public ICollection<ActivePointTransaction> ActivePointTransactions { get; set; }
        public ICollection<MoneyTopUpBill> MoneyTopUpBills { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<DailyReward> DailyRewards { get; set; }
        public ICollection<Report> Reports { get; set; }
    }
    public enum UserRole
    {
        User,
        Admin
    }
    public class Genre
    {
        [Key]
        public int GenreId { get; set; }

        [Required]
        public string Name { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<Story> Stories { get; set; }
    }

    public class Tag
    {
        [Key]
        public int TagId { get; set; }

        public string Name { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<StoryTag> StoryTags { get; set; }
    }

    public class StoryTag
    {
        [Key]
        public int StoryTagId { get; set; }
        public int StoryId { get; set; }
        public Story? Story { get; set; }
        public int TagId { get; set; }
        public Tag? Tag { get; set; }
    }

    public enum StoryStatus
    {
        DangCapNhat,
        TamNgung,
        DaNgung,
        HoanThanh
    }

    public class Story
    {
        [Key]
        public int StoryId { get; set; }

        [Required]
        public string Title { get; set; }

        public string Author { get; set; }
        public string Description { get; set; }
        public string CoverImage { get; set; } = "/images/defaul/defaulImage.png";

        public StoryStatus Status { get; set; } = StoryStatus.DangCapNhat;

        public int GenreId { get; set; }
        public Genre? Genre { get; set; }

        public int UploadedByUserId { get; set; }
        public User? UploadedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Chapter> Chapters { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Rating> Ratings { get; set; }
        public ICollection<StoryTag> StoryTags { get; set; }
        public ICollection<FollowedStoryByUser> FollowedStoryByUsers { get; set; }
        public ICollection<Bookmark> Bookmarks { get; set; }
        public int TotalReads { get; set; } = 0;
        public int TotalChapters { get; set; } = 0;
        public int TotalRatings { get; set; } = 0;
        public decimal AverageRating { get; set; } = 0.0m;
        public int TotalComments { get; set; } = 0;
        public int TotalTicketsEarned { get; set; } = 0;
        public int TotalBookmarks { get; set; } = 0;
        public int TotalFollowers { get; set; } = 0;
        public bool IsDeleted { get; set; } = false;
    }

    public class Chapter
    {
        [Key]
        public int ChapterId { get; set; }

        public int StoryId { get; set; }
        public Story? Story { get; set; }

        public int ChapterNumber { get; set; }
        public string Title { get; set; }
        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsVip { get; set; } = false;

        public DateTime? VipUnlockAt { get; set; }
        public int UnlockPriceMoney { get; set; } = 0;

        public int UnlockPriceActivePoint { get; set; } = 0;        

        public bool IsDeleted { get; set; } = false;
        public ICollection<Comment> Comments { get; set; }
        public ICollection<ChapterReadedByUser> ChapterReadedByUsers { get; set; }
        public ICollection<UnlockedChapter> Unlocks { get; set; }
    }
    public class Bookmark
    {
        [Key]
        public int BookmarkId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int StoryId { get; set; }
        public Story? Story { get; set; }        
        public int ChapterId { get; set; }
        public Chapter? Chapter { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? StoryId { get; set; }
        [InverseProperty("Comments")]
        public Story? Story { get; set; }

        public int? ChapterId { get; set; }
        [InverseProperty("Comments")]
        public Chapter? Chapter { get; set; }

        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public int RepliesCount { get; set; } = 0;

        [InverseProperty("ParentComment")]
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

    }

    public class Rating
    {
        [Key]
        public int RatingId { get; set; }
        
        public int UserId { get; set; }
        public User? User { get; set; }

        public int StoryId { get; set; }
        public Story? Story { get; set; }

        public decimal Score { get; set; }
        public string Review { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class UnlockedChapter
    {
        [Key]
        public int UnlockId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int ChapterId { get; set; }
        public Chapter Chapter { get; set; }
        public int UsedMoney { get; set; } = 0;
        public int UsedActivePoint { get; set; } = 0;
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    }

    public class ChapterReadedByUser
    {
        [Key]
        public int ReadId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int ChapterId { get; set; }
        public Chapter? Chapter { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }

    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
        [Required]
        public string Message { get; set; }
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class FollowedStoryByUser
    {
        [Key]
        public int FollowId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int StoryId { get; set; }
        public Story Story { get; set; }
        public int? CurrentChapterId { get; set; } 
        public Chapter? CurrentChapter { get; set; }
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
    }
    public class Report
    {
        [Key]
        public int ReportId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public string TargetType { get; set; } // Story, Chapter, Comment
        public int TargetId { get; set; }
        [Required]
        public string Reason { get; set; }

        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    }

    public class DailyReward
    {
        [Key]
        public int RewardId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime RewardDate { get; set; }
        public int ActivePointAmount { get; set; }
    }
    public class MoneyTopUpBill
    {
        [Key]
        public int MoneyTopUpBillId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class MoneyTransaction
    {
        [Key]
        public int MoneyTransactionId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
        public int? ChapterId { get; set; }
        public Chapter? Chapter { get; set; }

        public int Amount { get; set; }
        [Required]
        public TransactionType Type { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TicketTransaction
    {
        [Key]
        public int TicketTransactionId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int? StoryId { get; set; }
        public Story? Story { get; set; }
        public int Amount { get; set; }
        [Required]
        public TransactionType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ActivePointTransaction
    {
        public int ActivePointTransactionId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int Amount { get; set; }
        [Required]
        public TransactionType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum TransactionType
    {
        Earning,
        Spending
    }
}
