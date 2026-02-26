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
builder.Services.AddAutoMapper(typeof(Program));

// 🔹 Repository DI
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();

// 🔹 HttpClient za mikroservise
builder.Services.AddHttpClient("OrganizationService", client =>
{
    client.BaseAddress = new Uri("https://localhost:5001"); // PORT OrganizationService
});

builder.Services.AddHttpClient("BillingService", client =>
{
    client.BaseAddress = new Uri("https://localhost:5002"); // PORT BillingService
});

// 🔹 Service Calls DI
builder.Services.AddScoped<OrganizationServiceCall>();
builder.Services.AddScoped<BillingServiceCall>();

var app = builder.Build();

// ===============================
// Configure the HTTP request pipeline.
// ===============================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();