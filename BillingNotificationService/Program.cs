using System.Text;
using BillingNotificationService.Context;
using BillingNotificationService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<BillingNotificationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BillingNotificationDB")));


builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));
builder.Services.AddScoped<IBillingNotificationRepository, BillingNotificationRepository>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("LoggerService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:LoggerServiceBaseUrl"]!);
});
builder.Services.AddScoped<BillingNotificationService.Clients.LoggerServiceClient>();

// Accept JWTs minted by any of the system's issuers (AnonymousUserService, OrganizationService)
// so that producer calls forwarding either an anonymous-user or an org-user bearer validate here.
// Keys/issuers/audiences come from the "Jwt" config section (env-overridable in Docker).
var jwtKeys = builder.Configuration.GetSection("Jwt:Keys").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuers = builder.Configuration.GetSection("Jwt:ValidIssuers").Get<string[]>(),
            ValidAudiences = builder.Configuration.GetSection("Jwt:ValidAudiences").Get<string[]>(),
            IssuerSigningKeys = jwtKeys.Select(k => (SecurityKey)new SymmetricSecurityKey(Encoding.UTF8.GetBytes(k)))
        };
    });

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingNotificationContext>();
    if (!app.Environment.IsEnvironment("Testing"))
        db.Database.Migrate();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }
