using LoggerService.Context;
using LoggerService.Models;
using LoggerService.Models.DTOs;

namespace LoggerService.Data
{
    public class LoggerRepository : ILoggerRepository
    {
        private readonly LoggerContext _context;

        public LoggerRepository(LoggerContext context)
        {
            _context = context;
        }

        public IEnumerable<LogDTO> GetAll(int take = 100)
        {
            return _context.Logs
                .OrderByDescending(l => l.Timestamp)
                .Take(take)
                .Select(l => MapToDTO(l))
                .ToList();
        }

        public LogDTO? GetById(Guid id)
        {
            var log = _context.Logs.FirstOrDefault(l => l.Id == id);
            if (log == null) return null;
            return MapToDTO(log);
        }

        public IEnumerable<LogDTO> Search(
            string? userId,
            string? action,
            string? entityName,
            string? serviceName,
            string? httpMethod,
            bool? isSuccess,
            DateTime? fromUtc,
            DateTime? toUtc,
            int take = 100)
        {
            var query = _context.Logs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action.Contains(action));

            if (!string.IsNullOrEmpty(entityName))
                query = query.Where(l => l.EntityName == entityName);

            if (!string.IsNullOrEmpty(serviceName))
                query = query.Where(l => l.ServiceName == serviceName);

            if (!string.IsNullOrEmpty(httpMethod))
                query = query.Where(l => l.HttpMethod == httpMethod);

            if (isSuccess.HasValue)
                query = query.Where(l => l.IsSuccess == isSuccess.Value);

            if (fromUtc.HasValue)
                query = query.Where(l => l.Timestamp >= fromUtc.Value);

            if (toUtc.HasValue)
                query = query.Where(l => l.Timestamp <= toUtc.Value);

            return query
                .OrderByDescending(l => l.Timestamp)
                .Take(take)
                .Select(l => MapToDTO(l))
                .ToList();
        }

        public LogDTO Create(LogCreationDTO dto)
        {
            var log = new Log
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = dto.UserId,
                Action = dto.Action,
                EntityName = dto.EntityName,
                OldValues = dto.OldValues,
                NewValues = dto.NewValues,
                IsSuccess = dto.IsSuccess,
                ServiceName = dto.ServiceName,
                HttpMethod = dto.HttpMethod
            };

            _context.Logs.Add(log);
            _context.SaveChanges();

            return MapToDTO(log);
        }

        public void Delete(Guid id)
        {
            var log = _context.Logs.FirstOrDefault(l => l.Id == id);
            if (log == null) return;

            _context.Logs.Remove(log);
            _context.SaveChanges();
        }

        
        private static LogDTO MapToDTO(Log log) //ovo je metoda koja ce da pretvori log iz baze u logdto koji cemo da vratimo klijentu 
        {
            return new LogDTO
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                UserId = log.UserId,
                Action = log.Action,
                EntityName = log.EntityName,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                IsSuccess = log.IsSuccess,
                ServiceName = log.ServiceName,
                HttpMethod = log.HttpMethod
            };
        }
    }
}
