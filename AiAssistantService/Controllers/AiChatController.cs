using AiAssistantService.Agent;
using AiAssistantService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AiAssistantService.Controllers;

/// <summary>
/// Demo agent endpoint. Requires the caller's (OrganizationService) JWT — that token is forwarded
/// to the gateway as the on-behalf-of principal. Rate-limited per user. The controller is thin: all
/// orchestration lives in <see cref="IAiChatAgent"/>; all authorization/audit lives in the gateway.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AiChatController : ControllerBase
{
    private readonly IAiChatAgent _agent;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(IAiChatAgent agent, ILogger<AiChatController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("aichat")]
    public async Task<ActionResult<AiChatResponseDTO>> Chat([FromBody] AiChatRequestDTO request)
    {
        try
        {
            var oboBearer = Request.Headers["Authorization"].ToString();
            var result = await _agent.RunAsync(request, oboBearer, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected / request aborted — not an application error; don't shape it as one.
            throw;
        }
        catch (Exception ex)
        {
            // Fail-closed: never fabricate an answer or silently execute. Log the detail server-side
            // (Claude/gateway/transport faults, config errors) and return a fixed, non-revealing
            // message — raw exception text can carry internal topology, so it is NOT surfaced (R15).
            _logger.LogError(ex, "AiChat agent run failed; failing closed");
            return BadRequest(new { error = "Asistent trenutno ne moze da obradi zahtev." });
        }
    }
}
