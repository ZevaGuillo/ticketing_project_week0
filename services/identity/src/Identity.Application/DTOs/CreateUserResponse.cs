namespace Identity.Application.DTOs;

public record CreateUserResponse(
    Guid UserId,
    string Email,
    string Role
);
