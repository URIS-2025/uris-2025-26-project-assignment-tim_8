namespace SystemNotificationService.Models.DTOs.SystemNotification
{
    public class SystemNotificationDTO
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public bool IsRead { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? AnonymousUserId { get; set; }
        public Guid? ProblemCommentId { get; set; }
        public Guid? SuggestionCommentId { get; set; }
    }
}
