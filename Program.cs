using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Konfiguration laden
builder.Configuration.AddJsonFile("appsettings.json", optional:false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

//Dienste registrieren
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<AllowedDomainsConfig>(builder.Configuration.GetSection("AllowedDomains"));
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<CalendarService>();
builder.Services.AddSingleton<EmailProcessingJob>();

// Datenbank Service Provider aktivieren -> Online oder Test Datenbank auswählen
builder.Services.AddSingleton<AppointmentRepository>(sp =>
    new AppointmentRepository(
        builder.Configuration["Supabase:Url"],
        builder.Configuration["Supabase:Key"]
    ));

//Hangfire
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

//Controller und Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo{Title="VisitorApp Email Service", Version="v1"});
});

builder.Services.AddOpenApi();
var app = builder.Build();

// Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "VisitorApp Email Service v1"));

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

//Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] {new AllowAllDashboardAuthorizationFilter()}
});

// Hangfire Job starten

var cron = builder.Configuration.GetValue<string>("Hangfire:ScheduleInterval") ?? "0 */5 * * * *";
var timeZoneId = builder.Configuration.GetValue<string>("Hangfire:TimeZone") ?? "UTC";
var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId!);

RecurringJob.AddOrUpdate<EmailProcessingJob>(
    "email-processing",
    job => job.ExecuteAsync(),
    cron,
    timeZone);
    
app.Run();