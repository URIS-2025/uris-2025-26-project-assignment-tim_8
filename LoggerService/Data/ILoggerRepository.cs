using LoggerService.Models.DTOs;

namespace LoggerService.Data
{
    public interface ILoggerRepository
    {
        IEnumerable<LogDTO> GetAll(int take = 100);

        LogDTO? GetById(Guid id);

        IEnumerable<LogDTO> Search(
            string? userId,
            string? action,
            string? entityName,
            string? serviceName,
            string? httpMethod,
            bool? isSuccess,
            DateTime? fromUtc,
            DateTime? toUtc,
            int take = 100);

        LogDTO Create(LogCreationDTO dto);

        void Delete(Guid id);
    }
}
