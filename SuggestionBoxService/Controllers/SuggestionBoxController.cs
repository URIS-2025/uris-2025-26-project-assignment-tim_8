using Microsoft.AspNetCore.Mvc;
using SuggestionBoxService.Data;
using SuggestionBoxService.Models.DTOs;

namespace AnonymousAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuggestionBoxController : ControllerBase
    {
        private readonly ISuggestionBoxRepository _repository;

        public SuggestionBoxController(ISuggestionBoxRepository repository)
        {
            _repository = repository;
        }

        // GET: api/suggestionbox/{id}
        [HttpGet("{id}")]
        public ActionResult<SuggestionBoxDTO> GetById(Guid id)
        {
            var result = _repository.GetById(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // GET: api/suggestionbox/organization/{organizationId}
        [HttpGet("organization/{organizationId}")]
        public ActionResult<IEnumerable<SuggestionBoxDTO>> GetByOrganizationId(Guid organizationId)
        {
            var result = _repository.GetByOrganizationId(organizationId);
            return Ok(result);
        }

        // POST: api/suggestionbox
        [HttpPost]
        public ActionResult<SuggestionBoxDTO> Create(
            [FromBody] SuggestionBoxCreateDTO dto)
        {
            var result = _repository.Create(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }

        // PUT: api/suggestionbox
        [HttpPut]
        public ActionResult<SuggestionBoxDTO> Update(
            [FromBody] SuggestionBoxUpdateDTO dto)
        {
            var result = _repository.Update(dto);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // DELETE: api/suggestionbox/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            _repository.Delete(id);
            return NoContent();
        }
        // DELETE: api/suggestionbox/organization/{organizationId}
        [HttpDelete("organization/{organizationId}")]
        public IActionResult DeleteByOrganizationId(Guid organizationId)
        {
            _repository.DeleteByOrganizationId(organizationId);
            return NoContent();
        }
    }
}