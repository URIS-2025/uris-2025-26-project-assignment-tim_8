using AnonymousDomain.Models.Suggestion;
using AutoMapper;
using SuggestionService.Data;
using SuggestionService.Models.DTOs;

namespace AnonymousRepository.Repositories
{
    public class SuggestionCategoryRepository : ISuggestionCategoryRepository
    {
        private readonly SuggestionContext _context;
        private readonly IMapper _mapper;

        public SuggestionCategoryRepository(SuggestionContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<SuggestionCategoryDTO> GetAll()
        {
            var categories = _context.SuggestionCategories.ToList();
            return _mapper.Map<IEnumerable<SuggestionCategoryDTO>>(categories);
        }

        public SuggestionCategoryDTO GetById(Guid id)
        {
            var category = _context.SuggestionCategories.FirstOrDefault(c => c.Id == id);
            return _mapper.Map<SuggestionCategoryDTO>(category);
        }

        public SuggestionCategoryDTO Create(SuggestionCategoryDTO categoryDto)
        {
            var category = _mapper.Map<SuggestionCategory>(categoryDto);
            _context.SuggestionCategories.Add(category);
            SaveChanges();
            return _mapper.Map<SuggestionCategoryDTO>(category);
        }

        public SuggestionCategoryDTO Update(SuggestionCategoryDTO categoryDto)
        {
            var category = _context.SuggestionCategories.FirstOrDefault(c => c.Id == categoryDto.Id);
            _mapper.Map(categoryDto, category);
            SaveChanges();
            return _mapper.Map<SuggestionCategoryDTO>(category);
        }

        public void Delete(Guid id)
        {
            var category = _context.SuggestionCategories.FirstOrDefault(c => c.Id == id);
            if (category != null)
            {
                _context.SuggestionCategories.Remove(category);
                SaveChanges();
            }
        }
    }
}