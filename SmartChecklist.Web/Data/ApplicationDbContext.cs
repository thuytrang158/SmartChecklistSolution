using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Models;

namespace SmartChecklist.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Checklist> Checklists { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AiSuggestionHistory> AiSuggestionHistories { get; set; }
        public DbSet<TaskProgressLog> TaskProgressLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProjectMember>()
                .HasIndex(x => new { x.ProjectId, x.UserId })
                .IsUnique();

            builder.Entity<Project>()
                .HasOne(p => p.Manager)
                .WithMany()
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TaskProgressLog>()
                .HasOne(tpl => tpl.TaskItem)
                .WithMany(t => t.TaskProgressLogs)
                .HasForeignKey(tpl => tpl.TaskItemId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TaskProgressLog>()
                .HasOne(tpl => tpl.ChangedByUser)
                .WithMany()
                .HasForeignKey(tpl => tpl.ChangedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }

    }
}