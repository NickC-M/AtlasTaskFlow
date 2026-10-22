using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NickCM_AtlasTaskFlow;
using NickCM_AtlasTaskFlow.Models;

namespace NickCM_AtlasTaskFlow.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Project> Projects => Set<Project>();

        public DbSet<ProjectMember> ProjectMembers
            => Set<ProjectMember>();

        public DbSet<TaskItem> TaskItems => Set<TaskItem>();

        public DbSet<TaskComment> TaskComments
            => Set<TaskComment>();

        public DbSet<ProjectActivity> ProjectActivities
            => Set<ProjectActivity>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //user can belong to a project only once
            builder.Entity<ProjectMember>()
                .HasKey(pm => new { pm.ProjectId, pm.UserId });

            //project creator
            builder.Entity<Project>()
                .HasOne(p => p.CreatedBy)
                .WithMany(u => u.CreatedProjects)
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            //project membership
            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            //tasks belong to projects
            builder.Entity<TaskItem>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            //task ownership is optional
            builder.Entity<TaskItem>()
                .HasOne(t => t.Owner)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            //SQL Server optimistic concurrency token
            builder.Entity<TaskItem>()
                .Property(t => t.RowVersion)
                .IsRowVersion();

            //task comments
            builder.Entity<TaskComment>()
                .HasOne(c => c.TaskItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskComment>()
                .HasOne(c => c.Author)
                .WithMany(u => u.TaskComments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            //activity history belongs to a project
            builder.Entity<ProjectActivity>()
                .HasOne(a => a.Project)
                .WithMany(p => p.Activities)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            //preserve project history if a task is deleted
            builder.Entity<ProjectActivity>()
                .HasOne<TaskItem>()
                .WithMany()
                .HasForeignKey(a => a.TaskItemId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ProjectActivity>()
                .HasOne(a => a.Actor)
                .WithMany()
                .HasForeignKey(a => a.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            
            builder.Entity<Project>()
                .Property(p => p.Name)
                .HasMaxLength(100);

            builder.Entity<TaskItem>()
                .HasIndex(t => new { t.ProjectId, t.Status });

            builder.Entity<TaskItem>()
                .HasIndex(t => new { t.ProjectId, t.OwnerId });

            builder.Entity<ProjectActivity>()
                .HasIndex(a => new { a.ProjectId, a.OccurredAtUtc });
        }
    }
}
