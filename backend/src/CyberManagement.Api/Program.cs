using CyberManagement.Api.Configuration;
using CyberManagement.Api.Data;
using CyberManagement.Api.Middleware;
using CyberManagement.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Neo4j.Driver;
using Serilog;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ──────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/cyber-management-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Configuration Binding ────────────────────────────────
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<DiscoveryEngineOptions>(builder.Configuration.GetSection("DiscoveryEngine"));
builder.Services.Configure<GlpiOptions>(builder.Configuration.GetSection("Glpi"));
builder.Services.Configure<SiemOptions>(builder.Configuration.GetSection("Siem"));

// ─── Database ─────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.EnableRetryOnFailure(5)));

// ─── Redis ────────────────────────────────────────────────
var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConn));

// ─── Neo4j ────────────────────────────────────────────────
var neo4jUri = builder.Configuration["Neo4j:Uri"] ?? "bolt://localhost:7687";
var neo4jUser = builder.Configuration["Neo4j:Username"] ?? "neo4j";
var neo4jPass = builder.Configuration["Neo4j:Password"] ?? "password";
builder.Services.AddSingleton<IDriver>(
    GraphDatabase.Driver(neo4jUri, AuthTokens.Basic(neo4jUser, neo4jPass)));

// ─── Hangfire (background jobs) ───────────────────────────
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();

// ─── JWT Authentication ───────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CyberManagement";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CyberManagementUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// ─── Application Services ─────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IVulnerabilityService, VulnerabilityService>();
builder.Services.AddScoped<IRiskService, RiskService>();
builder.Services.AddScoped<IThreatIntelService, ThreatIntelService>();
builder.Services.AddScoped<ISiemService, SiemService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IDiscoveryService, DiscoveryService>();
builder.Services.AddHttpClient<IGlpiService, GlpiService>();

// ─── FluentValidation ─────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ─── Controllers ──────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

// ─── Swagger ──────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "سامانه مدیریت دارایی‌های سایبری",
        Version = "v1",
        Description = "Cyber Asset Management Platform API"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Bearer token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ─── CORS ─────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ─── Health Checks ────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "")
    .AddRedis(redisConn);

// ─── Build ────────────────────────────────────────────────
var app = builder.Build();

// ─── Auto-migrate and Seed ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

// ─── Middleware pipeline ──────────────────────────────────
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cyber Management API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<AuditMiddleware>();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
