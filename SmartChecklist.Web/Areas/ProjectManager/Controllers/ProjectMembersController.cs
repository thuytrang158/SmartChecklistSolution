using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Models;
using System.Security.Claims;

namespace SmartChecklist.Web.Areas.ProjectManager.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class ProjectMembersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectMembersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ManagerId == userId && !p.IsDeleted);

            if (project == null)
                return NotFound();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project.Name;

            var members = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == projectId && pm.IsActive)
                .ToListAsync();

            return View(members);
        }

        public async Task<IActionResult> Overview()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var members = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Include(pm => pm.Project)
                .Where(pm => pm.IsActive
                             && pm.Project != null
                             && !pm.Project.IsDeleted
                             && pm.Project.ManagerId == userId)
                .OrderBy(pm => pm.Project!.Name)
                .ThenBy(pm => pm.User!.Email)
                .ToListAsync();

            ViewBag.IsOverview = true;
            return View("Index", members);
        }

        public async Task<IActionResult> Create(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ManagerId == userId && !p.IsDeleted);

            if (project == null)
                return NotFound();

            var existingUserIds = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId && pm.IsActive)
                .Select(pm => pm.UserId)
                .ToListAsync();

            var users = await _userManager.Users
                .Where(u => !existingUserIds.Contains(u.Id))
                .ToListAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project.Name;
            ViewBag.UserId = new SelectList(users, "Id", "Email");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int projectId, string userId, string memberRole)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ManagerId == currentUserId && !p.IsDeleted);

            if (project == null)
                return NotFound();

            var existed = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && pm.IsActive);

            if (existed)
            {
                ModelState.AddModelError("", "Người dùng này đã là thành viên của dự án.");
            }

            if (!ModelState.IsValid)
            {
                var existingUserIds = await _context.ProjectMembers
                    .Where(pm => pm.ProjectId == projectId && pm.IsActive)
                    .Select(pm => pm.UserId)
                    .ToListAsync();

                var users = await _userManager.Users
                    .Where(u => !existingUserIds.Contains(u.Id))
                    .ToListAsync();

                ViewBag.ProjectId = projectId;
                ViewBag.ProjectName = project.Name;
                ViewBag.UserId = new SelectList(users, "Id", "Email", userId);

                return View();
            }

            var projectMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                MemberRole = memberRole,
                JoinedAt = DateTime.Now,
                IsActive = true
            };

            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { projectId });
        }

        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var projectMember = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Include(pm => pm.Project)
                .FirstOrDefaultAsync(pm => pm.ProjectMemberId == id && pm.IsActive);

            if (projectMember == null)
                return NotFound();

            if (projectMember.Project == null || projectMember.Project.ManagerId != currentUserId)
                return Forbid();

            return View(projectMember);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var projectMember = await _context.ProjectMembers
                .Include(pm => pm.Project)
                .FirstOrDefaultAsync(pm => pm.ProjectMemberId == id && pm.IsActive);

            if (projectMember == null)
                return NotFound();

            if (projectMember.Project == null || projectMember.Project.ManagerId != currentUserId)
                return Forbid();

            projectMember.IsActive = false;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { projectId = projectMember.ProjectId });
        }
    }
}