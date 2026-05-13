using System.ComponentModel.DataAnnotations;

namespace OrganizationService.Models.DTOs
{
    public class RefreshTokenRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
