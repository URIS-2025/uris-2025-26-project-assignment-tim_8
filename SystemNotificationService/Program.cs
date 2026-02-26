using Microsoft.EntityFrameworkCore;
using SystemNotificationService.Context;
using SystemNotificationService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddDbContext<SystemNotificationContext>();

builder.Services.AddScoped<ISystemNotificationRepository,SystemNotificationRepository>();

builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
