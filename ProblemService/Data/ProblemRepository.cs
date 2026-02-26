using AutoMapper;
using ProblemService.Context;
using ProblemService.Models.DTOs;
using ProblemService.Models.Problem;

namespace ProblemService.Data
{
    public class ProblemRepository : IProblemRepository
    {
        private readonly ProblemContext _context;
        private readonly IMapper _mapper;

        public ProblemRepository(ProblemContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<ProblemDTO> GetAllProblems()
        {
            var problems = _context.Problems.ToList();
            var problemResult = new List<ProblemDTO>();
            foreach (var problem in problems)
            {
                var dto = _mapper.Map<ProblemDTO>(problem);
                problemResult.Add(dto);
            }
            return problemResult;
        }

        public ProblemDTO GetProblemById(Guid id)
        {
            var problem = _context.Problems.FirstOrDefault(p => p.Id == id);
            if (problem == null)
                throw new ArgumentException("Problem with that Id does not exist.");
            var dto = _mapper.Map<ProblemDTO>(problem);
            return dto;
        }

        public IEnumerable<ProblemDTO> GetProblemsByProblemBoxId(Guid problemBoxId)
        {
            var problems = _context.Problems.Where(p => p.ProblemBoxId == problemBoxId).ToList();
            var problemResult = new List<ProblemDTO>();
            foreach (var problem in problems)
            {
                var dto = _mapper.Map<ProblemDTO>(problem);
                problemResult.Add(dto);
            }
            return problemResult;
        }

        public ProblemCreatedDTO CreateProblem(ProblemCreationDTO problem)
        {
            if (problem.ProblemBoxId == Guid.Empty)
                throw new ArgumentException("ProblemBoxId must be provided.");
            if (string.IsNullOrWhiteSpace(problem.Title))
                throw new ArgumentException("Title must be provided.");
            if (string.IsNullOrWhiteSpace(problem.Description))
                throw new ArgumentException("Description must be provided.");

            var entity = _mapper.Map<Problem>(problem);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            _context.Problems.Add(entity);
            SaveChanges();
            return _mapper.Map<ProblemCreatedDTO>(entity);
        }

        public ProblemDTO UpdateProblem(ProblemUpdateDTO problem)
        {
            var entity = _context.Problems.FirstOrDefault(p => p.Id == problem.Id);
            if (entity == null)
                throw new ArgumentException("Problem with that Id does not exist.");

            entity.Title = problem.Title;
            entity.Description = problem.Description;
            entity.Status = problem.Status;
            entity.Priority = problem.Priority;
            entity.ProblemBoxId = problem.ProblemBoxId;

            _context.Problems.Update(entity);
            SaveChanges();
            var dto = _mapper.Map<ProblemDTO>(entity);
            return dto;
        }

        public void DeleteProblem(Guid id)
        {
            var problem = _context.Problems.FirstOrDefault(p => p.Id == id);
            if (problem == null)
                throw new ArgumentException("Problem with that Id does not exist.");
            _context.Problems.Remove(problem);
            SaveChanges();
        }
    }
}
