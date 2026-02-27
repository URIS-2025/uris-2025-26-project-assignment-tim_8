

using AnonymousDomain.Enums;

namespace SuggestionService.Models.DTOs
{
    public class SuggestionCreatedDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ProblemSuggestionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid SuggestionBoxId { get; set; }
        public Guid AnonymousUserId { get; set; }
    }
}
