using MediatR;
using Inventory.Application.DTOs;
using Inventory.Application.UseCases.CreateReservation;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request, CancellationToken cancellationToken)
    {
        if (request.SeatId == Guid.Empty)
            return BadRequest("SeatId must not be empty");

        var userIdHeader = Request.Headers["X-User-Id"];
        var userId = userIdHeader.Count > 0 ? userIdHeader[0] : null;
        
        if (string.IsNullOrEmpty(userId))
            return BadRequest("X-User-Id header is required");

        var customerId = !string.IsNullOrEmpty(request.CustomerId) ? request.CustomerId : userId;
        
        var command = new CreateReservationCommand(request.SeatId, customerId);
        var response = await _mediator.Send(command, cancellationToken);

        return Created($"/reservations/{response.ReservationId}", response);
    }
}