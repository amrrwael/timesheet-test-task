using MongoDB.Driver;
using Timesheet.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

var app = builder.Build();

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