using OmniRoute.API.Middleware;
using OmniRoute.Application;
using OmniRoute.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanCreateLead",      p => p.RequireRole("TV"));
    options.AddPolicy("CanProcessLead",     p => p.RequireRole("SA"));
    options.AddPolicy("CanProcessTicket",   p => p.RequireRole("CS"));
    options.AddPolicy("CanDispatchToStore", p => p.RequireRole("DP"));
    options.AddPolicy("CanReassign",        p => p.RequireRole("TN", "QL"));
    options.AddPolicy("CanEscalate",        p => p.RequireRole("TN", "CS"));
    options.AddPolicy("CanManageTeam",      p => p.RequireRole("TN"));
    options.AddPolicy("CanManageStore",     p => p.RequireRole("QL"));
    options.AddPolicy("CanAdminSystem",     p => p.RequireRole("QT"));
    options.AddPolicy("CanViewDashboard",   p => p.RequireRole("BQL", "QT", "TN", "QL"));
});

var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: your token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseMiddleware<BannedUserMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program
{
}

