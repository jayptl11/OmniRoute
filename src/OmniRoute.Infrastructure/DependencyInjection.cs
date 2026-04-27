using System.Text;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.BackgroundServices;
using OmniRoute.Infrastructure.Persistence;
using OmniRoute.Infrastructure.Repositories;
using OmniRoute.Infrastructure.Services;
using OmniRoute.Infrastructure.Settings;using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace OmniRoute.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("MyCnn")));
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("MyCnn")), ServiceLifetime.Scoped);

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IOTPService, OTPService>();
        services.AddScoped<IOTPCacheService, OTPCacheService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        // Lead management repositories
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IRoutingRuleRepository, RoutingRuleRepository>();
        services.AddScoped<ISlaConfigRepository, SlaConfigRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IMasterDataRepository, MasterDataRepository>();

        // Routing engine
        services.AddScoped<IRoutingEngine, RoutingEngine>();

        var redisConnectionString = configuration["Redis:ConnectionString"];
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = string.IsNullOrWhiteSpace(redisConnectionString)
                ? ConfigurationOptions.Parse("localhost:6379")
                : ConfigurationOptions.Parse(redisConnectionString);

            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 10000;
            options.ConnectRetry = 3;

            return ConnectionMultiplexer.Connect(options);
        });

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings?.Issuer,
                ValidAudience = jwtSettings?.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? string.Empty))
            };
        });

        services.AddLogging(builder => builder.AddConsole());
        services.AddHostedService<TokenBlacklistCleanupService>();
        services.AddHostedService<SlaMonitoringService>();
        return services;
    }
}

