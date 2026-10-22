using System.ComponentModel.DataAnnotations;

namespace NickCM_AtlasTaskFlow.Models
{
    public class TaskComment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(3000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int TaskItemId { get; set; }

        public TaskItem TaskItem { get; set; } = null!;

        [Required]
        public string AuthorId { get; set; } = string.Empty;

        public ApplicationUser Author { get; set; } = null!;
    }
}
