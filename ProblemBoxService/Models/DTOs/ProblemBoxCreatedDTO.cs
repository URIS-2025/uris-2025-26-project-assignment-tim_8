using ProblemBoxService.Enums;

namespace ProblemBoxService.Models.DTOs
{
    public class ProblemBoxCreatedDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDarkTheme { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid BoxAccessLinkId { get; set; }
        public ProblemSuggestionStatus Status { get; set; }
    }
}
