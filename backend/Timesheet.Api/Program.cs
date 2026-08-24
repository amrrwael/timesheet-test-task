using System.Text.Json.Serialization;
using MongoDB.Driver;
using Timesheet.Api.Contracts;
using Timesheet.Api.Domain.Exceptions;
using Timesheet.Api.Middleware;
using Timesheet.Api.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using System.ComponentModel.Design;
using Timesheet.Api.Services;
using FluentValidation;
using Timesheet.Api.Services.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration["Mongo:ConnectionString"]));

builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>()
    .GetDatabase(builder.Configuration["Mongo:DatabaseName"]));

builder.Services.AddScoped<ReferenceService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<TimeEntryService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTimeEntryRequestValidator>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

await MongoIndexInitializer.EnsureIndexesAsync(
    app.Services.GetRequiredService<IMongoDatabase>());

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();

app.MapGet("/health", async (IMongoDatabase db) =>
{
    try
    {
        await db.ListCollectionNamesAsync();
        return Results.Ok(new { status = "ok", database = db.DatabaseNamespace.DatabaseName });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "error", detail = ex.Message }, statusCode: 503);
    }
});

app.Run();