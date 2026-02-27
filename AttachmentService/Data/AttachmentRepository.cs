using AnonymousDomain.Models.Attachment;
using AttachmentService.Context;
using AutoMapper;
using AttachmentService.Models.DTOs;
using AttachmentService.Repositories;
using AttachmentService.Models.Attachment.DTOs;
using AttachmentService.Interfaces;

namespace AttachmentService.Repositories
{
    public class AttachmentRepository : IAttachmentRepository
    {
        private readonly AttachmentContext _context;
        private readonly IMapper _mapper;

        public AttachmentRepository(AttachmentContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<AttachmentDTO> GetAll()
        {
            var attachments = _context.Attachments.ToList();
            return _mapper.Map<IEnumerable<AttachmentDTO>>(attachments);
        }

        public AttachmentDTO GetById(Guid id)
        {
            var attachment = _context.Attachments.FirstOrDefault(a => a.Id == id);
            return _mapper.Map<AttachmentDTO>(attachment);
        }

        public IEnumerable<AttachmentDTO> GetBySuggestionId(Guid suggestionId)
        {
            var attachments = _context.Attachments
                                      .Where(a => a.SuggestionId == suggestionId)
                                      .ToList();
            return _mapper.Map<IEnumerable<AttachmentDTO>>(attachments);
        }

        public IEnumerable<AttachmentDTO> GetByProblemId(Guid problemId)
        {
            var attachments = _context.Attachments
                                      .Where(a => a.ProblemId == problemId)
                                      .ToList();
            return _mapper.Map<IEnumerable<AttachmentDTO>>(attachments);
        }

        public AttachmentDTO Create(AttachmentCreationDTO attachmentDto)
        {
            var attachment = _mapper.Map<Attachment>(attachmentDto);
            attachment.UploadedAt = DateTime.UtcNow;
            _context.Attachments.Add(attachment);
            SaveChanges();
            return _mapper.Map<AttachmentDTO>(attachment);
        }

        public AttachmentDTO Update(AttachmentUpdateDTO attachmentDto)
        {
            var attachment = _context.Attachments.FirstOrDefault(a => a.Id == attachmentDto.Id);
            _mapper.Map(attachmentDto, attachment);
            SaveChanges();
            return _mapper.Map<AttachmentDTO>(attachment);
        }

        public void Delete(Guid id)
        {
            var attachment = _context.Attachments.FirstOrDefault(a => a.Id == id);
            if (attachment != null)
            {
                _context.Attachments.Remove(attachment);
                SaveChanges();
            }
        }
    }
}