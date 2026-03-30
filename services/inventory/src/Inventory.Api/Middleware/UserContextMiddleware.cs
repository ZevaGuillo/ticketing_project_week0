namespace Inventory.Api.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var userRole = context.Request.Headers["X-User-Role"].FirstOrDefault();

        if (!string.IsNullOrEmpty(userId))
        {
            userContext.SetUserId(userId);
        }

        if (!string.IsNullOrEmpty(userRole))
        {
            userContext.SetUserRole(userRole);
        }

        await _next(context);
    }
}

public interface IUserContext
{
    string? UserId { get; }
    string? UserRole { get; }
    void SetUserId(string userId);
    void SetUserRole(string userRole);
}

public class UserContext : IUserContext
{
    private string? _userId;
    private string? _userRole;

    public string? UserId => _userId;
    public string? UserRole => _userRole;

    public void SetUserId(string userId) => _userId = userId;
    public void SetUserRole(string userRole) => _userRole = userRole;
}
