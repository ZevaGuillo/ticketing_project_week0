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
        var userId = httpContext.User.FindFirstValue("sub");
        var userRole = httpContext.User.FindFirstValue("role");
        var userEmail = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Email);

        if (!string.IsNullOrEmpty(userId))
            context.ProxyRequest.Headers.Add(GatewayHeaders.UserId, userId);

        if (!string.IsNullOrEmpty(userRole))
            context.ProxyRequest.Headers.Add(GatewayHeaders.UserRole, userRole);

        if (!string.IsNullOrEmpty(userEmail))
            context.ProxyRequest.Headers.Add(GatewayHeaders.UserEmail, userEmail);

        return ValueTask.CompletedTask;
    }
}
