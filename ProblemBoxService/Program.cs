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

builder.Services.AddDbContext<ProblemBoxContext>();

builder.Services.AddScoped<IProblemBoxRepository, ProblemBoxRepository>();

builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProblemService, ProblemService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
