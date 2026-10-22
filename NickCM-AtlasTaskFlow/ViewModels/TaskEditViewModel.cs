using System.ComponentModel.DataAnnotations;
using NickCM_AtlasTaskFlow.Models;
using TaskStatus = NickCM_AtlasTaskFlow.Models.TaskStatus;

namespace NickCM_AtlasTaskFlow.ViewModels
{
    public class TaskEditViewModel
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        public TaskPriority Priority { get; set; }

        public TaskStatus Status { get; set; }

        public string? OwnerId { get; set; }
    }
}