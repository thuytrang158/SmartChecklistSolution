using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartChecklist.Web.Models
{
    public class TaskProgressLog
    {
        [Key]
        public int TaskProgressLogId { get; set; }

        [Required]
        public int TaskItemId { get; set; }

        [StringLength(50)]
        public string? OldStatus { get; set; }

        [StringLength(50)]
        public string? NewStatus { get; set; }

        [Required]
        public string ChangedByUserId { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.Now;

        [StringLength(1000)]
        public string? Note { get; set; }

        [ForeignKey("TaskItemId")]
        public TaskItem? TaskItem { get; set; }

        [ForeignKey("ChangedByUserId")]
        public ApplicationUser? ChangedByUser { get; set; }
    }
}