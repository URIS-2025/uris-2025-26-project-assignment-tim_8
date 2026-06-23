namespace SuggestionService.Models.DTOs
{
    public class SuggestionCreationDTO
    {

        public string Title { get; set; }
        public string Description { get; set; }
        public Guid SuggestionBoxId { get; set; }
        public Guid AnonymousUserId { get; set; }
        public IEnumerable<Guid> CategoryIds { get; set; }
        public string? BoxPassword { get; set; } // submitter-entered box password; never persisted
    }
}
