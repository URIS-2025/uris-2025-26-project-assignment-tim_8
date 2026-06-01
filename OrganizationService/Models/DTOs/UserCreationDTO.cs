using System.ComponentModel.DataAnnotations;

namespace OrganizationService.Models.DTOs
{
    public class UserCreationDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(100)]
        public string Surname { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public Guid RoleId { get; set; }

        public Guid? OrganizationId { get; set; }
    }
}
