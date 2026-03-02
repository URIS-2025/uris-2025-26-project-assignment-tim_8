using LoggerService.Data;
using LoggerService.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoggerService.Controllers
{

    [ApiController]
    [Route("api/[controller]")]

    public class LoggerController : ControllerBase
    {
        private readonly ILoggerRepository _repo;

        public LoggerController(ILoggerRepository repo)
        {
            _repo = repo;
        }

        // GET: api/logger?take=100
        [HttpGet]
        [HttpHead]
        public ActionResult<IEnumerable<LogDTO>> GetAll([FromQuery] int take = 100)
        {
            var logs = _repo.GetAll(take);
            if (logs == null || !logs.Any())
                return NoContent();
            return Ok(logs);
        }

        [HttpGet("{id:guid}")]
        public ActionResult<LogDTO> GetById(Guid id)
        {
            var log = _repo.GetById(id);
            if (log == null) return NotFound();
            return Ok(log);
        }

        [HttpGet("search")]
        public ActionResult<IEnumerable<LogDTO>> Search(
            [FromQuery] string? userId,
            [FromQuery] string? action,
            [FromQuery] string? entityName,
            [FromQuery] string? serviceName,
            [FromQuery] string? httpMethod,
            [FromQuery] bool? isSuccess,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] int take = 100)
        {
            var result = _repo.Search(userId, action, entityName, serviceName, httpMethod, isSuccess, fromUtc, toUtc, take);
            if (result == null || !result.Any())
                return NoContent();
            return Ok(result);
        }

        [HttpPost]
        [AllowAnonymous] //ova post metoda moze da se kreira iz bilo kog servisa i zato je allowAnonymous
        public ActionResult<LogDTO> Create([FromBody] LogCreationDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var created = _repo.Create(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public IActionResult Delete(Guid id)
        {
            var existing = _repo.GetById(id);
            if (existing == null) return NotFound();
            _repo.Delete(id);
            return NoContent();
        }

        [HttpOptions]
        [AllowAnonymous]
        public IActionResult GetOptions()
        {
            Response.Headers.Add("Allow", "GET, HEAD, POST, DELETE, OPTIONS");
            return Ok();
        }
    }
}
