using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartChecklist.Web.Models
{
    public class ProjectMember
    {
        [Key]
        public int ProjectMemberId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Vai trò trong dự án")]
        public string MemberRole { get; set; } = "TeamMember";

        [Display(Name = "Ngày tham gia")]
        public DateTime JoinedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}