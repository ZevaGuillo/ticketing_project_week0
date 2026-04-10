using Ordering.Infrastructure;
using UserContext;
using IUserContext = UserContext.IUserContext;
using UserContextImpl = UserContext.UserContext;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IUserContext, UserContextImpl>();

var app = builder.Build();

app.UseInfrastructure();

app.UseCors("FrontendPolicy");

app.Use(async (context, next) =>
{
    var userContext = context.RequestServices.GetRequiredService<IUserContext>();
    var userId = context.Request.Headers[UserContextExtensions.UserIdHeader].FirstOrDefault();
    var userRole = context.Request.Headers[UserContextExtensions.UserRoleHeader].FirstOrDefault();

    if (!string.IsNullOrEmpty(userId))
    {
        userContext.SetUserId(userId);
    }

    if (!string.IsNullOrEmpty(userRole))
    {
        userContext.SetUserRole(userRole);
    }

    await next();
});

Console.WriteLine("Ordering API is starting...");
Console.WriteLine("Listening on http://0.0.0.0:5003");
Console.Out.Flush();

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application failed: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}

public partial class Program { }
