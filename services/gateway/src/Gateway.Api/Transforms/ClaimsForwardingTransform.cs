using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Gateway.Api.Constants;

namespace Gateway.Api.Transforms;

public class ClaimsForwardingTransform
{
    public static ValueTask ApplyAsync(HttpContext context, RequestTransformContext transformContext)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = context.User.FindFirstValue(ClaimTypes.Role);

        if (!string.IsNullOrEmpty(userId))
        {
            transformContext.ProxyRequest.Headers.Add(GatewayHeaders.UserId, userId);
        }

        if (!string.IsNullOrEmpty(userRole))
        {
            transformContext.ProxyRequest.Headers.Add(GatewayHeaders.UserRole, userRole);
        }

        return ValueTask.CompletedTask;
    }
}
