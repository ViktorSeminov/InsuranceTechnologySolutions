using Claims.Application;
using Claims.Infrastructure;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Claims;
using Claims.Infrastructure.ExceptionHendlers;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

var builder = WebApplication.CreateBuilder(args);

// Start Testcontainers for SQL Server and MongoDB and register them in DI
var sqlContainer = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        : new MsSqlBuilder()
    ).Build();

var mongoContainer = new MongoDbBuilder()
    .WithImage("mongo:latest")
    .Build();

// Start containers before configuring services that depend on them
await sqlContainer.StartAsync();
await mongoContainer.StartAsync();

// Register containers as singletons so the host disposes them on shutdown
builder.Services.AddSingleton(sqlContainer);
builder.Services.AddSingleton(mongoContainer);

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(sqlContainer.GetConnectionString()));


// Register MongoClient as a singleton using the started container
builder.Services.AddSingleton(sp => new MongoClient(mongoContainer.GetConnectionString()));

builder.Services.AddDbContext<ClaimsContext>((sp, options) =>
{
    var client = sp.GetRequiredService<MongoClient>();
    var databaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "ClaimsDb";
    var database = client.GetDatabase(databaseName);
    options.UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName);
});

builder.Services
    .AddApplication()
    .AddInfrastructure();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseCustomExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
    context.Database.Migrate();
}

app.Run();

public partial class Program { }
