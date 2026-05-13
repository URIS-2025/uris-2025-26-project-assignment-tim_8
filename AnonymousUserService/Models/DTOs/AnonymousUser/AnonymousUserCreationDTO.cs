using System.ComponentModel.DataAnnotations;

namespace AnonymousUserService.Models.DTOs.AnonymousUser
{
    public class AnonymousUserCreationDTO
    {
        [Required]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
        [MaxLength(30, ErrorMessage = "Username must not exceed 30 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$",
            ErrorMessage = "Username may only contain letters, digits, and underscores.")]
        public string Username { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(64, ErrorMessage = "Password must not exceed 64 characters.")]
        public string Password { get; set; }
    }
}
