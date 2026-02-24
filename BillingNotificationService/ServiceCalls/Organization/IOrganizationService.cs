using BillingNotificationService.Models.DTOs.Organization;

namespace BillingNotificationService.ServiceCalls.Organization

{
    public interface IOrganizationService
    {
        OrganizationDTO getOrganizationById(Guid id);
    }
}
