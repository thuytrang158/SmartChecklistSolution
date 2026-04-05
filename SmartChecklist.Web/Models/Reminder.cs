using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartChecklist.Web.Models
{
    public class Reminder
    {
        [Key]
        public int ReminderId { get; set; }

        [Required]
        public int TaskItemId { get; set; }

        [Required]
        [Display(Name = "Thời gian nhắc")]
        public DateTime ReminderTime { get; set; }

        [StringLength(50)]
        [Display(Name = "Loại nhắc nhở")]
        public string ReminderType { get; set; } = "InApp";

        public bool IsSent { get; set; } = false;

        public DateTime? SentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("TaskItemId")]
        public TaskItem? TaskItem { get; set; }
    }
}