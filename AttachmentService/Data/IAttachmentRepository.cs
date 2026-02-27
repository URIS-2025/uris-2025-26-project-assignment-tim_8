using AttachmentService.Models.Attachment.DTOs;
using AttachmentService.Models.DTOs;

namespace AttachmentService.Interfaces
{
    public interface IAttachmentRepository
    {
        bool SaveChanges();
        IEnumerable<AttachmentDTO> GetAll();
        AttachmentDTO GetById(Guid id);
        IEnumerable<AttachmentDTO> GetBySuggestionId(Guid suggestionId);
        IEnumerable<AttachmentDTO> GetByProblemId(Guid problemId);
        AttachmentDTO Create(AttachmentCreationDTO attachment);
        AttachmentDTO Update(AttachmentUpdateDTO attachment);
        void Delete(Guid id);
    }
}