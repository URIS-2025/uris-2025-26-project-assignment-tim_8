using AutoMapper;
using SuggestionBoxService.Context;
using SuggestionBoxService.Models;
using SuggestionBoxService.Models.DTOs;

namespace SuggestionBoxService.Data
{
    public class SuggestionBoxRepository : ISuggestionBoxRepository
    {
        private readonly SuggestionBoxContext _context;
        private readonly IMapper _mapper;

        public SuggestionBoxRepository(
            SuggestionBoxContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public IEnumerable<SuggestionBoxDTO> GetAll()
        {
            var boxes = _context.SuggestionBoxes.ToList();
            return _mapper.Map<IEnumerable<SuggestionBoxDTO>>(boxes);
        }

        public SuggestionBoxDTO GetById(Guid id)
        {
            var box = _context.SuggestionBoxes
                .FirstOrDefault(x => x.Id == id);

            if (box == null)
                return null;

            return _mapper.Map<SuggestionBoxDTO>(box);
        }

        public IEnumerable<SuggestionBoxDTO> GetByOrganizationId(Guid organizationId)
        {
            var boxes = _context.SuggestionBoxes
                .Where(x => x.OrganizationId == organizationId)
                .ToList();

            return _mapper.Map<IEnumerable<SuggestionBoxDTO>>(boxes);
        }

        public SuggestionBoxDTO Create(SuggestionBoxCreateDTO dto)
        {
            var entity = _mapper.Map<SuggestionBox>(dto);

            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;

            _context.SuggestionBoxes.Add(entity);
            SaveChanges();

            return _mapper.Map<SuggestionBoxDTO>(entity);
        }

        public SuggestionBoxDTO Update(SuggestionBoxUpdateDTO dto)
        {
            var entity = _context.SuggestionBoxes
                .FirstOrDefault(x => x.Id == dto.Id);

            if (entity == null)
                return null;

            _mapper.Map(dto, entity);

            SaveChanges();

            return _mapper.Map<SuggestionBoxDTO>(entity);
        }

        public void Delete(Guid id)
        {
            var entity = _context.SuggestionBoxes
                .FirstOrDefault(x => x.Id == id);

            if (entity == null)
                return;

            _context.SuggestionBoxes.Remove(entity);
            SaveChanges();
        }

        public void DeleteByOrganizationId(Guid organizationId)
        {
            var boxes = _context.SuggestionBoxes
                .Where(x => x.OrganizationId == organizationId)
                .ToList();

            _context.SuggestionBoxes.RemoveRange(boxes);
            SaveChanges();
        }
    }
}