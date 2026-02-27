using Microsoft.EntityFrameworkCore;
using ProblemService.Context;
using ProblemService.Data;
using ProblemService.ServiceCalls;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ProblemContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProblemDB")));

builder.Services.AddDbContext<ProblemContext>();

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
