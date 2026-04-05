using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartChecklist.Web.Models
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Tên dự án không được để trống")]
        [StringLength(150)]
        [Display(Name = "Tên dự án")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Mới tạo";

        [Display(Name = "Điểm ưu tiên")]
        public int PriorityScore { get; set; } = 0;

        [Required]
        public string ManagerId { get; set; } = string.Empty;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("ManagerId")]
        public ApplicationUser? Manager { get; set; }

        public ICollection<ProjectMember>? ProjectMembers { get; set; }
        public ICollection<Checklist>? Checklists { get; set; }
    }
}