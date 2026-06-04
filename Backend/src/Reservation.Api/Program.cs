using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Reservation.Api.Converters;
using Reservation.Api.Middleware;
using Reservation.Application;
using Reservation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.Converters.Add(new TimeOnlyHHmmConverter());
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Format unifié pour les erreurs de binding (corps JSON invalide, champs manquants)
builder.Services.Configure<ApiBehaviorOptions>(opts =>
{
    opts.InvalidModelStateResponseFactory = ctx =>
    {
        var errors = ctx.ModelState
            .SelectMany(ms => ms.Value!.Errors
                .Select(e => new { property = ms.Key, message = e.ErrorMessage }))
            .ToList();

        return new BadRequestObjectResult(new
        {
            code = "VALIDATION_ERROR",
            message = "Validation de la requête échouée.",
            details = new { errors },
        });
    };
});

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
             ?? ["http://localhost:4200"])
         .AllowAnyHeader()
         .AllowAnyMethod()));

// ── Pipeline ─────────────────────────────────────────────────────────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
