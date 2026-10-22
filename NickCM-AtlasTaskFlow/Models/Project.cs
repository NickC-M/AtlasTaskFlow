using System.ComponentModel.DataAnnotations;

namespace NickCM_AtlasTaskFlow.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        public ApplicationUser CreatedBy { get; set; } = null!;

        public ICollection<ProjectMember> Members { get; set; }
            = new List<ProjectMember>();

        public ICollection<TaskItem> Tasks { get; set; }
            = new List<TaskItem>();

        public ICollection<ProjectActivity> Activities { get; set; }
            = new List<ProjectActivity>();
    }
}
