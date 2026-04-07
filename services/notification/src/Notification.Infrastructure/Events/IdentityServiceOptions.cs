namespace Notification.Infrastructure.Events;

public class IdentityServiceOptions
{
    public const string Section = "IdentityService";
    public string BaseUrl { get; set; } = "http://identity:5001";
}
