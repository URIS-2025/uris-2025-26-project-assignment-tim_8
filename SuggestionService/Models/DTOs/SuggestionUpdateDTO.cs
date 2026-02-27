using AnonymousDomain.Enums;

namespace SuggestionService.Models.DTOs
{
    public class SuggestionUpdateDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ProblemSuggestionStatus Status { get; set; }
        public IEnumerable<Guid> CategoryIds { get; set; }
    }
}