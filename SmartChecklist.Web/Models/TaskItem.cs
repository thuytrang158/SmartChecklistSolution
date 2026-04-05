using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartChecklist.Web.Models
{
    public class TaskItem
    {
        [Key]
        public int TaskItemId { get; set; }

        [Required]
        public int ChecklistId { get; set; }

        [Required(ErrorMessage = "Tiêu đề công việc không được để trống")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Hạn chót")]
        [DataType(DataType.DateTime)]
        public DateTime? Deadline { get; set; }

        [StringLength(50)]
        [Display(Name = "Mức độ ưu tiên")]
        public string Priority { get; set; } = "Trung bình";

        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Chưa thực hiện";

        [Display(Name = "Người phụ trách")]
        public string? AssignedToUserId { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Ngày hoàn thành")]
        public DateTime? CompletedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("ChecklistId")]
        public Checklist? Checklist { get; set; }

        [ForeignKey("AssignedToUserId")]
        public ApplicationUser? AssignedToUser { get; set; }

        [ForeignKey("CreatedByUserId")]
        public ApplicationUser? CreatedByUser { get; set; }

        public ICollection<Reminder>? Reminders { get; set; }
        public ICollection<TaskProgressLog>? TaskProgressLogs { get; set; }
    }
}