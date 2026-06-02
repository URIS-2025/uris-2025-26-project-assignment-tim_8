namespace OrganizationService.Clients
{
    // Local copy of SystemNotificationService's SystemNotificationCreationDTO.
    // Duplicated per service (like LogCreationDTO) to avoid a cross-project reference.
    public class SystemNotificationCreationDTO
    {
        public string Text { get; set; } = string.Empty;
        public Guid? OrganizationId { get; set; }
        public Guid? AnonymousUserId { get; set; }
        public Guid? ProblemCommentId { get; set; }
        public Guid? SuggestionCommentId { get; set; }
    }
}
