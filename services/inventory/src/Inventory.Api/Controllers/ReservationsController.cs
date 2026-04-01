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

        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(userId))
            return BadRequest("X-User-Id header is required");

        var command = new CreateReservationCommand(request.SeatId, userId);
        var response = await _mediator.Send(command, cancellationToken);

        return Created($"/reservations/{response.ReservationId}", response);
    }
}