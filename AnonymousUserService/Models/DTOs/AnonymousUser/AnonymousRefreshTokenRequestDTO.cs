using System.ComponentModel.DataAnnotations;

namespace AnonymousUserService.Models.DTOs.AnonymousUser
{
    public class AnonymousRefreshTokenRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
