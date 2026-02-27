namespace SuggestionService.Models.DTOs
{
    public class VoteDTO
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid VoteAuthorId { get; set; }
        public Guid SuggestionId { get; set; }
    }
}
