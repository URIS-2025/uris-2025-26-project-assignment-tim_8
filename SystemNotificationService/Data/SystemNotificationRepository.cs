using AnonymousDomain.Models.SystemNotification;
using AutoMapper;
using SystemNotificationService.Context;
using SystemNotificationService.Models.DTOs.SystemNotification;

namespace SystemNotificationService.Data
{
    public class SystemNotificationRepository : ISystemNotificationRepository
    {

        private readonly SystemNotificationContext _context;
        private readonly IMapper _mapper;

        public SystemNotificationRepository(SystemNotificationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<SystemNotificationCreatedDTO> GetAllSystemNotifications()
        {
            var notifications = _context.SystemNotifications.ToList();
            return _mapper.Map<IEnumerable<SystemNotificationCreatedDTO>>(notifications);
        }

        public SystemNotificationCreatedDTO CreateSystemNotification(SystemNotificationCreationDTO systemNotification)
        {
            var hasProblem = systemNotification.ProblemCommentId.HasValue;
            var hasSuggestion = systemNotification.SuggestionCommentId.HasValue;

            if (!hasProblem && !hasSuggestion)
                throw new ArgumentException(
                    "Either ProblemCommentId or SuggestionCommentId must be provided.");

            if (hasProblem && hasSuggestion)
                throw new ArgumentException(
                    "Cannot provide both ProblemCommentId and SuggestionCommentId. Choose one.");

            var entity = _mapper.Map<SystemNotification>(systemNotification);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.IsRead = false;

            _context.SystemNotifications.Add(entity);
            SaveChanges();
            return _mapper.Map<SystemNotificationCreatedDTO>(entity);
        }
        public void DeleteSystemNotification(Guid id)
        {
            var notification = _context.SystemNotifications.Find(id);
            if (notification == null)
                throw new ArgumentException($"SystemNotification with ID {id} not found.");

            _context.SystemNotifications.Remove(notification);
            SaveChanges();
        }
    }
}
