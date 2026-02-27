using AutoMapper;
using ProblemBoxService.Context;
using ProblemBoxService.Models;
using ProblemBoxService.Models.DTOs;

namespace ProblemBoxService.Data
{
    public class ProblemBoxRepository : IProblemBoxRepository
    {
        private readonly ProblemBoxContext _context;
        private readonly IMapper _mapper;

        public ProblemBoxRepository(ProblemBoxContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<ProblemBoxDTO> GetProblemBoxByOrganizationId(Guid organizationId)
        {
            var problemBoxes = _context.ProblemBoxes.Where(pb => pb.OrganizationId == organizationId).ToList();
            var problemBoxResult = new List<ProblemBoxDTO>();
            foreach (var problemBox in problemBoxes)
            {
                var dto = _mapper.Map<ProblemBoxDTO>(problemBox);
                problemBoxResult.Add(dto);
            }
            return problemBoxResult;
        }

        public ProblemBoxDTO GetProblemBoxById(Guid id)
        {
            var problemBox = _context.ProblemBoxes.FirstOrDefault(pb => pb.Id == id);
            if (problemBox == null)
                throw new ArgumentException("ProblemBox with that Id does not exist.");
            var dto = _mapper.Map<ProblemBoxDTO>(problemBox);
            return dto;
        }

        public ProblemBoxDTO GetProblemBoxByAccessLinkId(Guid boxAccessLinkId)
        {
            var problemBox = _context.ProblemBoxes.FirstOrDefault(pb => pb.BoxAccessLinkId == boxAccessLinkId);
            if (problemBox == null)
                throw new ArgumentException("ProblemBox with that AccessLinkId does not exist.");
            var dto = _mapper.Map<ProblemBoxDTO>(problemBox);
            return dto;
        }

        public ProblemBoxCreatedDTO CreateProblemBox(ProblemBoxCreationDTO problemBox)
        {
            if (problemBox.OrganizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId must be provided.");
            if (string.IsNullOrWhiteSpace(problemBox.Name))
                throw new ArgumentException("Name must be provided.");
            if (string.IsNullOrWhiteSpace(problemBox.Description))
                throw new ArgumentException("Description must be provided.");
            if (string.IsNullOrWhiteSpace(problemBox.Password))
                throw new ArgumentException("Password must be provided.");

            var entity = _mapper.Map<ProblemBox>(problemBox);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            _context.ProblemBoxes.Add(entity);
            SaveChanges();
            return _mapper.Map<ProblemBoxCreatedDTO>(entity);
        }

        public ProblemBoxDTO UpdateProblemBox(ProblemBoxUpdateDTO problemBox)
        {
            var entity = _context.ProblemBoxes.FirstOrDefault(pb => pb.Id == problemBox.Id);
            if (entity == null)
                throw new ArgumentException("ProblemBox with that Id does not exist.");

            entity.Name = problemBox.Name;
            entity.Description = problemBox.Description;
            entity.IsDarkTheme = problemBox.IsDarkTheme;
            entity.Password = problemBox.Password;
            entity.Status = problemBox.Status;

            _context.ProblemBoxes.Update(entity);
            SaveChanges();
            var dto = _mapper.Map<ProblemBoxDTO>(entity);
            return dto;
        }

        public void DeleteProblemBox(Guid id)
        {
            var problemBox = _context.ProblemBoxes.FirstOrDefault(pb => pb.Id == id);
            if (problemBox == null)
                throw new ArgumentException("ProblemBox with that Id does not exist.");
            _context.ProblemBoxes.Remove(problemBox);
            SaveChanges();
        }
    }
}
