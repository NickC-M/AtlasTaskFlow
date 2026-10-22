namespace NickCM_AtlasTaskFlow.Models
{
    public enum ProjectRole
    {
        Member = 0,
        Manager = 1
    }

    public class ProjectMember
    {
        public int ProjectId { get; set; }

        public Project Project { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public ProjectRole Role { get; set; } = ProjectRole.Member;

        public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
