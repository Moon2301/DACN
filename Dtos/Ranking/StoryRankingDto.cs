

namespace DACN.Dtos
{
    public class StoryRankingDto
    {
        public int StoryId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string CoverImage { get; set; }

        // Một trường chung để chứa giá trị xếp hạng (lượt đọc, bình luận, v.v.)
        public int Value { get; set; }
    }
}