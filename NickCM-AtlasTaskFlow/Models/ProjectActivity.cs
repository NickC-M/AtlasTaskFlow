using System.ComponentModel.DataAnnotations;

namespace NickCM_AtlasTaskFlow.Models
{
    public class ProjectActivity
    {
        public long Id { get; set; }

        public int ProjectId { get; set; }

        public Project Project { get; set; } = null!;

        public int? TaskItemId { get; set; }

        [Required]
        public string ActorId { get; set; } = string.Empty;

        public ApplicationUser Actor { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Details { get; set; }

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }
}
