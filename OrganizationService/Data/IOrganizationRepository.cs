using AnonymousDomain.Models.Organization;
using OrganizationService.Models.DTOs;

namespace OrganizationService.Data
{
    public interface IOrganizationRepository
    {
        IEnumerable<OrganizationDTO> GetAllOrganizations();
        OrganizationDTO GetOrganizationById(Guid Id);
        OrganizationCreatedDTO CreateOrganization(OrganizationCreationDTO organization);
        OrganizationCreatedDTO UpdateOrganization(OrganizationDTO organization);
        void DeleteOrganization(Guid id);
    }
}
