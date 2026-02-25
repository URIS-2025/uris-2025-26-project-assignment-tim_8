using AnonymousDomain.Models.Organization;
using AutoMapper;
using OrganizationService.Context;
using OrganizationService.Models.DTOs;

namespace OrganizationService.Data
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly OrganizationContext _context;
        private readonly IMapper _mapper;

        public OrganizationRepository(OrganizationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }
        public OrganizationCreatedDTO CreateOrganization(OrganizationCreationDTO organization)
        {
             var entity = _mapper.Map<Organization>(organization)!;
             entity.Id = Guid.NewGuid();
             entity.CreatedAt = DateTime.UtcNow;

             _context.Organizations.Add(entity);
             SaveChanges();

             return _mapper.Map<OrganizationCreatedDTO>(entity);

          
        }

        public void DeleteOrganization(Guid id)
        {
            var organization = _context.Organizations.Find(id);
            if (organization == null)
                throw new KeyNotFoundException($"Organization with id {id} not found.");

            _context.Organizations.Remove(organization);
            SaveChanges();
        }

        public IEnumerable<OrganizationDTO> GetAllOrganizations()
        {
            var organizations = _context.Organizations.ToList();
            var organizationResult = new List<OrganizationDTO>();

            foreach (var organization in organizations)
            {
                var dto = _mapper.Map<OrganizationDTO>(organization);
                organizationResult.Add(dto);
            }

            return organizationResult;
        }

        public OrganizationDTO GetOrganizationById(Guid Id)
        {
            var organization = _context.Organizations.Find(Id);
            if (organization == null)
                throw new KeyNotFoundException($"Organization with id {Id} not found.");

            return _mapper.Map<OrganizationDTO>(organization);
        }

        public OrganizationCreatedDTO UpdateOrganization(OrganizationDTO organization)
        {
            var existOrg = _context.Organizations.Find(organization.Id);
            if (existOrg == null)
                throw new KeyNotFoundException($"Organization with id {organization.Id} not found.");

            return _mapper.Map<OrganizationCreatedDTO>(existOrg);
        }
    }
}
