using Microsoft.EntityFrameworkCore;
using SubscriptionService.Context;
using SubscriptionService.Data;
using SubscriptionService.ServiceCalls;
using AutoMapper;
using SubscriptionService.ServiceCalls.SubscriptionService.ServiceCalls;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// Add services to the container.
// ===============================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 DbContext
builder.Services.AddDbContext<SubscriptionContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SubscriptionDB")));

// 🔹 AutoMapper
builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));

// 🔹 Repository DI
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();

builder.Services.AddScoped<SubscriptionService.Clients.LoggerServiceClient>();


// 🔹 HttpClient za mikroservise
builder.Services.AddHttpClient("OrganizationService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:OrganizationService"]); // PORT OrganizationService
});

builder.Services.AddHttpClient("BillingNotificationService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:BillingNotificationService"]); // PORT BillingService
});

builder.Services.AddHttpClient("LoggerService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:LoggerServiceBaseUrl"]!);
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

// 🔹 Service Calls DI
builder.Services.AddScoped<OrganizationServiceCall>();
builder.Services.AddScoped<BillingServiceCall>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SubscriptionContext>();
    if (!app.Environment.IsEnvironment("Testing"))
    {
        db.Database.Migrate();
        SubscriptionPlanSeeder.Seed(db);
    }
}

// ===============================
// Configure the HTTP request pipeline.
// ===============================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }