namespace ProblemService.Models.DTOs
{
    public class ProblemCommentUpdateDTO
    {
        public Guid Id { get; set; }
        public string CommentText { get; set; }
        public bool IsAnonymous { get; set; }
    }
}
