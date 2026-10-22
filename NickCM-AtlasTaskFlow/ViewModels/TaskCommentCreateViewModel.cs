using System.ComponentModel.DataAnnotations;

namespace NickCM_AtlasTaskFlow.ViewModels
{
    public class TaskCommentCreateViewModel
    {
        public int TaskItemId { get; set; }

        [Required(ErrorMessage = "Please enter a comment.")]
        [StringLength(3000, ErrorMessage =
            "Comments cannot exceed 3,000 characters.")]
        public string Content { get; set; } = string.Empty;
    }
}
