using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartChecklist.Web.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Tên hiển thị")]
        public string? DisplayName { get; set; }

        [Display(Name = "Vai trò")]
        public string? RoleName { get; set; }

        public string? AvatarUrl { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }
    }
}