using Microsoft.EntityFrameworkCore;
using ProblemService.Context;
using ProblemService.Data;
using ProblemService.ServiceCalls;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ProblemContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProblemDB")));

builder.Services.AddScoped<IProblemRepository, ProblemRepository>();
builder.Services.AddScoped<IProblemCommentRepository, ProblemCommentRepository>();
builder.Services.AddScoped<IProblemCategoryRepository, ProblemCategoryRepository>();

builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IProblemCommentAuthorUserService, ProblemCommentAuthorUserService>();
builder.Services.AddScoped<ProblemService.Clients.LoggerServiceClient>();


builder.Services.AddHttpClient("LoggerService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:LoggerServiceBaseUrl"]!);
});

builder.Services.AddHttpClient("AttachmentService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AttachmentService"]); // PORT OrganizationService
});

builder.Services.AddHttpClient("OrganizationService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:OrganizationService"]); // PORT BillingService
});


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProblemContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            await context.Response.WriteAsJsonAsync(new
            {
                message = error.Error.Message
            });
        }
    });
});

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }