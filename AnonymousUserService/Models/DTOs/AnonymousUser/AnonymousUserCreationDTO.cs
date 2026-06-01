using System.ComponentModel.DataAnnotations;

namespace AnonymousUserService.Models.DTOs.AnonymousUser
{
    public class AnonymousUserCreationDTO
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
