using Hangfire;
using Hangfire.Logging;
using PRM.API;
using PRM.Application.Interfaces.Service;
using PRM.Infrastructure.AI;
using PRM.Infrastructure.Persistence;
using Hangfire.SqlServer; // Add this using if you use SQL Server storage for Hangfire
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging with Serilog
//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .WriteTo.Console()
//    .WriteTo.File("logs/prm-.log", rollingInterval: RollingInterval.Day)
//    .CreateLogger();

//builder.Host.UseSerilog((context, services, configuration) => configuration
//        .ReadFrom.Configuration(context.Configuration)
//        .ReadFrom.Services(services)
//        .Enrich.FromLogContext());


// ── Service Registration ──────────────────────────────────────
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.Configure<LlmSettings>(builder.Configuration.GetSection(LlmSettings.SectionName));
builder.Services.AddHttpClient("LlmClient");   // Named HttpClient for LLM calls
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "PRM API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

//app.UseSerilogRequestLogging();
// ── Database Migration + Seed ─────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PRMTDbContext>();
    await DatabaseSeeder.SeedAsync(dbContext);

    // Kick off recurring jobs based on system config
    var scheduler = scope.ServiceProvider.GetRequiredService<ISchedulerService>();
    var config = dbContext.SystemConfigurations.FirstOrDefault();
    scheduler.ScheduleRecurringJobs(config?.SchedulerIntervalHours ?? 4);
}

// ── Middleware Pipeline ───────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevelopmentCors");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard — restrict to Admin role in production
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // For development, no auth. In production, add: Authorization = [new HangfireAdminFilter()]
    IsReadOnlyFunc = _ => false
});

app.MapControllers();

app.Run();