namespace OrganizationService.Models.DTOs
{
    public class OrganizationCreationDTO
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid AdminId { get; set; }
    }
}
