namespace ProblemService.Clients
{
    public class LogCreationDTO
    {
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string ServiceName { get; set; } = string.Empty;
        public string? HttpMethod { get; set; }
    }
}
