using System.ComponentModel.DataAnnotations;

namespace NickCM_AtlasTaskFlow.ViewModels
{
    public class ProjectCreateViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Project name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End date")]
        public DateTime? EndDate { get; set; }
    }
}
