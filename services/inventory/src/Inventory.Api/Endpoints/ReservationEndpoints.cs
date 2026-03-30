using MediatR;
using Inventory.Application.DTOs;
using Inventory.Application.UseCases.CreateReservation;
using UserContext;

namespace Inventory.Api.Endpoints;

public static class ReservationEndpoints
{
    public static void MapReservationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/reservations")
            .WithName("Reservations");

        group.MapPost("/", CreateReservation)
            .WithName("CreateReservation")
            .WithSummary("Create a new seat reservation")
            .WithDescription("Reserves a seat with a 15-minute TTL. Returns 409 if seat is already reserved.");
    }

    private static async Task<IResult> CreateReservation(
        HttpContext context,
        CreateReservationRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (request.SeatId == Guid.Empty)
            return Results.BadRequest("SeatId must not be empty");

        var userId = context.Request.Headers[UserContextExtensions.UserIdHeader].FirstOrDefault();
        if (string.IsNullOrEmpty(userId))
            return Results.BadRequest($"{UserContextExtensions.UserIdHeader} header is required");

        try
        {
            var command = new CreateReservationCommand(request.SeatId, userId);
            var response = await mediator.Send(command, cancellationToken);
            
            return Results.Created($"/reservations/{response.ReservationId}", response);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
