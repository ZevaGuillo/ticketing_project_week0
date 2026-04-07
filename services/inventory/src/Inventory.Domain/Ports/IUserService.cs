namespace Inventory.Domain.Ports;

public interface IUserService
{
    Task<UserInfo?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public string FullName { get; set; } = string.Empty;
}
