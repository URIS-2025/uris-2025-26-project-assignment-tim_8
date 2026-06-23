using System.ComponentModel.DataAnnotations;

namespace OrganizationService.Models.DTOs
{
    public class UserLoginDTO
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public string? CaptchaToken { get; set; }
    }
}
