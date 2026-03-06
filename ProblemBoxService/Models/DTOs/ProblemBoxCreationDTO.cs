using ProblemBoxService.Enums;

namespace ProblemBoxService.Models.DTOs
{
    public class ProblemBoxCreationDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDarkTheme { get; set; }
        public string Password { get; set; }
        public string CreatedBy { get; set; }
        public Guid OrganizationId { get; set; }
    }
}
