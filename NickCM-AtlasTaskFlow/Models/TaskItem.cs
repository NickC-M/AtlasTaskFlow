using System.ComponentModel.DataAnnotations;


namespace NickCM_AtlasTaskFlow.Models
{
    public enum TaskStatus
    {
        ToDo = 0,
        InProgress = 1,
        Review = 2,
        Completed = 3
    }

    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public TaskStatus Status { get; set; } = TaskStatus.ToDo;

        public int ProjectId { get; set; }

        public Project Project { get; set; } = null!;

        //null means the task is unclaimed.
        public string? OwnerId { get; set; }

        public ApplicationUser? Owner { get; set; }

        //SQL Server uses this to detect concurrent updates
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<TaskComment> Comments { get; set; }
            = new List<TaskComment>();
    }
}
