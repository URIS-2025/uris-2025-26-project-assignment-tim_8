namespace SuggestionBoxService.Models.DTOs
{
    public class SuggestionBoxUpdateDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDarkTheme { get; set; }
        public string Password { get; set; }
    }
}