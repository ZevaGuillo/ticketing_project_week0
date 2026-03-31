using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Gateway.Api.Constants;

namespace Gateway.Api.Transforms;

public class ClaimsForwardingTransform : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        var httpContext = context.HttpContext;
        var userId = httpContext.User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        var userRole = httpContext.User.FindFirstValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

        if (!string.IsNullOrEmpty(userId))
        {
            context.ProxyRequest.Headers.Add(GatewayHeaders.UserId, userId);
        }

        if (!string.IsNullOrEmpty(userRole))
        {
            context.ProxyRequest.Headers.Add(GatewayHeaders.UserRole, userRole);
        }

        return ValueTask.CompletedTask;
    }
}
