namespace AnonymousUserService.Models.DTOs.AnonymousUser
{
    public class AnonymousUserDTO
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid BoxAccessLinkId { get; set; }
    }
}
