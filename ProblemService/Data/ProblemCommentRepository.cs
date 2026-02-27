using AutoMapper;
using ProblemService.Context;
using ProblemService.Models.DTOs;
using ProblemService.Models.Problem;

namespace ProblemService.Data
{
    public class ProblemCommentRepository : IProblemCommentRepository
    {
        private readonly ProblemContext _context;
        private readonly IMapper _mapper;

        public ProblemCommentRepository(ProblemContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<ProblemCommentDTO> GetAllProblemComments()
        {
            var comments = _context.ProblemComments.ToList();
            var commentResult = new List<ProblemCommentDTO>();
            foreach (var comment in comments)
            {
                var dto = _mapper.Map<ProblemCommentDTO>(comment);
                commentResult.Add(dto);
            }
            return commentResult;
        }

        public ProblemCommentDTO GetProblemCommentById(Guid id)
        {
            var comment = _context.ProblemComments.FirstOrDefault(c => c.Id == id);
            if (comment == null)
                throw new ArgumentException("ProblemComment with that Id does not exist.");
            var dto = _mapper.Map<ProblemCommentDTO>(comment);
            return dto;
        }

        public ProblemCommentCreatedDTO CreateProblemComment(ProblemCommentCreationDTO comment)
        {
            if (comment.ProblemId == Guid.Empty)
                throw new ArgumentException("ProblemId must be provided.");
            if (comment.ProblemCommentAuthorId == Guid.Empty)
                throw new ArgumentException("ProblemCommentAuthorId must be provided.");
            if (string.IsNullOrWhiteSpace(comment.CommentText))
                throw new ArgumentException("CommentText must be provided.");

            var entity = _mapper.Map<ProblemComment>(comment);
            entity.Id = Guid.NewGuid();
            _context.ProblemComments.Add(entity);
            SaveChanges();
            return _mapper.Map<ProblemCommentCreatedDTO>(entity);
        }

        public ProblemCommentDTO UpdateProblemComment(ProblemCommentUpdateDTO comment)
        {
            var entity = _context.ProblemComments.FirstOrDefault(c => c.Id == comment.Id);
            if (entity == null)
                throw new ArgumentException("ProblemComment with that Id does not exist.");

            entity.CommentText = comment.CommentText;
            entity.IsAnonymous = comment.IsAnonymous;

            _context.ProblemComments.Update(entity);
            SaveChanges();
            var dto = _mapper.Map<ProblemCommentDTO>(entity);
            return dto;
        }

        public void DeleteProblemComment(Guid id)
        {
            var comment = _context.ProblemComments.FirstOrDefault(c => c.Id == id);
            if (comment == null)
                throw new ArgumentException("ProblemComment with that Id does not exist.");
            _context.ProblemComments.Remove(comment);
            SaveChanges();
        }
    }
}
