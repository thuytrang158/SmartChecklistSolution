using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartChecklist.Web.Models
{
    public class Checklist
    {
        [Key]
        public int ChecklistId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Tên checklist không được để trống")]
        [StringLength(150)]
        [Display(Name = "Tên checklist")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Display(Name = "Loại công việc")]
        public string? WorkType { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        public ICollection<TaskItem>? TaskItems { get; set; }
    }
}