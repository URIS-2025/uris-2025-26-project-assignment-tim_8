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
builder.Services.AddDbContext<SuggestionBoxContext>();

// ?? AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ?? Repository DI
builder.Services.AddScoped<ISuggestionBoxRepository, SuggestionBoxRepository>();

// ?? HttpClient za OrganizationService (ako validiraš OrganizationId)
builder.Services.AddHttpClient("OrganizationService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:OrganizationService"]); // PORT OrganizationService
});

// ?? Service Call DI
builder.Services.AddScoped<OrganizationServiceCall>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

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