using RFactAI.Api.Endpoints;
using RFactAI.Application.Facts;
using RFactAI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IFactService, FactService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "RFactAI v1");
        o.DocumentTitle = "RFactAI";
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapFactEndpoints();

app.Run();

public partial class Program;
