using Microsoft.EntityFrameworkCore;
using SuggestionBoxService.Context;
using SuggestionBoxService.Data;
using SuggestionBoxService.ServiceCalls;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using SuggestionBoxService.Profiles;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// Add services to the container.
// ===============================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ?? DbContext
builder.Services.AddDbContext<SuggestionBoxContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SuggestionBoxDB")));

// ?? AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// ?? Repository DI
builder.Services.AddScoped<ISuggestionBoxRepository, SuggestionBoxRepository>();
builder.Services.AddScoped<SuggestionBoxService.Clients.LoggerServiceClient>();


// ?? HttpClient za OrganizationService (ako validiraš OrganizationId)
builder.Services.AddHttpClient("OrganizationService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:OrganizationService"]); // PORT OrganizationService
});

builder.Services.AddHttpClient("LoggerService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:LoggerServiceBaseUrl"]!);
});

// ?? Service Call DI
builder.Services.AddScoped<OrganizationServiceCall>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SuggestionBoxContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
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