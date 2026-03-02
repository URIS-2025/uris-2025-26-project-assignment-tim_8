using AnonymousUserService.Context;
using AnonymousUserService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AnonymousUserContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AnonymousUserDB")));

builder.Services.AddScoped<IAnonymousUserRepository, AnonymousUserRepository>();

builder.Services.AddScoped<IBoxAccessLinkRepository, BoxAccessLinkRepository>();

builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnonymousUserContext>();
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
