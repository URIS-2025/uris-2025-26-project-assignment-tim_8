using AnonymousRepository.Interfaces;
using AnonymousRepository.Repositories;
using Microsoft.EntityFrameworkCore;
using SuggestionService.Data;
using SuggestionService.ServiceCalls;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<SuggestionContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SuggestionDB")));

// Repositories
builder.Services.AddScoped<ISuggestionRepository, SuggestionRepository>();
builder.Services.AddScoped<ISuggestionCommentRepository, SuggestionCommentRepository>();
builder.Services.AddScoped<ISuggestionCategoryRepository, SuggestionCategoryRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();

// Service Calls
builder.Services.AddHttpClient<IUserServiceCall, UserServiceCall>();

// AutoMapper
builder.Services.AddAutoMapper(config => config.AddMaps(typeof(Program).Assembly));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SuggestionContext>();
    if (!app.Environment.IsEnvironment("Testing"))
        db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
public partial class Program { }