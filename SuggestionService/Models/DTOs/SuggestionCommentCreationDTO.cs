namespace SuggestionService.Models.DTOs
{
    public class SuggestionCommentCreationDTO
    {
        public string Text { get; set; }
        public bool IsAnonymous { get; set; }
        public Guid SuggestionId { get; set; }
        public Guid? SuggestionCommentId { get; set; } // null ako nije reply
        public Guid CommentAuthorId { get; set; }
        public string CreatedBy { get; set; }




    }
}
