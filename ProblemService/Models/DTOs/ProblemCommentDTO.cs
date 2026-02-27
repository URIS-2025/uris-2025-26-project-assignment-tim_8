namespace ProblemService.Models.DTOs
{
    public class ProblemCommentDTO
    {
        public Guid Id { get; set; }
        public string CommentText { get; set; }
        public bool IsAnonymous { get; set; }
        public Guid? ProblemCommentId { get; set; }
        public Guid ProblemId { get; set; }
        public Guid ProblemCommentAuthorId { get; set; }
    }
}
