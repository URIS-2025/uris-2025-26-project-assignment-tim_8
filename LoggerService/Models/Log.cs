using Microsoft.Identity.Client;

namespace LoggerService.Models
{
    public class Log
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;  
        public string? EntityName { get; set; }                       
        public string? OldValues { get; set; }   //stare vrednosti pre izmene
        public string? NewValues { get; set; }   //nove vrednosti posle izmene
        public bool IsSuccess { get; set; } = true;
        public string? ServiceName { get; set; }   // koji mikroservis je poslao log
        public string? HttpMethod { get; set; }    
    }
}
