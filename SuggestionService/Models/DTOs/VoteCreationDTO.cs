namespace SuggestionService.Models.DTOs
{
    public class VoteCreationDTO
    {
        public Guid VoteAuthorId { get; set; }
        public Guid SuggestionId { get; set; }
    }
}
