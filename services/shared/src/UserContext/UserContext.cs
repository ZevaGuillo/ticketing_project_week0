namespace UserContext;

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

public static class UserContextExtensions
{
    public const string UserIdHeader = "X-User-Id";
    public const string UserRoleHeader = "X-User-Role";
}
