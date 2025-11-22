using DACN.Models;
using System.ComponentModel.DataAnnotations;

namespace DACN.Dtos
{
    // DTO để hiển thị
    public class GenreDto
    {
        public int GenreId { get; set; }
        public string Name { get; set; }
        public int BookCount { get; set; }
    }

    // DTO để tạo/cập nhật
    public class GenreCreateDto
    {
        [Required]
        public string Name { get; set; }
    }

    // DTO để hiển thị
    public class TagDto
    {
        public int TagId { get; set; }
        public string Name { get; set; }
        public int BookCount { get; set; }
    }

    // DTO để tạo/cập nhật
    public class TagCreateUpdateDto
    {
        [Required]
        public string Name { get; set; }
    }

    // DTO để hiển thị thông tin User (an toàn, không có hash)
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Bio { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public string? AvatarUrl { get; set; }
        public int Money { get; set; } = 0;
        public int ActivePoint { get; set; } = 0;
        public int Ticket { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public bool IsBanned { get; set; } = false;
        public DateTime? BannedUntil { get; set; }
    }

    // DTO để tạo mới User (ví dụ: admin tạo)
    public class UserCreateDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; } // Mật khẩu thô

        public UserRole Role { get; set; } = UserRole.User;
    }

    // DTO để cập nhật User (ví dụ: admin cập nhật)
    public class UserUpdateDto
    {
        // Admin có thể không được phép đổi Username

        [EmailAddress]
        public string Email { get; set; }

        public string? PhoneNumber { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Bio { get; set; }
        public UserRole Role { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsBanned { get; set; }
        public DateTime? BannedUntil { get; set; }
    }

    // DTO để hiển thị (RẤT QUAN TRỌNG)
    // Chúng ta trả về TÊN (string) chứ không phải ID
    public class StoryDto
    {
        public int StoryId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string CoverImage { get; set; }
        public StoryStatus Status { get; set; }

        // Thông tin đã được giải quyết (resolved)
        public int GenreId { get; set; }
        public string GenreName { get; set; } // Thay vì GenreId

        public int UploadedByUserId { get; set; }
        public string UploadedByUsername { get; set; } // Thay vì UploadedByUserId

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Danh sách các Tag
        public List<string> Tags { get; set; } = new List<string>();

        // Các thông số thống kê
        public int TotalReads { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalFollowers { get; set; }
        public int TotalComments { get; set; }
        public int TotalChapters { get; set; }
    }

    // DTO để TẠO MỚI Story
    public class StoryCreateDto
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Author { get; set; }
        public string Description { get; set; }
        [Required]
        public string CoverImage { get; set; }
        public StoryStatus Status { get; set; } = StoryStatus.DangCapNhat;
        [Required]
        public int GenreId { get; set; }

        [Required]
        public int UploadedByUserId { get; set; } // BẮT BUỘC: Cần biết ai là người đăng
        // SAU NÀY: ID này sẽ được lấy từ Token (JWT) chứ không cần truyền vào

        // Danh sách các TagId mà user chọn
        public List<int> TagIds { get; set; } = new List<int>();
    }

    // DTO để CẬP NHẬT Story
    public class StoryUpdateDto
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Author { get; set; }
        public string Description { get; set; }
        [Required]
        public string CoverImage { get; set; }
        [Required]
        public StoryStatus Status { get; set; }
        [Required]
        public int GenreId { get; set; }

        // Danh sách CẬP NHẬT các TagId
        public List<int> TagIds { get; set; } = new List<int>();
    }

    // DTO để hiển thị trong DANH SÁCH (KHÔNG CÓ CONTENT)
    public class ChapterListItemDto
    {
        public int ChapterId { get; set; }
        public int ChapterNumber { get; set; }
        public string Title { get; set; }
        public bool IsVip { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO để hiển thị CHI TIẾT (CÓ CONTENT)
    public class ChapterDetailDto
    {
        public int ChapterId { get; set; }
        public int StoryId { get; set; }
        public int ChapterNumber { get; set; }
        public string Title { get; set; }
        public string Content { get; set; } // Có content!
        public DateTime CreatedAt { get; set; }
        public bool IsVip { get; set; }
        public DateTime? VipUnlockAt { get; set; }
        public int UnlockPriceMoney { get; set; }
        public int UnlockPriceActivePoint { get; set; }

        public int? PreviousChapterId { get; set; }
        public int? NextChapterId { get; set; }
    }

    // DTO để TẠO MỚI / CẬP NHẬT
    public class ChapterCreateUpdateDto
    {
        [Required]
        public int ChapterNumber { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public bool IsVip { get; set; } = false;
        public DateTime? VipUnlockAt { get; set; }
        public int UnlockPriceMoney { get; set; } = 0;
        public int UnlockPriceActivePoint { get; set; } = 0;
    }

    // DTO để HIỂN THỊ (lấy danh sách bookmark của user)
    // Cần hiển thị thông tin truyện và chương
    public class BookmarkDto
    {
        public int BookmarkId { get; set; }
        public int UserId { get; set; }

        // Thông tin truyện
        public int StoryId { get; set; }
        public string StoryTitle { get; set; }
        public string StoryCoverImage { get; set; }

        // Thông tin chương
        public int ChapterId { get; set; }
        public int ChapterNumber { get; set; }
        public string ChapterTitle { get; set; }

        public DateTime CreatedAt { get; set; } // Ngày bookmark
    }

    // DTO để TẠO MỚI / CẬP NHẬT bookmark
    public class BookmarkCreateDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ token
        [Required]
        public int StoryId { get; set; }
        [Required]
        public int ChapterId { get; set; }
    }

    // DTO để HIỂN THỊ (lấy danh sách user đang theo dõi)
    public class FollowDto
    {
        public int FollowId { get; set; }
        public int UserId { get; set; }

        // Thông tin truyện
        public int StoryId { get; set; }
        public string StoryTitle { get; set; }
        public string StoryCoverImage { get; set; }
        public StoryStatus StoryStatus { get; set; }

        // Thông tin TIẾN ĐỘ
        public int? CurrentChapterId { get; set; }
        // Dữ liệu "suy ra" (derived) mà chúng ta đã thống nhất
        public int CurrentChapterNumber { get; set; }
        public int TotalStoryChapters { get; set; }
        public string CurrentChapterTitle { get; set; }

        public DateTime FollowedAt { get; set; }
    }

    // DTO để TẠO MỚI (Follow)
    public class FollowCreateDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ token
        [Required]
        public int StoryId { get; set; }
    }

    // DTO để XÓA (Unfollow)
    // (Dùng DTO cho Unfollow sẽ dễ dàng hơn là bắt client phải biết FollowId)
    public class UnfollowDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ token
        [Required]
        public int StoryId { get; set; }
    }


    // DTO để CẬP NHẬT TIẾN ĐỘ ĐỌC
    public class FollowProgressUpdateDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ token
        [Required]
        public int StoryId { get; set; }
        [Required]
        public int ChapterId { get; set; } // Chương vừa đọc xong
    }

    // DTO Wrapper cho phân trang
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; } // Tổng số comment (để client tính số trang)
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    // DTO để HIỂN THỊ
    public class CommentDto
    {
        public int CommentId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public int UserId { get; set; }
        public string Username { get; set; }
        public string? AvatarUrl { get; set; }

        public int? ParentCommentId { get; set; }
        public int RepliesCount { get; set; } // Số lượng con (để hiển thị "Xem 5 trả lời")
    }
    // DTO để TẠO MỚI
    public class CommentCreateDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ Token

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        // Phải có 1 trong 3
        public int? StoryId { get; set; }
        public int? ChapterId { get; set; }
        public int? ParentCommentId { get; set; } // ID của bình luận cha
    }

    // DTO để CẬP NHẬT
    public class CommentUpdateDto
    {
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }
    }

    // DTO để HIỂN THỊ (trong danh sách)
    public class RatingDto
    {
        public int RatingId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } // Cần biết ai đánh giá
        public string? AvatarUrl { get; set; }
        public int StoryId { get; set; }
        public decimal Score { get; set; } // 1.0 -> 5.0
        public string Review { get; set; } // Nội dung review
        public DateTime CreatedAt { get; set; }
    }

    // DTO để TẠO MỚI / CẬP NHẬT
    public class RatingCreateUpdateDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ Token
        [Required]
        public int StoryId { get; set; }
        [Required]
        [Range(0.5, 5.0)] // Cho phép 0.5 sao
        public decimal Score { get; set; }
        public string Review { get; set; } = string.Empty;
    }

    // DTO để HIỂN THỊ (lấy danh sách đã đọc)
    public class ChapterReadDto
    {
        public int ReadId { get; set; }
        public DateTime ReadAt { get; set; }

        // Thông tin chương
        public int ChapterId { get; set; }
        public int ChapterNumber { get; set; }
        public string ChapterTitle { get; set; }

        // Thông tin truyện
        public int StoryId { get; set; }
    }

    // DTO để TẠO MỚI (đánh dấu đã đọc)
    public class ChapterReadCreateDto
    {
        [Required]
        public int UserId { get; set; } // Sau này lấy từ token

        [Required]
        public int ChapterId { get; set; }
    }
    public class ReadHistorySummaryDto
    {
        public int StoryId { get; set; }
        public string StoryTitle { get; set; }
        public string StoryCoverImage { get; set; }
        public int LastReadChapterId { get; set; }
        public int LastReadChapterNumber { get; set; }
        public string LastReadChapterTitle { get; set; }
        public DateTime ReadAt { get; set; }
        public int TotalChapters { get; set; }
    }
}
