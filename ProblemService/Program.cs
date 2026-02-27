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

builder.Services.AddHttpClient("AttachmentService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AttachmentService"]); // PORT OrganizationService
});

builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"]); // PORT BillingService
});


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
