using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartChecklist.Web.Models
{
    public class AiSuggestionHistory
    {
        [Key]
        public int AiSuggestionHistoryId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Loại gợi ý")]
        public string SuggestionType { get; set; } = string.Empty;

        [Display(Name = "Ngữ cảnh đầu vào")]
        public string? InputContext { get; set; }

        [Display(Name = "Kết quả gợi ý")]
        public string? OutputSuggestion { get; set; }

        [Display(Name = "Người dùng chấp nhận")]
        public bool Accepted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}