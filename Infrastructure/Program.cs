using Application.Command;
using Application.Event;
using Application.IntegrationEvent;
using Application.Repository;
using Application.Service.PasswordEncryptor;
using Application.Storage;
using Application.UnitOfWork;
using Domain.Event;
using Infrastructure.Client.MongoDb;
using Infrastructure.Client.RabbitMq;
using Infrastructure.Command;
using Infrastructure.Event;
using Infrastructure.IntegrationEvent;
using Infrastructure.Repository;
using Infrastructure.Service.PasswordEncryptor;
using Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Infrastructure;

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

        // Register repository
        builder.Services.Configure<MongoDbRepositoryOptions>(
            builder.Configuration.GetSection(MongoDbRepositoryOptions.SectionName)
        );
        builder.Services.AddSingleton<IRepository, MongoDbRepository>();

        // Register storage
        builder.Services.Configure<MongoDbStorageOptions>(
            builder.Configuration.GetSection(MongoDbStorageOptions.SectionName)
        );
        builder.Services.AddSingleton<IAccountStorage, MongoDbAccountStorage>();

        // Register unit of work
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register password encryptor
        builder.Services.AddSingleton<IPasswordEncryptor, Pbkdf2PasswordEncryptor>();

        // Register commands
        RegisterCommandHandler<InitializePasswordCommand, InitializePasswordHandler, InitializePasswordResponse>(builder.Services);
        RegisterCommandHandler<ChangePasswordCommand, ChangePasswordHandler, ChangePasswordResponse>(builder.Services);
        builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        // Register domain events
        RegisterEventHandler<PasswordInitializedEvent, PasswordInitializedEventHandler>(builder.Services);
        RegisterEventHandler<PasswordChangedEvent, PasswordChangedEventHandler>(builder.Services);
        builder.Services.AddScoped<IEventDispatcher, EventDispatcher>();

        // Register integration events
        RegisterIntegrationEventHandler<AccountRegisteredIntegrationEvent, AccountRegisteredHandler>(builder.Services);
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

    // Public method for creating the WebApplication - can be called by tests
    // This allows WebApplicationFactory to work properly with the minimal hosting model
    private static WebApplication CreateWebApplication(string[] args)
    {
        var builder = Bootstrapper.PrepareApplicationBuilder(args);
        return ConfigureApplication(builder);
    }

    // Configure the application pipeline
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
