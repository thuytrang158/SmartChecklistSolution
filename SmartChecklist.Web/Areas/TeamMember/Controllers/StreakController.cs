using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.ViewModels;
using System.Security.Claims;

namespace SmartChecklist.Web.Areas.TeamMember.Controllers
{
    [Area("TeamMember")]
    [Authorize(Roles = "TeamMember")]
    public class StreakController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StreakController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var completionDays = await _context.TaskItems
                .Where(t => t.AssignedToUserId == currentUserId
                            && t.CompletedAt != null
                            && !t.IsDeleted)
                .Select(t => t.CompletedAt!.Value.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            int currentStreak = 0;
            int bestStreak = 0;

            if (completionDays.Any())
            {
                var today = DateTime.Today;
                var expectedDay = completionDays.Contains(today) ? today : today.AddDays(-1);

                foreach (var day in completionDays)
                {
                    if (day == expectedDay)
                    {
                        currentStreak++;
                        expectedDay = expectedDay.AddDays(-1);
                    }
                    else if (day < expectedDay)
                    {
                        break;
                    }
                }

                int tempStreak = 1;
                for (int i = 1; i < completionDays.Count; i++)
                {
                    if (completionDays[i - 1].AddDays(-1) == completionDays[i])
                    {
                        tempStreak++;
                    }
                    else
                    {
                        bestStreak = Math.Max(bestStreak, tempStreak);
                        tempStreak = 1;
                    }
                }

                bestStreak = Math.Max(bestStreak, tempStreak);
            }

            var model = new StreakViewModel
            {
                CurrentStreak = currentStreak,
                BestStreak = bestStreak,
                CompletionDays = completionDays.Take(30).ToList()
            };

            return View(model);
        }
    }
}