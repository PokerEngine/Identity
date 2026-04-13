using Application.Command;
using Application.Event;
using Application.IntegrationEvent;
using Application.Repository;
using Application.Service.AuthTokenCodec;
using Application.Service.MessageSender;
using Application.Service.PasswordHasher;
using Application.Service.RefreshTokenHasher;
using Application.Storage;
using Application.UnitOfWork;
using Domain.Event;
using Infrastructure.Client.MongoDb;
using Infrastructure.Client.RabbitMq;
using Infrastructure.Command;
using Infrastructure.Event;
using Infrastructure.IntegrationEvent;
using Infrastructure.Repository;
using Infrastructure.Service.AuthTokenCodec;
using Infrastructure.Service.MessageSender;
using Infrastructure.Service.PasswordHasher;
using Infrastructure.Service.RefreshTokenHasher;
using Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Api;

public static class Bootstrapper
{
    public static WebApplicationBuilder PrepareApplicationBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddOpenApi();

        // Register clients
        builder.Services.Configure<MongoDbClientOptions>(
            builder.Configuration.GetSection(MongoDbClientOptions.SectionName)
        );
        builder.Services.AddSingleton<MongoDbClient>();
        builder.Services.Configure<RabbitMqClientOptions>(
            builder.Configuration.GetSection(RabbitMqClientOptions.SectionName)
        );
        builder.Services.AddSingleton<RabbitMqClient>();

        // Register repositories
        builder.Services.Configure<MongoDbRepositoryOptions>(
            builder.Configuration.GetSection(MongoDbRepositoryOptions.SectionName)
        );
        builder.Services.AddSingleton<IIdentityRepository, MongoDbIdentityRepository>();
        builder.Services.AddSingleton<ISessionRepository, MongoDbSessionRepository>();

        // Register storages
        builder.Services.Configure<MongoDbAccountStorageOptions>(
            builder.Configuration.GetSection(MongoDbAccountStorageOptions.SectionName)
        );
        builder.Services.AddSingleton<IAccountStorage, MongoDbAccountStorage>();
        builder.Services.Configure<MongoDbPasswordResetTokenStorageOptions>(
            builder.Configuration.GetSection(MongoDbPasswordResetTokenStorageOptions.SectionName)
        );
        builder.Services.AddSingleton<IPasswordResetTokenStorage, MongoDbPasswordResetTokenStorage>();

        // Register unit of work
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register services
        builder.Services.Configure<JwtAuthTokenCodecOptions>(
            builder.Configuration.GetSection(JwtAuthTokenCodecOptions.SectionName)
        );
        builder.Services.AddSingleton<IAuthTokenCodec, JwtAuthTokenCodec>();
        builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        builder.Services.AddSingleton<IRefreshTokenHasher, Pbkdf2RefreshTokenHasher>();
        builder.Services.AddSingleton<IMessageSender, ConsoleMessageSender>();

        // Register commands
        RegisterCommandHandler<RequestPasswordResetCommand, RequestPasswordResetHandler, RequestPasswordResetResponse>(builder.Services);
        RegisterCommandHandler<ConfirmPasswordResetCommand, ConfirmPasswordResetHandler, ConfirmPasswordResetResponse>(builder.Services);
        RegisterCommandHandler<CreateSessionCommand, CreateSessionHandler, CreateSessionResponse>(builder.Services);
        RegisterCommandHandler<RefreshSessionCommand, RefreshSessionHandler, RefreshSessionResponse>(builder.Services);
        RegisterCommandHandler<RevokeSessionCommand, RevokeSessionHandler, RevokeSessionResponse>(builder.Services);
        builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        // Register domain events
        RegisterEventHandler<PasswordInitializedEvent, PasswordInitializedEventHandler>(builder.Services);
        RegisterEventHandler<PasswordChangedEvent, PasswordChangedEventHandler>(builder.Services);
        builder.Services.AddScoped<IEventDispatcher, EventDispatcher>();

        // Register integration events
        RegisterIntegrationEventHandler<AccountRegisteredIntegrationEvent, AccountRegisteredHandler>(builder.Services);
        RegisterIntegrationEventHandler<EmailVerifiedIntegrationEvent, EmailVerifiedHandler>(builder.Services);
        builder.Services.AddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();

        builder.Services.Configure<RabbitMqIntegrationEventPublisherOptions>(
            builder.Configuration.GetSection(RabbitMqIntegrationEventPublisherOptions.SectionName)
        );
        builder.Services.Configure<RabbitMqIntegrationEventConsumerOptions>(
            builder.Configuration.GetSection(RabbitMqIntegrationEventConsumerOptions.SectionName)
        );
        builder.Services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
        builder.Services.AddHostedService(provider =>
            new RabbitMqIntegrationEventConsumer(
                scopeFactory: provider.GetRequiredService<IServiceScopeFactory>(),
                options: provider.GetRequiredService<IOptions<RabbitMqIntegrationEventConsumerOptions>>(),
                logger: provider.GetRequiredService<ILogger<RabbitMqIntegrationEventConsumer>>()
            )
        );

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        return builder;
    }

    private static void RegisterCommandHandler<TCommand, THandler, TResponse>(IServiceCollection services)
        where TCommand : ICommand
        where TResponse : ICommandResponse
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<ICommandHandler<TCommand, TResponse>>(provider => provider.GetRequiredService<THandler>());
    }

    private static void RegisterEventHandler<TEvent, THandler>(IServiceCollection services)
        where TEvent : IEvent
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IEventHandler<TEvent>>(provider => provider.GetRequiredService<THandler>());
    }

    private static void RegisterIntegrationEventHandler<TIntegrationEvent, THandler>(IServiceCollection services)
        where TIntegrationEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TIntegrationEvent>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IIntegrationEventHandler<TIntegrationEvent>>(provider => provider.GetRequiredService<THandler>());
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = CreateWebApplication(args);
        app.Run();
    }

    private static WebApplication CreateWebApplication(string[] args)
    {
        var builder = Bootstrapper.PrepareApplicationBuilder(args);
        return ConfigureApplication(builder);
    }

    private static WebApplication ConfigureApplication(WebApplicationBuilder builder)
    {
        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.MapOpenApi();
        app.MapControllers();

        return app;
    }
}
