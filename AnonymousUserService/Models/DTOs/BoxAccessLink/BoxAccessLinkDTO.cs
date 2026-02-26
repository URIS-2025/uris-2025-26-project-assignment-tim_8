namespace AnonymousUserService.Models.DTOs.BoxAccessLink
{
    public class BoxAccessLinkDTO
    {
        public Guid Id { get; set; }
        public string AccessToken { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
