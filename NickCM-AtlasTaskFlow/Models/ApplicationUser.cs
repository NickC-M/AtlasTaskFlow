using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace NickCM_AtlasTaskFlow.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? DisplayName { get; set; }

        public ICollection<Project> CreatedProjects { get; set; }
            = new List<Project>();

        public ICollection<ProjectMember> ProjectMemberships { get; set; }
            = new List<ProjectMember>();

        public ICollection<TaskItem> AssignedTasks { get; set; }
            = new List<TaskItem>();

        public ICollection<TaskComment> TaskComments { get; set; }
            = new List<TaskComment>();
    }
}
