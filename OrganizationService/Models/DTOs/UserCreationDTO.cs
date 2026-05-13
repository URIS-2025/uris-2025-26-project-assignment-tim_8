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
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        [MaxLength(150)]
        public string Email { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(64, ErrorMessage = "Password must not exceed 64 characters.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one digit, and one special character.")]
        public string Password { get; set; }

        [Required]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
        [MaxLength(150, ErrorMessage = "Username must not exceed 150 characters.")]
        public string Username { get; set; }

        [Required]
        public Guid RoleId { get; set; }

        public Guid? OrganizationId { get; set; }
    }
}
