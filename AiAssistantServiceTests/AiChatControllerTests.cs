using AiAssistantService.Agent;
using AiAssistantService.Controllers;
using AiAssistantService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AiAssistantServiceTests;

public class AiChatControllerTests
{
    private static AiChatController BuildController(IAiChatAgent agent) =>
        new(agent, NullLogger<AiChatController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

    [Fact] // T6 — fail-closed: any agent failure becomes a shaped BadRequest with a fixed, non-revealing message
    public async Task Agent_failure_returns_shaped_bad_request_without_leaking_internal_detail()
    {
        var agent = new Mock<IAiChatAgent>();
        agent.Setup(a => a.RunAsync(It.IsAny<AiChatRequestDTO>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection refused (mcp-gateway:8080)"));

        var controller = BuildController(agent.Object);

        var action = await controller.Chat(new AiChatRequestDTO { Message = "zdravo" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var error = (string)badRequest.Value!.GetType().GetProperty("error")!.GetValue(badRequest.Value)!;
        Assert.DoesNotContain("mcp-gateway", error); // internal topology never leaked
        Assert.Equal("Asistent trenutno ne moze da obradi zahtev.", error);
    }

    [Fact] // A client abort (OperationCanceledException) is not shaped as an application error
    public async Task Client_cancellation_is_not_turned_into_a_bad_request()
    {
        var agent = new Mock<IAiChatAgent>();
        agent.Setup(a => a.RunAsync(It.IsAny<AiChatRequestDTO>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var controller = BuildController(agent.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => controller.Chat(new AiChatRequestDTO { Message = "x" }));
    }

    [Fact] // Happy path: the controller returns the agent's response as 200 OK
    public async Task Successful_run_returns_ok_with_the_agent_response()
    {
        var expected = new AiChatResponseDTO { Status = AiChatStatus.Completed, AssistantText = "ok", CorrelationId = "c1" };
        var agent = new Mock<IAiChatAgent>();
        agent.Setup(a => a.RunAsync(It.IsAny<AiChatRequestDTO>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = BuildController(agent.Object);

        var action = await controller.Chat(new AiChatRequestDTO { Message = "zdravo" });

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
    }
}
