using System.ComponentModel.DataAnnotations;

namespace NickCM_AtlasTaskFlow.ViewModels
{
    public class AddProjectMemberViewModel
    {
        public int ProjectId { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Member's email address")]
        public string Email { get; set; } = string.Empty;
    }
}