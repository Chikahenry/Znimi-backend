using LoanApplication.Data;
using LoanApplication.Extensions;
using LoanApplication.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using LoanApplication.BackgroundJobs;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Loan Management API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// Database Configuration
builder.Services.AddDbContext<LoanManagementDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => 
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer(options =>
{
    options.SchedulePollingInterval = TimeSpan.FromMinutes(1);
    options.ServerName = "LoanManagementServer";
});

// Register background job service


// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "LoanManagementAPI",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "LoanManagementClient",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
// Register Services
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IBorrowerService, BorrowerService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ITokenService>(sp =>
    new TokenService(
        secretKey,
        jwtSettings["Issuer"] ?? "LoanManagementAPI",
        jwtSettings["Audience"] ?? "LoanManagementClient"
    ));
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddScoped<ILoanCalculatorService, LoanCalculatorService>();

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://lampndlight.netlify.app",
                            "http://loan.znimi.com.ng",
                            "http://localhost:5173",
                            "http://localhost:5175",
                            "http://localhost:5174")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseCors();
app.UseRouting();
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseActivityLogging();

app.MapControllers();

// Use Hangfire Dashboard (access at /hangfire)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Schedule recurring jobs
RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "update-loan-statuses",
    job => job.UpdateLoanStatusesJob(),
    "0 0 * * *"); // Daily at 8 AM

RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "send-payment-reminders",
    job => job.SendPaymentRemindersJob(),
    "0 8 * * *"); // Daily at 8 AM

RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "calculate-penalties",
    job => job.CalculatePenaltiesJob(),
    "0 0 * * *"); // Daily at midnight

RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "update-credit-scores",
    job => job.UpdateBorrowerCreditScoresJob(),
    "0 2 * * 0"); // Weekly on Sunday at 2 AM

RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "generate-daily-reports",
    job => job.GenerateDailyReportsJob(),
    "0 23 * * *"); // Daily at 11 PM

RecurringJob.AddOrUpdate<IBackgroundJobService>(
    "archive-closed-loans",
    job => job.ArchiveClosedLoansJob(),
    "0 3 1 * *"); // Monthly on 1st at 3 AM

app.Run();

// Hangfire Authorization Filter
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, add proper authorization
        return true; // For development
    }
}