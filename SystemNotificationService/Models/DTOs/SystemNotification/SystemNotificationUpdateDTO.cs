namespace SystemNotificationService.Models.DTOs.SystemNotification
{
    public class SystemNotificationUpdateDTO
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? AnonymousUserId { get; set; }
        public Guid? ProblemCommentId { get; set; }
        public Guid? SuggestionCommentId { get; set; }
    }
}
