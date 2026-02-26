using ProblemBoxService.Enums;

namespace ProblemBoxService.Models.DTOs
{
    public class ProblemBoxUpdateDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDarkTheme { get; set; }
        public string Password { get; set; }
        public ProblemSuggestionStatus Status { get; set; }
    }
}
