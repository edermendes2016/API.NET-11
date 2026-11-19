using Alibi.Api;
using Alibi.Infrastructure;
using Alibi.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAlibiInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

await DatabaseStartup.InitializeAsync(app.Services, app.Logger);

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapOpenApi();
app.MapScalarApiReference("/docs", o => o.WithTitle("Álibi API"));

app.MapGameEndpoints();

app.Run();

public partial class Program;
