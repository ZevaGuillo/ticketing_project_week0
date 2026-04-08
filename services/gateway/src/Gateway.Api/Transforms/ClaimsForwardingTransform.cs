using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Gateway.Api.Constants;

namespace Gateway.Api.Transforms;

public class ClaimsForwardingTransform : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        var httpContext = context.HttpContext;

        // Prefer JWT claims (authenticated routes) over client-supplied headers
        var userId = httpContext.User.FindFirstValue(GatewayClaims.Subject)
            ?? httpContext.Request.Headers[GatewayHeaders.UserId].FirstOrDefault();

        context.ProxyRequest.Headers.Remove(GatewayHeaders.UserId);
        if (!string.IsNullOrEmpty(userId))
            context.ProxyRequest.Headers.TryAddWithoutValidation(GatewayHeaders.UserId, userId);

        var userRole = httpContext.User.FindFirstValue(GatewayClaims.Role)
            ?? httpContext.Request.Headers[GatewayHeaders.UserRole].FirstOrDefault();

        context.ProxyRequest.Headers.Remove(GatewayHeaders.UserRole);
        if (!string.IsNullOrEmpty(userRole))
            context.ProxyRequest.Headers.TryAddWithoutValidation(GatewayHeaders.UserRole, userRole);

        var userEmail = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Email);
        context.ProxyRequest.Headers.Remove(GatewayHeaders.UserEmail);
        if (!string.IsNullOrEmpty(userEmail))
            context.ProxyRequest.Headers.TryAddWithoutValidation(GatewayHeaders.UserEmail, userEmail);

        return ValueTask.CompletedTask;
    }
}
