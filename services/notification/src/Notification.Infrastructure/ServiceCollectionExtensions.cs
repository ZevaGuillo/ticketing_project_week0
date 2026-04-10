using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notification.Application.Ports;
using Notification.Infrastructure.Email;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Messaging.Strategies;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Controllers
        services.AddControllers();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Notification.Application.UseCases.SendTicketNotification.SendTicketNotificationCommandHandler).Assembly));

        // Add Database
        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"),
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_notification"));
        });

        // IDbInitializer removed - migrations handled externally
        services.AddScoped<IEmailNotificationRepository, EmailNotificationRepository>();

        // Add Email Service
        services.Configure<SmtpEmailOptions>(
            configuration.GetSection(SmtpEmailOptions.Section));
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IQrCodeService, QrCodeService>();

        // Notification event strategies (Strategy pattern)
        services.AddScoped<INotificationEventStrategy, TicketIssuedStrategy>();
        services.AddScoped<INotificationEventStrategy, WaitlistOpportunityStrategy>();

        // Unified Kafka consumer — dispatches to strategies by topic
        services.AddHttpClient("identity");
        services.AddHttpClient("gateway");
        services.AddHostedService<NotificationEventConsumer>();

        return services;
    }

    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        // Apply migrations automatically on startup
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                dbContext.Database.Migrate();
                Console.WriteLine("✅ Notification migrations applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning: Could not apply migrations: {ex.Message}");
            }
        }

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.MapControllers();

        return app;
    }
}
