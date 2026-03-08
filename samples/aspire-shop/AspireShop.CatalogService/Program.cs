using AspireShop.CatalogDb;
using AspireShop.CatalogService;
using AspireShop.NlpCache;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

// Register the NLP/ML caching library (tokenizer + cache) for code intelligence features.
builder.Services.AddNlpCache(options =>
{
    options.MaxEntries = 10_000;
    options.AutoPurge = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler();
}

app.MapCatalogApi();
app.MapDefaultEndpoints();

app.Run();
