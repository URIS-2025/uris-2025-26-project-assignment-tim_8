using AutoMapper;
using ProblemService.Context;
using ProblemService.Models.DTOs;
using ProblemService.Models.Problem;

namespace ProblemService.Data
{
    public class ProblemCategoryRepository : IProblemCategoryRepository
    {
        private readonly ProblemContext _context;
        private readonly IMapper _mapper;

        public ProblemCategoryRepository(ProblemContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<ProblemCategoryDTO> GetAllProblemCategories()
        {
            var categories = _context.ProblemCategories.ToList();
            var categoryResult = new List<ProblemCategoryDTO>();
            foreach (var category in categories)
            {
                var dto = _mapper.Map<ProblemCategoryDTO>(category);
                categoryResult.Add(dto);
            }
            return categoryResult;
        }

        public ProblemCategoryDTO GetProblemCategoryById(Guid id)
        {
            var category = _context.ProblemCategories.FirstOrDefault(c => c.Id == id);
            if (category == null)
                throw new ArgumentException("ProblemCategory with that Id does not exist.");
            var dto = _mapper.Map<ProblemCategoryDTO>(category);
            return dto;
        }

        public ProblemCategoryCreatedDTO CreateProblemCategory(ProblemCategoryCreationDTO category)
        {
            if (string.IsNullOrWhiteSpace(category.Title))
                throw new ArgumentException("Title must be provided.");
            if (string.IsNullOrWhiteSpace(category.Description))
                throw new ArgumentException("Description must be provided.");

            var entity = _mapper.Map<ProblemCategory>(category);
            entity.Id = Guid.NewGuid();
            _context.ProblemCategories.Add(entity);
            SaveChanges();
            return _mapper.Map<ProblemCategoryCreatedDTO>(entity);
        }

        public ProblemCategoryDTO UpdateProblemCategory(ProblemCategoryUpdateDTO category)
        {
            var entity = _context.ProblemCategories.FirstOrDefault(c => c.Id == category.Id);
            if (entity == null)
                throw new ArgumentException("ProblemCategory with that Id does not exist.");

            if (string.IsNullOrWhiteSpace(category.Title))
                throw new ArgumentException("Title must be provided.");
            if (string.IsNullOrWhiteSpace(category.Description))
                throw new ArgumentException("Description must be provided.");

            entity.Title = category.Title;
            entity.Description = category.Description;
            _context.ProblemCategories.Update(entity);
            SaveChanges();
            var dto = _mapper.Map<ProblemCategoryDTO>(entity);
            return dto;
        }

        public void DeleteProblemCategory(Guid id)
        {
            var category = _context.ProblemCategories.FirstOrDefault(c => c.Id == id);
            if (category == null)
                throw new ArgumentException("ProblemCategory with that Id does not exist.");
            _context.ProblemCategories.Remove(category);
            SaveChanges();
        }
    }
}
