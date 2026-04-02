namespace Gateway.Api.Constants;

public static class GatewayHeaders
{
    public const string UserId = "X-User-Id";
    public const string UserRole = "X-User-Role";
    public const string UserEmail = "X-User-Email";
}

public static class GatewayClaims
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string Email = "email";
}
