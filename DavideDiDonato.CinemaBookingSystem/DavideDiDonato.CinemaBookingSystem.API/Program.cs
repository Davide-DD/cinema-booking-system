using System.Net;
using System.Text.Json.Serialization;
using DavideDiDonato.CinemaBookingSystem.API.Endpoints;
using DavideDiDonato.CinemaBookingSystem.API.Exceptions;
using DavideDiDonato.CinemaBookingSystem.API.Security;
using DavideDiDonato.CinemaBookingSystem.API.Services;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database.Seeder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});
builder.Services.AddScoped<Seeder>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IScreenService, ScreenService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddAuthentication("Selector")
    .AddPolicyScheme("Selector", "Bearer token or API key", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var environment = context.RequestServices.GetService<IWebHostEnvironment>()!;
            return environment.IsDevelopment() && builder.Configuration["MockAuthentication"] == "true"
                ? MockAuthenticationOptions.DefaultScheme : BearerTokenDefaults.AuthenticationScheme;
        };
    })
    .AddScheme<MockAuthenticationOptions, MockAuthenticationHandler>(MockAuthenticationOptions.DefaultScheme, options => {})
    .AddBearerToken();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

var connectionString =
    builder.Configuration.GetConnectionString("DatabaseConnectionString")
    ?? throw new InvalidOperationException("Connection string not found.");
builder.Services.AddDbContext<CinemaBookingSystemDbContext>(options => { options.UseNpgsql(connectionString); });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CinemaBookingSystemExceptionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
    using IServiceScope scope = app.Services.CreateScope();
    scope.ServiceProvider.GetService<Seeder>()!.SeedData().GetAwaiter().GetResult();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapBookingEndpoints();
app.MapMovieEndpoints();
app.MapScreenEndpoints();

app.Run();