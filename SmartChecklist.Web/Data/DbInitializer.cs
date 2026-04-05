using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Models;

namespace SmartChecklist.Web.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                string[] roles = { "Admin", "ProjectManager", "TeamMember" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                await EnsureUserAsync(userManager, "admin@gmail.com", "Admin123!", "System Admin", "Admin", "Admin");
                await EnsureUserAsync(userManager, "pm@gmail.com", "Pm123456!", "Project Manager", "PM", "ProjectManager");
                await EnsureUserAsync(userManager, "member1@gmail.com", "Member123!", "Nguyễn Văn An", "An", "TeamMember");
                await EnsureUserAsync(userManager, "member2@gmail.com", "Member123!", "Trần Minh Trang", "Trang", "TeamMember");
                await EnsureUserAsync(userManager, "member3@gmail.com", "Member123!", "Lê Quốc Huy", "Huy", "TeamMember");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DbInitializer lỗi: " + ex.Message);
            }
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string fullName,
            string displayName,
            string roleName)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    DisplayName = displayName,
                    RoleName = roleName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, roleName);
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, roleName))
                {
                    await userManager.AddToRoleAsync(user, roleName);
                }

                user.RoleName = roleName;
                user.FullName = fullName;
                user.DisplayName = displayName;
                user.IsActive = true;

                await userManager.UpdateAsync(user);
            }

            return user;
        }

        public static async Task SeedDemoDataAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            if (await context.Projects.AnyAsync())
                return;

            var pm = await userManager.FindByEmailAsync("pm@gmail.com");
            var member1 = await userManager.FindByEmailAsync("member1@gmail.com");
            var member2 = await userManager.FindByEmailAsync("member2@gmail.com");
            var member3 = await userManager.FindByEmailAsync("member3@gmail.com");

            if (pm == null || member1 == null || member2 == null || member3 == null)
                return;

            var project1 = new Project
            {
                Name = "Hệ thống quản lý checklist nhóm dự án",
                Description = "Dự án xây dựng web quản lý checklist, task, tiến độ và báo cáo.",
                StartDate = DateTime.Today.AddDays(-20),
                EndDate = DateTime.Today.AddDays(20),
                Status = "Đang thực hiện",
                PriorityScore = 90,
                ManagerId = pm.Id,
                CreatedAt = DateTime.Now.AddDays(-20),
                IsDeleted = false
            };

            var project2 = new Project
            {
                Name = "Dashboard báo cáo nội bộ",
                Description = "Xây dựng dashboard tổng hợp hiệu suất và báo cáo nhóm.",
                StartDate = DateTime.Today.AddDays(-15),
                EndDate = DateTime.Today.AddDays(15),
                Status = "Đang thực hiện",
                PriorityScore = 75,
                ManagerId = pm.Id,
                CreatedAt = DateTime.Now.AddDays(-15),
                IsDeleted = false
            };

            context.Projects.AddRange(project1, project2);
            await context.SaveChangesAsync();

            context.ProjectMembers.AddRange(
                new ProjectMember { ProjectId = project1.ProjectId, UserId = member1.Id, MemberRole = "TeamMember", JoinedAt = DateTime.Now.AddDays(-18), IsActive = true },
                new ProjectMember { ProjectId = project1.ProjectId, UserId = member2.Id, MemberRole = "TeamMember", JoinedAt = DateTime.Now.AddDays(-18), IsActive = true },
                new ProjectMember { ProjectId = project1.ProjectId, UserId = member3.Id, MemberRole = "TeamMember", JoinedAt = DateTime.Now.AddDays(-18), IsActive = true },
                new ProjectMember { ProjectId = project2.ProjectId, UserId = member1.Id, MemberRole = "TeamMember", JoinedAt = DateTime.Now.AddDays(-12), IsActive = true },
                new ProjectMember { ProjectId = project2.ProjectId, UserId = member2.Id, MemberRole = "TeamMember", JoinedAt = DateTime.Now.AddDays(-12), IsActive = true }
            );
            await context.SaveChangesAsync();

            var checklist1 = new Checklist
            {
                ProjectId = project1.ProjectId,
                Title = "Phân tích yêu cầu",
                Description = "Phân tích use case và yêu cầu chức năng",
                WorkType = "Phân tích",
                CreatedBy = pm.Id,
                CreatedAt = DateTime.Now.AddDays(-18),
                IsDeleted = false
            };

            var checklist2 = new Checklist
            {
                ProjectId = project1.ProjectId,
                Title = "Phát triển chức năng",
                Description = "Xây dựng dashboard, task, checklist, notification",
                WorkType = "Lập trình",
                CreatedBy = pm.Id,
                CreatedAt = DateTime.Now.AddDays(-16),
                IsDeleted = false
            };

            var checklist3 = new Checklist
            {
                ProjectId = project1.ProjectId,
                Title = "Kiểm thử hệ thống",
                Description = "Kiểm thử luồng tạo task, reminder, báo cáo",
                WorkType = "Testing",
                CreatedBy = pm.Id,
                CreatedAt = DateTime.Now.AddDays(-14),
                IsDeleted = false
            };

            var checklist4 = new Checklist
            {
                ProjectId = project2.ProjectId,
                Title = "Thiết kế dashboard",
                Description = "Thiết kế giao diện báo cáo tổng quan",
                WorkType = "UI/UX",
                CreatedBy = pm.Id,
                CreatedAt = DateTime.Now.AddDays(-12),
                IsDeleted = false
            };

            var checklist5 = new Checklist
            {
                ProjectId = project2.ProjectId,
                Title = "Báo cáo hiệu suất",
                Description = "Thống kê và trình bày hiệu suất thành viên",
                WorkType = "Báo cáo",
                CreatedBy = pm.Id,
                CreatedAt = DateTime.Now.AddDays(-10),
                IsDeleted = false
            };

            context.Checklists.AddRange(checklist1, checklist2, checklist3, checklist4, checklist5);
            await context.SaveChangesAsync();

            var tasks = new List<TaskItem>
            {
                new TaskItem
                {
                    ChecklistId = checklist1.ChecklistId,
                    Title = "Phân tích actor và use case",
                    Description = "Hoàn thiện đặc tả actor Project Manager và Team Member",
                    Deadline = DateTime.Now.AddDays(-2),
                    Priority = "Cao",
                    Status = "Hoàn thành",
                    AssignedToUserId = member1.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-17),
                    CompletedAt = DateTime.Now.AddDays(-3),
                    UpdatedAt = DateTime.Now.AddDays(-3),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist1.ChecklistId,
                    Title = "Phân tích luồng reminder",
                    Description = "Mô tả luồng nhắc việc theo deadline",
                    Deadline = DateTime.Now.AddDays(-1),
                    Priority = "Trung bình",
                    Status = "Hoàn thành",
                    AssignedToUserId = member2.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-15),
                    CompletedAt = DateTime.Now.AddHours(-20),
                    UpdatedAt = DateTime.Now.AddHours(-20),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist2.ChecklistId,
                    Title = "Xây dựng Dashboard Team Member",
                    Description = "Hiển thị task ưu tiên, tiến độ cá nhân, dự án đang tham gia",
                    Deadline = DateTime.Now.AddDays(1),
                    Priority = "Cao",
                    Status = "Đang thực hiện",
                    AssignedToUserId = member1.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-8),
                    UpdatedAt = DateTime.Now.AddDays(-1),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist2.ChecklistId,
                    Title = "Xây dựng Dashboard Project Manager",
                    Description = "Hiển thị tổng dự án, checklist, task, quá hạn",
                    Deadline = DateTime.Now.AddDays(2),
                    Priority = "Cao",
                    Status = "Đang thực hiện",
                    AssignedToUserId = member2.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-9),
                    UpdatedAt = DateTime.Now.AddHours(-10),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist2.ChecklistId,
                    Title = "Tạo chức năng AI reminder",
                    Description = "Sinh lời nhắc thân thiện cho team member",
                    Deadline = DateTime.Now.AddHours(10),
                    Priority = "Khẩn cấp",
                    Status = "Chưa thực hiện",
                    AssignedToUserId = member1.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-4),
                    UpdatedAt = DateTime.Now.AddDays(-1),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist2.ChecklistId,
                    Title = "Tạo chức năng báo cáo hiệu suất",
                    Description = "Thống kê completion rate và overdue task",
                    Deadline = DateTime.Now.AddDays(-1),
                    Priority = "Cao",
                    Status = "Đang thực hiện",
                    AssignedToUserId = member2.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-7),
                    UpdatedAt = DateTime.Now.AddHours(-5),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist2.ChecklistId,
                    Title = "Tối ưu trang danh sách công việc",
                    Description = "Cải thiện tốc độ tải và hiển thị task",
                    Deadline = DateTime.Now.AddDays(4),
                    Priority = "Trung bình",
                    Status = "Chưa thực hiện",
                    AssignedToUserId = member3.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-5),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist3.ChecklistId,
                    Title = "Kiểm thử luồng cập nhật trạng thái task",
                    Description = "Kiểm thử Todo -> Doing -> Done",
                    Deadline = DateTime.Now.AddDays(2),
                    Priority = "Trung bình",
                    Status = "Chưa thực hiện",
                    AssignedToUserId = member3.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-2),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist4.ChecklistId,
                    Title = "Thiết kế giao diện báo cáo tổng quan",
                    Description = "Thiết kế card và tiến độ trực quan",
                    Deadline = DateTime.Now.AddDays(3),
                    Priority = "Trung bình",
                    Status = "Đang thực hiện",
                    AssignedToUserId = member1.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-6),
                    UpdatedAt = DateTime.Now.AddDays(-1),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist5.ChecklistId,
                    Title = "Viết phần nhận xét hiệu suất thành viên",
                    Description = "Tạo narrative summary cho manager",
                    Deadline = DateTime.Now.AddDays(5),
                    Priority = "Cao",
                    Status = "Chưa thực hiện",
                    AssignedToUserId = member2.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-3),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist5.ChecklistId,
                    Title = "Chuẩn bị dữ liệu mẫu để demo",
                    Description = "Seed project, task, log, reminder, notification",
                    Deadline = DateTime.Now.AddDays(1),
                    Priority = "Khẩn cấp",
                    Status = "Đang thực hiện",
                    AssignedToUserId = member2.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-3),
                    UpdatedAt = DateTime.Now.AddHours(-8),
                    IsDeleted = false
                },
                new TaskItem
                {
                    ChecklistId = checklist5.ChecklistId,
                    Title = "Rà soát giao diện notifications",
                    Description = "Kiểm tra giao diện hiển thị thông báo",
                    Deadline = DateTime.Now.AddDays(-3),
                    Priority = "Trung bình",
                    Status = "Hoàn thành",
                    AssignedToUserId = member3.Id,
                    CreatedByUserId = pm.Id,
                    CreatedAt = DateTime.Now.AddDays(-10),
                    CompletedAt = DateTime.Now.AddDays(-2),
                    UpdatedAt = DateTime.Now.AddDays(-2),
                    IsDeleted = false
                }
            };

            context.TaskItems.AddRange(tasks);
            await context.SaveChangesAsync();

            var progressLogs = new List<TaskProgressLog>();
            foreach (var task in tasks)
            {
                progressLogs.Add(new TaskProgressLog
                {
                    TaskItemId = task.TaskItemId,
                    OldStatus = "Chưa thực hiện",
                    NewStatus = task.Status == "Chưa thực hiện" ? "Chưa thực hiện" : "Đang thực hiện",
                    ChangedByUserId = task.AssignedToUserId ?? pm.Id,
                    ChangedAt = task.CreatedAt.AddHours(8),
                    Note = "Khởi tạo tiến độ ban đầu"
                });

                if (task.Status == "Đang thực hiện" || task.Status == "Hoàn thành")
                {
                    progressLogs.Add(new TaskProgressLog
                    {
                        TaskItemId = task.TaskItemId,
                        OldStatus = "Chưa thực hiện",
                        NewStatus = "Đang thực hiện",
                        ChangedByUserId = task.AssignedToUserId ?? pm.Id,
                        ChangedAt = task.CreatedAt.AddDays(1),
                        Note = "Bắt đầu xử lý công việc"
                    });
                }

                if (task.Status == "Hoàn thành" && task.CompletedAt.HasValue)
                {
                    progressLogs.Add(new TaskProgressLog
                    {
                        TaskItemId = task.TaskItemId,
                        OldStatus = "Đang thực hiện",
                        NewStatus = "Hoàn thành",
                        ChangedByUserId = task.AssignedToUserId ?? pm.Id,
                        ChangedAt = task.CompletedAt.Value,
                        Note = "Hoàn thành công việc"
                    });
                }
            }

            context.TaskProgressLogs.AddRange(progressLogs);

            var reminders = new List<Reminder>();
            var reminderCandidates = tasks
                .Where(t => t.Status != "Hoàn thành" && t.Deadline.HasValue)
                .Take(6)
                .ToList();

            foreach (var task in reminderCandidates)
            {
                reminders.Add(new Reminder
                {
                    TaskItemId = task.TaskItemId,
                    ReminderTime = task.Deadline!.Value.AddHours(-6),
                    ReminderType = "InApp",
                    IsSent = false,
                    CreatedAt = DateTime.Now.AddDays(-1)
                });
            }

            context.Reminders.AddRange(reminders);

            var notifications = new List<Notification>
            {
                new Notification
                {
                    UserId = member1.Id,
                    Title = "Công việc sắp đến hạn",
                    Message = "Bạn nên ưu tiên hoàn thiện Dashboard Team Member trước để tránh bị dồn việc.",
                    Type = "Reminder",
                    IsRead = false,
                    CreatedAt = DateTime.Now.AddHours(-3)
                },
                new Notification
                {
                    UserId = member2.Id,
                    Title = "Khối lượng công việc đang tăng",
                    Message = "Bạn đang xử lý khá nhiều việc quan trọng cùng lúc, nên ưu tiên chốt từng task trước.",
                    Type = "AI",
                    IsRead = false,
                    CreatedAt = DateTime.Now.AddHours(-2)
                },
                new Notification
                {
                    UserId = member3.Id,
                    Title = "Ghi nhận tiến độ tốt",
                    Message = "Bạn đã hoàn thành một số đầu việc đúng hạn, tiếp tục giữ nhịp này nhé.",
                    Type = "AI",
                    IsRead = false,
                    CreatedAt = DateTime.Now.AddHours(-1)
                }
            };

            context.Notifications.AddRange(notifications);

            await context.SaveChangesAsync();
        }
    }
}