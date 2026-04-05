using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using System.Security.Claims;

namespace SmartChecklist.Web.Areas.TeamMember.Controllers
{
    [Area("TeamMember")]
    [Authorize(Roles = "TeamMember")]
    public class MyProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MyProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.DebugUserId = currentUserId;

            var projects = await _context.ProjectMembers
                .Include(pm => pm.Project)
                .Where(pm => pm.UserId == currentUserId && pm.IsActive && pm.Project != null && !pm.Project.IsDeleted)
                .Select(pm => pm.Project!)
                .Distinct()
                .ToListAsync();

            return View(projects);
        }

        public async Task<IActionResult> Details(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.ProjectMembers
                .Include(pm => pm.Project)
                .Where(pm => pm.UserId == currentUserId && pm.IsActive && pm.ProjectId == id)
                .Select(pm => pm.Project)
                .FirstOrDefaultAsync();

            if (project == null || project.IsDeleted)
                return NotFound();

            return View(project);
        }
    }
}