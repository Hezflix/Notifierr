
using Microsoft.EntityFrameworkCore;
using PlexNotifierr.Core.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PlexNotifierr");

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<PlexNotifierrDbContext>(options =>
                  options.UseSqlite(
                            connectionString,
                            x => x.MigrationsAssembly("PlexNotifierr.Core"))
                   );
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
