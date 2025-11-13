using DACN.Data;
using DACN.Dtos;
using DACN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACN.Controllers
{
    [Route("api/unlocked")]
    [ApiController]
    public class UnlockedChaptersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UnlockedChaptersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/unlocked/user/5/story/10
        // Lấy danh sách ID các chương đã mở khóa của 1 truyện
        [HttpGet("user/{userId}/story/{storyId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetUnlockedChapterIdsForStory(int userId, int storyId)
        {
            // Tương tự API "đã đọc", trả về list ID là nhẹ nhất
            var unlockedChapterIds = await _context.UnlockedChapters
                .Where(u => u.UserId == userId && u.Chapter.StoryId == storyId)
                .Select(u => u.ChapterId)
                .ToListAsync();

            return Ok(unlockedChapterIds);
        }

        // POST: api/unlocked
        // HÀNH ĐỘNG MỞ KHÓA (Quan trọng!)
        [HttpPost]
        public async Task<IActionResult> UnlockChapter(UnlockChapterCreateDto unlockDto)
        {
            // --- BƯỚC 1: LẤY DỮ LIỆU VÀ KIỂM TRA ---

            // Lấy User (để trừ tiền)
            var user = await _context.Users.FindAsync(unlockDto.UserId);
            if (user == null || user.IsDeleted)
                return NotFound("Không tìm thấy người dùng.");

            // Lấy Chapter (để biết giá)
            var chapter = await _context.Chapters
                .Include(c => c.Story) // Lấy cả Story
                .FirstOrDefaultAsync(c => c.ChapterId == unlockDto.ChapterId);

            if (chapter == null || chapter.IsDeleted)
                return NotFound("Không tìm thấy chương.");

            // Kiểm tra xem có phải VIP không
            if (!chapter.IsVip || chapter.VipUnlockAt.HasValue && chapter.VipUnlockAt.Value < DateTime.UtcNow)
                return BadRequest("Chương này miễn phí, không cần mở khóa.");

            // Kiểm tra xem ĐÃ MỞ KHÓA CHƯA
            bool alreadyUnlocked = await _context.UnlockedChapters
                .AnyAsync(u => u.UserId == unlockDto.UserId && u.ChapterId == unlockDto.ChapterId);

            if (alreadyUnlocked)
                return Ok("Chương đã được mở khóa từ trước."); // 200 OK

            // --- BƯỚC 2: KIỂM TRA PHƯƠNG THỨC VÀ TIỀN ---

            int costMoney = 0;
            int costActivePoint = 0;

            if (unlockDto.Method == UnlockMethod.Money)
            {
                costMoney = chapter.UnlockPriceMoney;
                if (costMoney <= 0)
                    return BadRequest("Chương này không thể mở khóa bằng tiền.");
                if (user.Money < costMoney)
                    return StatusCode(402, "Không đủ tiền (Money)."); // 402 Payment Required
            }
            else if (unlockDto.Method == UnlockMethod.ActivePoint)
            {
                costActivePoint = chapter.UnlockPriceActivePoint;
                if (costActivePoint <= 0)
                    return BadRequest("Chương này không thể mở khóa bằng điểm.");
                if (user.ActivePoint < costActivePoint)
                    return StatusCode(402, "Không đủ điểm (ActivePoint).");
            }

            // --- BƯỚC 3: THỰC HIỆN GIAO DỊCH (Transaction) ---

            // ĐỊNH NGHĨA PHÍ (Commission)
            // decimal tốt hơn float khi làm việc với tiền
            // 0.7m nghĩa là tác giả nhận 70%
            const decimal authorCommissionRate = 0.7m;

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // --- PHẦN 1: XỬ LÝ NGƯỜI MUA ---

                // 1. Trừ tiền/điểm của Người Mua
                user.Money -= costMoney;
                user.ActivePoint -= costActivePoint;

                // 2. Tạo bản ghi UnlockedChapter (giữ nguyên)
                var newUnlock = new UnlockedChapter
                {
                    UserId = user.UserId,
                    ChapterId = chapter.ChapterId,
                    UsedMoney = costMoney,
                    UsedActivePoint = costActivePoint,
                    UnlockedAt = DateTime.UtcNow
                };
                _context.UnlockedChapters.Add(newUnlock);

                // 3. Tạo bản ghi "Sổ kế toán" (Spending) cho Người Mua
                if (costMoney > 0)
                {
                    var moneyTrans = new MoneyTransaction
                    {
                        UserId = user.UserId,
                        ChapterId = chapter.ChapterId,
                        Amount = -costMoney, // Ghi sổ ÂM
                        Type = TransactionType.Spending,
                    };
                    _context.MoneyTransactions.Add(moneyTrans);
                }

                if (costActivePoint > 0)
                {
                    // (Giữ nguyên logic của ActivePoint)
                    var apTrans = new ActivePointTransaction
                    {
                        UserId = user.UserId,
                        Amount = -costActivePoint,
                        Type = TransactionType.Spending,
                    };
                    _context.ActivePointTransactions.Add(apTrans);
                }

                // --- PHẦN 2: XỬ LÝ NGƯỜI ĐĂNG (TÁC GIẢ) ---
                if (costMoney > 0)
                {
                    int uploaderId = chapter.Story.UploadedByUserId;

                    // Kiểm tra: Tác giả không thể tự mua và tự kiếm tiền
                    if (uploaderId != user.UserId)
                    {
                        // Tính số tiền tác giả kiếm được
                        int earnedAmount = (int)Math.Floor(costMoney * authorCommissionRate);

                        if (earnedAmount > 0)
                        {
                            var uploader = await _context.Users.FindAsync(uploaderId);
                            if (uploader != null && !uploader.IsDeleted)
                            {
                                // 4. Cộng tiền cho Tác giả
                                uploader.Money += earnedAmount;

                                // 5. Tạo bản ghi "Sổ kế toán" (Earning) cho Tác giả
                                var earnTrans = new MoneyTransaction
                                {
                                    UserId = uploader.UserId,
                                    ChapterId = chapter.ChapterId,
                                    Amount = earnedAmount, // Ghi sổ DƯƠNG
                                    Type = TransactionType.Earning,
                                };
                                _context.MoneyTransactions.Add(earnTrans);
                            }
                        }
                    }
                }

                // Lưu tất cả thay đổi (cả 2 user + 3 bảng)
                await _context.SaveChangesAsync();

                // Chốt giao dịch
                await dbTransaction.CommitAsync();

                return Ok("Mở khóa thành công.");
            }
            catch (Exception ex)
            {
                // ... (giữ nguyên logic rollback) ...
                await dbTransaction.RollbackAsync();
                return StatusCode(500, "Giao dịch thất bại, vui lòng thử lại.");
            }
        }
    }
}