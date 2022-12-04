using Microsoft.EntityFrameworkCore;
using PlexNotifierr.Core.Models;
using Serilog;
using static PlexNotifierr.Api.Extensions.HangfireExtensions;
using static PlexNotifierr.Api.Extensions.PlexExtensions;
using static PlexNotifierr.Api.Extensions.PublicationExtensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PlexNotifierr");


// Add services to the container.
builder.Host.UseSerilog((hostContext, logging) => _ = logging.ReadFrom.Configuration(hostContext.Configuration));

builder.Services.AddControllers();
builder.Services.AddDbContext<PlexNotifierrDbContext>(options =>
    options.UseSqlite(
        connectionString,
        x => x.MigrationsAssembly("PlexNotifierr.Core"))
);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configFile = new ConfigurationBuilder().AddJsonFile($"appsettings.json").AddJsonFile($"appsettings.{environment}.json").AddEnvironmentVariables().Build();

AddRabbitMqSender(builder.Services, configFile);

AddPlexApi(builder.Services, configFile);

AddHangfire(builder.Services, configFile);
AddHangfireServer(builder.Services, configFile);
AddHangfireConsoleExtensions(builder.Services);

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

app.UseRouting();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
ConfigureHangfireDashboard(app);
ConfigureRecurringJob();

app.Run();