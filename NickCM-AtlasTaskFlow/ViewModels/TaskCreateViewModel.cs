using System.ComponentModel.DataAnnotations;
using NickCM_AtlasTaskFlow.Models;

namespace NickCM_AtlasTaskFlow.ViewModels
{
    public class TaskCreateViewModel
    {
        public int ProjectId { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Task title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        [Display(Name = "Due date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        public TaskPriority Priority { get; set; }
            = TaskPriority.Medium;

        [Display(Name = "Assign to a team member")]
        public string? OwnerId { get; set; }
    }
}