using Microsoft.EntityFrameworkCore;
using PlexNotifierr.Api.Quartz;
using PlexNotifierr.Core.Models;
using Quartz;
using Serilog;
using SilkierQuartz;
using static PlexNotifierr.Api.Extensions.PlexExtensions;
using static PlexNotifierr.Api.Extensions.PublicationExtensions;
using static PlexNotifierr.Api.Extensions.QuartzExtensions;

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

var configFile = new ConfigurationBuilder().AddXmlFile("app.config").AddJsonFile("appsettings.Development.json").Build();

AddRabbitMqSender(builder.Services, configFile);

AddPlexApi(builder.Services, configFile);

RegisterJobs(builder.Services);

builder.Services.AddQuartz(configFile);
builder.Services.AddSingleton<ITriggerListener, LoggingTriggerListener>();

AddSilkierQuartz(builder.Services);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.ConfigureSilkierQuartzHost();

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

app.UseSilkierQuartz();

ConfigureSilkierQuartzTriggers(app);

app.Run();