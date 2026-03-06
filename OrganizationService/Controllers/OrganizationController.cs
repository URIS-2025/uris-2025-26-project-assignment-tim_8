using AnonymousDomain.Models.Organization;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Clients;
using OrganizationService.Data;
using OrganizationService.Models.DTOs;
using System.Text.Json;

namespace AnonymousAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationController : Controller
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly LoggerServiceClient _loggerClient;

        public OrganizationController(IOrganizationRepository organizationRepository, LoggerServiceClient loggerClient)
        {
            _organizationRepository = organizationRepository;
            _loggerClient = loggerClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<OrganizationDTO>> GetAllOrganizations()
        {
            var result = _organizationRepository.GetAllOrganizations();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public ActionResult<OrganizationDTO> GetOrganizationById(Guid id)
        {
            var result = _organizationRepository.GetOrganizationById(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<OrganizationCreatedDTO>> CreateOrganization([FromBody] OrganizationCreationDTO organization)
        {
            try
            {
                var result = _organizationRepository.CreateOrganization(organization);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_ORGANIZATION",
                    EntityName = "Organization",
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Created("", result);
            }
            catch (Exception ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "CREATE_ORGANIZATION",
                    EntityName = "Organization",
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "POST"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<OrganizationCreatedDTO>> UpdateOrganization([FromBody] OrganizationDTO organization)
        {
            try
            {
                var oldOrg = _organizationRepository.GetOrganizationById(organization.Id);
                var result = _organizationRepository.UpdateOrganization(organization);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_ORGANIZATION",
                    EntityName = "Organization",
                    OldValues = JsonSerializer.Serialize(oldOrg),
                    NewValues = JsonSerializer.Serialize(result),
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "UPDATE_ORGANIZATION",
                    EntityName = "Organization",
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "PUT"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrganization(Guid id)
        {
            try
            {
                _organizationRepository.DeleteOrganization(id);

                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_ORGANIZATION",
                    EntityName = "Organization",
                    OldValues = id.ToString(),
                    IsSuccess = true,
                    ServiceName = "OrganizationService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                await _loggerClient.TryLogAsync(new LogCreationDTO
                {
                    UserId = User.Identity?.Name,
                    Action = "DELETE_ORGANIZATION",
                    EntityName = "Organization",
                    OldValues = id.ToString(),
                    IsSuccess = false,
                    ServiceName = "OrganizationService",
                    HttpMethod = "DELETE"
                }, Request.Headers["Authorization"], HttpContext.RequestAborted);

                return NotFound(new { error = ex.Message });
            }
        }
    }
}