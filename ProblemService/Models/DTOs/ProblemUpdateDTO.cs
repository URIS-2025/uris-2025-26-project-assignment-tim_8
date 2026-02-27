using ProblemService.Enums;

namespace ProblemService.Models.DTOs
{
    public class ProblemUpdateDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid ProblemBoxId { get; set; }
        public ProblemSuggestionStatus Status { get; set; }
        public ProblemPriority Priority { get; set; }
    }
}
