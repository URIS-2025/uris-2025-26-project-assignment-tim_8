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
            throw new NotImplementedException();
        }

        public void DeleteOrganization(Guid id)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public OrganizationCreatedDTO UpdateOrganization(OrganizationDTO organization)
        {
            throw new NotImplementedException();
        }
    }
}
