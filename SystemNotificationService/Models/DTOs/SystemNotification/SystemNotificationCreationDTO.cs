namespace SystemNotificationService.Models.DTOs.SystemNotification
{
    public class SystemNotificationCreationDTO
    {
        public string Text { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? AnonymousUserId { get; set; }
        public Guid? ProblemCommentId { get; set; }
        public Guid? SuggestionCommentId { get; set; }
    }
}
