using MediatR;
using Inventory.Application.UseCases.JoinWaitlist;
using Inventory.Application.UseCases.GetWaitlistStatus;
using Inventory.Application.UseCases.CreateReservation;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WaitlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public WaitlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("/api/waitlist/join")]
    [ProducesResponseType(typeof(JoinWaitlistResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request, CancellationToken cancellationToken)
    {
        var userIdHeader = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(userIdHeader) || !Guid.TryParse(userIdHeader, out var userId))
            return BadRequest("X-User-Id header is required and must be a valid GUID");

        if (request.EventId == Guid.Empty)
            return BadRequest("EventId must not be empty");

        if (string.IsNullOrWhiteSpace(request.Section))
            return BadRequest("Section must not be empty");

        try
        {
            var command = new JoinWaitlistCommand(userId, request.EventId, request.Section);
            var response = await _mediator.Send(command, cancellationToken);

            return Created($"/api/waitlist/status?eventId={request.EventId}&section={request.Section}", response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already in waitlist"))
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("/api/waitlist/status")]
    [ProducesResponseType(typeof(GetWaitlistStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWaitlistStatus(
        [FromQuery] Guid eventId,
        [FromQuery] string section,
        CancellationToken cancellationToken)
    {
        var userIdHeader = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(userIdHeader) || !Guid.TryParse(userIdHeader, out var userId))
            return BadRequest("X-User-Id header is required and must be a valid GUID");

        if (eventId == Guid.Empty)
            return BadRequest("EventId must not be empty");

        if (string.IsNullOrWhiteSpace(section))
            return BadRequest("Section must not be empty");

        var query = new GetWaitlistStatusQuery(userId, eventId, section);
        var response = await _mediator.Send(query, cancellationToken);

        if (response == null)
            return NotFound(new { error = "User is not in waitlist for this event and section" });

        return Ok(response);
    }

    [HttpGet("/api/waitlist/opportunity/{token}")]
    [ProducesResponseType(typeof(ValidateOpportunityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateOpportunity(
        [FromRoute] string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Token must not be empty");

        try
        {
            var command = new ValidateOpportunityCommand(token);
            var response = await _mediator.Send(command, cancellationToken);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class JoinWaitlistRequest
{
    public Guid EventId { get; set; }
    public string Section { get; set; } = string.Empty;
}