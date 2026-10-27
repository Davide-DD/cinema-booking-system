using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IBookingService, BookingService>();

var connectionString =
    builder.Configuration.GetConnectionString("DatabaseConnectionString")
    ?? throw new InvalidOperationException("Connection string not found.");
builder.Services.AddDbContext<CinemaBookingSystemDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapBookingEndpoints();

app.Run();
