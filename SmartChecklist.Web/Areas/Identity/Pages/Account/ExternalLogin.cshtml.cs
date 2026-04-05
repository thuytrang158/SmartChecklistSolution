#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SmartChecklist.Web.Models;

namespace SmartChecklist.Web.Areas.Identity.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }
        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
            [StringLength(100)]
            [Display(Name = "Họ và tên")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
            [Display(Name = "Vai trò")]
            public string RoleName { get; set; } = "TeamMember";
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Lỗi từ nhà cung cấp ngoài: {remoteError}";
                return RedirectToPage("./Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "Không thể tải thông tin đăng nhập ngoài.";
                return RedirectToPage("./Login");
            }

            // Nếu tài khoản Google này đã liên kết rồi thì đăng nhập luôn
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (result.Succeeded)
            {
                var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (existingUser != null)
                {
                    existingUser.RoleName ??= await GetFirstRoleAsync(existingUser);
                    await _userManager.UpdateAsync(existingUser);

                    _logger.LogInformation("{Name} đã đăng nhập bằng {LoginProvider}.", existingUser.Email, info.LoginProvider);

                    return RedirectToDashboard(existingUser.RoleName);
                }

                return LocalRedirect(returnUrl);
            }

            // Lấy email từ Google
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Không lấy được email từ tài khoản Google.");
                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                Input = new InputModel();
                return Page();
            }

            // Nếu email đã tồn tại trong hệ thống nhưng chưa link Google
            var userByEmail = await _userManager.FindByEmailAsync(email);
            if (userByEmail != null)
            {
                // Tự động liên kết Google với tài khoản hiện có
                var addLoginResult = await _userManager.AddLoginAsync(userByEmail, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(userByEmail, isPersistent: false);
                    userByEmail.RoleName ??= await GetFirstRoleAsync(userByEmail);
                    await _userManager.UpdateAsync(userByEmail);

                    _logger.LogInformation("Đã liên kết tài khoản Google với user có email {Email}.", email);
                    return RedirectToDashboard(userByEmail.RoleName);
                }

                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                Input = new InputModel
                {
                    Email = email
                };
                return Page();
            }

            // Email chưa tồn tại => hiện form nhập thêm FullName + Role
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;
            Input = new InputModel
            {
                Email = email
            };

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                ReturnUrl = returnUrl;
                return Page();
            }

            if (Input.RoleName != "ProjectManager" && Input.RoleName != "TeamMember")
            {
                ModelState.AddModelError(string.Empty, "Vai trò không hợp lệ.");
                ReturnUrl = returnUrl;
                return Page();
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "Không thể tải thông tin đăng nhập ngoài trong lúc xác nhận.";
                return RedirectToPage("./Login");
            }

            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Email này đã tồn tại trong hệ thống.");
                ReturnUrl = returnUrl;
                return Page();
            }

            var user = CreateUser();
            user.FullName = Input.FullName;
            user.DisplayName = Input.FullName;
            user.RoleName = Input.RoleName;
            user.CreatedAt = DateTime.Now;
            user.IsActive = true;
            user.UserName = Input.Email;
            user.Email = Input.Email;
            user.EmailConfirmed = true;

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Input.RoleName);
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    _logger.LogInformation("Người dùng đã tạo tài khoản mới bằng {LoginProvider}.", info.LoginProvider);
                    return RedirectToDashboard(Input.RoleName);
                }

                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ReturnUrl = returnUrl;
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Không thể tạo instance của {nameof(ApplicationUser)}.");
            }
        }

        private async Task<string> GetFirstRoleAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.Count > 0 ? roles[0] : "TeamMember";
        }

        private IActionResult RedirectToDashboard(string roleName)
        {
            if (roleName == "ProjectManager")
            {
                return LocalRedirect("~/ProjectManager/Dashboard");
            }

            return LocalRedirect("~/TeamMember/Dashboard");
        }
    }
}