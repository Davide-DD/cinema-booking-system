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
builder.Services.AddScoped<IBookedSeatService, BookedSeatService>();
builder.Services.AddScoped<IScreenService, ScreenService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddAuthentication("Selector")
    .AddPolicyScheme("Selector", "Bearer token or mock authentication", options =>
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
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Roles.Admin, policy => policy.RequireRole(Roles.Admin));
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DatabaseConnectionString");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string not found.");
builder.Services.AddDbContext<CinemaBookingSystemDbContext>(options => { options.UseNpgsql(connectionString); });

builder.Services.AddExceptionHandler<CinemaBookingSystemExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
    using IServiceScope scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<Seeder>().SeedData();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBookingEndpoints();
app.MapMovieEndpoints();
app.MapScreenEndpoints();

app.Run();