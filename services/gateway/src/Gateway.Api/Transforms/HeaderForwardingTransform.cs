using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Gateway.Api.Constants;

namespace Gateway.Api.Transforms;

public class HeaderForwardingTransform : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        var httpContext = context.HttpContext;
        
        var userId = httpContext.Request.Headers["X-User-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userId))
        {
            context.ProxyRequest.Headers.Add(GatewayHeaders.UserId, userId);
            Console.WriteLine($"[HeaderForwarding] Added X-User-Id: {userId}");
        }

        var userRole = httpContext.Request.Headers["X-User-Role"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userRole))
        {
            context.ProxyRequest.Headers.Add(GatewayHeaders.UserRole, userRole);
        }

        return ValueTask.CompletedTask;
    }
}