using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProblemBoxService.Context;
using ProblemBoxService.Data;
using ProblemBoxService.Profiles;
using ProblemBoxService.ServiceCalls;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<ProblemBoxContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProblemBoxDB")));

builder.Services.AddScoped<IProblemBoxRepository, ProblemBoxRepository>();

builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProblemService, ProblemService>();

builder.Services.AddHttpClient("ProblemService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ProblemService"]); // PORT BillingService
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProblemBoxContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
