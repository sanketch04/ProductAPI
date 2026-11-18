using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Product.BLL.Implementation.ProductService;
using Product.BLL.Implementation.ReportService_impl;
using Product.BLL.Interfaces;
using Product.BLL.Interfaces.ProductServices;
using Product.BLL.Interfaces.ReportSevrice;
using Product.BLL.Models;
using Product.BLL.Services;
using Product.DAL.Context;
using Product.DAL.Implementation.Auth;
using Product.DAL.Implementation.ProductRepo;
using Product.DAL.Interfaces;
using Product.DAL.Interfaces.ProductInterfaces;
using Product.DAL.Repository;
using QuestPDF.Infrastructure;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

// --------------------------------------------------
// Serilog + Elasticsearch
// --------------------------------------------------
var environment = builder.Environment.EnvironmentName;

var elasticUri =
    builder.Configuration["ElasticConfiguration:Uri"]
    ?? "http://localhost:9200";

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "ProductApi")
    .Enrich.WithProperty("Environment", environment)
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(
        new Uri(elasticUri))
    {
        AutoRegisterTemplate = true,
        IndexFormat =
            $"productapi-logs-{environment.ToLower()}-{{0:yyyy.MM.dd}}",
        NumberOfShards = 1,
        NumberOfReplicas = 0
    })
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information(
        "Starting Product API in {Environment} environment",
        environment);

    // Controllers
    builder.Services.AddControllers();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // SQL Server
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection")));

    // Repository dependency injection
    builder.Services.AddScoped<IProductRepository, ProductRepository>();

    // Business logic dependency injection
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IProductImageService, ProductImageService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IQrService, QrService>();
    builder.Services.AddScoped<IEmailService, EmailService>();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("Content-Disposition");
        });
    });

    // Rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter(
            "EmailRateLimit",
            limiterOptions =>
            {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder =
                    QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
    });

    builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

    var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JWT settings are missing.");

    if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
    {
        throw new InvalidOperationException(
            "JWT signing key is missing.");
    }

    var keyBytes = Convert.FromBase64String(jwtSettings.SecretKey);

    if (keyBytes.Length < 64)
    {
        throw new InvalidOperationException(
            "JWT signing key must contain at least 64 bytes.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,

                    ValidateAudience = true,

                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings.Issuer,

                    ValidAudience = jwtSettings.Audience,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(keyBytes),

                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = JwtRegisteredClaimNames.Name,

                    RoleClaimType = "role"
                };
        });

    builder.Services.AddAuthorization();
    // User Repository
    builder.Services.AddScoped<IUserRepository, UserRepository>();

    // Refresh Token Repository
    builder.Services.AddScoped<
        IRefreshTokenRepository,
        RefreshTokenRepository>();

    // Authentication Services
    builder.Services.AddScoped<IAuthService, AuthService>();

    builder.Services.AddScoped<IJwtService, JwtService>();

    builder.Services.AddScoped<
        IRefreshTokenService,
        RefreshTokenService>();
    #region Swagger JWT Authentication

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Product API",
            Version = "v1"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token. Example: eyJhbGciOi..."
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

    #endregion

    var app = builder.Build();

    // HTTP request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext =
            (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set(
                    "RequestHost",
                    httpContext.Request.Host.Value);

                diagnosticContext.Set(
                    "UserAgent",
                    httpContext.Request.Headers["User-Agent"].ToString());

                diagnosticContext.Set(
                    "ClientIp",
                    httpContext.Connection.RemoteIpAddress?.ToString());
            };
    });

    // Log unhandled request exceptions
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            throw;
        }
    });

    // Swagger enabled for local IIS troubleshooting.
    // Protect or disable Swagger before public production deployment.
    app.UseSwagger();
    app.UseSwaggerUI();

    // HTTPS redirection is intentionally omitted during
    // local IIS HTTP testing on port 8081.
    // Configure HTTPS before enabling it for production.

    // Static files
    app.UseStaticFiles();

    // CORS
    app.UseCors("AllowFrontend");

    // Rate limiting
    app.UseRateLimiter();

    // Authentication
    app.UseAuthentication();

    // Authorization
    app.UseAuthorization();

    // Controller endpoints
    app.MapControllers();

    Log.Information(
        "Product API configured. Environment: {Environment}",
        environment);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Product API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}