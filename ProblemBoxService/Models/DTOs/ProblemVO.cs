using ProblemBoxService.Enums;

namespace ProblemBoxService.Models.DTOs
{
    public class ProblemVO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ProblemSuggestionStatus Status { get; set; }
        public ProblemPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
