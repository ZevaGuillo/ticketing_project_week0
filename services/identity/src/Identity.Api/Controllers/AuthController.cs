using Identity.Application.DTOs;
using Identity.Application.UseCases.CreateUser;
using Identity.Application.UseCases.IssueToken;
using Identity.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IssueTokenHandler _issueTokenHandler;
    private readonly CreateUserHandler _createUserHandler;

    public AuthController(IssueTokenHandler issueTokenHandler, CreateUserHandler createUserHandler)
    {
        _issueTokenHandler = issueTokenHandler;
        _createUserHandler = createUserHandler;
    }

    [HttpPost("token")]
    [ProducesResponseType(typeof(IssueTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IssueToken([FromBody] IssueTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Email is required"
            });

        if (string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Invalid credentials"
            });

        try
        {
            var result = await _issueTokenHandler.Handle(new IssueTokenCommand(request.Email, request.Password));

            return Ok(new IssueTokenResponse(
                Token: result.AccessToken,
                ExpiresAt: result.ExpiresAt,
                UserRole: result.UserRole.ToString(),
                UserEmail: result.UserEmail
            ));
        }
        catch (Exception)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Invalid credentials"
            });
        }
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
    {
        try
        {
            var role = Enum.TryParse<Role>(request.Role, true, out var parsedRole)
                ? parsedRole
                : Role.User;

            var userId = await _createUserHandler.Handle(new CreateUserCommand(request.Email, request.Password, role));

            return CreatedAtAction(nameof(Register), new CreateUserResponse(
                UserId: userId,
                Email: request.Email,
                Role: role.ToString()
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Conflict",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() =>
        Ok(new { status = "healthy", service = "identity" });
}
