using System.Security.Cryptography;
using System.Text;
using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Data;
using RaceDay.Api.Middleware;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication (Environment -> Config -> Ephemeral RNG fallback for isolated sandboxes)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
{
    secretKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}

builder.Services.AddAuthentication(options =>
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
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Azure Blob Storage SDK registration
var blobConnString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
    ?? builder.Configuration["AzureStorage:ConnectionString"]
    ?? "UseDevelopmentStorage=true";

builder.Services.AddSingleton(_ =>
{
    var clientOptions = new BlobClientOptions
    {
        Retry =
        {
            MaxRetries = 1,
            Delay = TimeSpan.FromMilliseconds(300),
            NetworkTimeout = TimeSpan.FromSeconds(3),
            Mode = RetryMode.Fixed
        }
    };
    return new BlobServiceClient(blobConnString, clientOptions);
});

// Service Layer DI
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEnrolmentService, EnrolmentService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger / OpenAPI with JWT Bearer support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "REST API for the RaceDay race event management system. " +
                      "Supports event creation, Azure Blob Storage banner & profile uploads, participant enrolment, and result recording " +
                      "with JWT-based role authentication (Organiser / Participant).",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "RaceDay Dev Team"
        }
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGciOiJIUzI1NiIsInR..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS (configured for front-end communication)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://localhost:52071", "http://localhost:52072", "http://localhost:8080" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("RaceDayWeb", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Middleware Pipeline
app.UseExceptionHandling();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
        options.RoutePrefix = string.Empty;
    });
}

// In Development the MVC front end (and its Docker container) calls the API over plain HTTP
// on port 52070; redirecting to HTTPS would break those server-to-server calls.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
// Start schema initialisation & seeding in the background so Kestrel binds port 52070 immediately,
// while ensuring any incoming HTTP request awaits completion before hitting controllers.
var dbInitTask = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbSeeder.InitialiseAndSeedAsync(db, logger);
});

app.Use(async (context, next) =>
{
    await dbInitTask;
    await next();
});

app.UseCors("RaceDayWeb");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
