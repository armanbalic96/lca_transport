using Microsoft.AspNetCore.Diagnostics;
using TransportGwp.Application.Interfaces;
using TransportGwp.Infrastructure;
using TransportGwp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Prototype: create the SQLite schema on start-up. Switch to EF migrations before production use.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<TransportDbContext>().Database.EnsureCreated();
}

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = error switch
    {
        DuplicateNameException => (StatusCodes.Status409Conflict, error.Message),
        ArgumentException => (StatusCodes.Status400BadRequest, error.Message),
        AiUnavailableException => (StatusCodes.Status503ServiceUnavailable, error.Message),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
    };
    context.Response.StatusCode = status;
    await Results.Problem(title: title, statusCode: status).ExecuteAsync(context);
}));

app.MapControllers();

app.Run();
