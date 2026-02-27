namespace SuggestionService.Models.DTOs
{
    public class SuggestionCommentDTO
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public bool IsAnonymous { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? SuggestionId { get; set; }
        public Guid? SuggestionCommentId { get; set; } //ako je komentar reply nekom komentaru ili ako nije null
        public Guid CommentAuthorId { get; set; }
    }
}
