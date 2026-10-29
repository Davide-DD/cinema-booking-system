# Cinema Booking System

REST API to manage movies, screens and screenings of a cinema and to book seats, choosing the best free seat when the customer doesn't pick one.

Built with .NET 10 minimal APIs, Entity Framework Core on PostgreSQL, xUnit and Testcontainers.

## Projects

| Project | Content |
|---|---|
| `DavideDiDonato.CinemaBookingSystem.API` | Endpoints, services, exception handling, authentication |
| `DavideDiDonato.CinemaBookingSystem.Domain` | Entities, validation and seat scoring |
| `DavideDiDonato.CinemaBookingSystem.Infrastructure` | `DbContext`, migrations and development seeder |
| `DavideDiDonato.CinemaBookingSystem.UnitTest` | Unit tests for the seat scoring |
| `DavideDiDonato.CinemaBookingSystem.IntegrationTests` | API tests against a real PostgreSQL started by Testcontainers |

## Running locally

Requirements: .NET 10 SDK and Docker.

1. Start PostgreSQL:

   ```bash
   docker run -d --name cinema-booking-system-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:18-alpine
   ```

2. Store the connection string in the user secrets of the API:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DatabaseConnectionString" "Host=localhost;Port=5432;Database=cinema_booking_system;Username=postgres;Password=postgres" --project DavideDiDonato.CinemaBookingSystem/DavideDiDonato.CinemaBookingSystem.API
   ```

3. Run the API:

   ```bash
   dotnet run --project DavideDiDonato.CinemaBookingSystem/DavideDiDonato.CinemaBookingSystem.API --launch-profile http
   ```

4. Open Swagger UI at http://localhost:5152/swagger.

In Development the API applies the migrations and **resets the database on every start**, then seeds two screens, two movies and four upcoming screenings. Never point the Development environment to a database you want to keep.

## Authentication

In Development the API uses a mock authentication scheme, enabled by `MockAuthentication` in `appsettings.Development.json`. The bearer token is the user id, optionally followed by a role:

| Header | User |
|---|---|
| `Authorization: Bearer 1` | Customer with id 1 |
| `Authorization: Bearer 1:Admin` | Admin with id 1 |

In Swagger UI, press **Authorize** and type only the token (`1` or `1:Admin`).

## Endpoints

| Method | Route | Allowed to | Description |
|---|---|---|---|
| `GET` | `/movies?page=1&pageSize=10` | Any user | Movies with their screenings |
| `PUT` | `/movies` or `/movies/{id}` | Admin | Creates a movie (no id) or updates it with its screenings |
| `DELETE` | `/movies/{id}` | Admin | Deletes a movie with its screenings and their bookings |
| `GET` | `/screens?page=1&pageSize=10` | Any user | Screens with their rows |
| `PUT` | `/screens` or `/screens/{id}` | Admin | Creates a screen (no id) or updates it with its rows |
| `DELETE` | `/screens/{id}` | Admin | Deletes a screen with its rows, screenings and bookings |
| `POST` | `/bookings` | Any user | Books one or more seats |
| `GET` | `/bookings?screeningId=1&rowId=1&seatNumber=5` | Any user | Tells whether a seat is still free |
| `DELETE` | `/bookings/{id}` | Booking owner | Cancels a booking of an upcoming screening |

Updating a movie or a screen replaces its screenings or rows with the ones in the body: the ones left out are deleted.

Lists are ordered by id. A `page` lower than 1 is treated as 1 and `pageSize` is kept between 1 and 100.

Errors are returned as [problem details](https://www.rfc-editor.org/rfc/rfc9457) with the matching status code.

## Booking rules

- Every booking request has a `screeningId` and either both `rowId` and `seatNumber` or neither of them. Seats are numbered from 1 in each row.
- A request with several bookings is all or nothing: if one of them can't be confirmed, none is saved.
- Past screenings can't be booked or cancelled.
- A seat already taken returns `409 Conflict`.
- Without a row and a seat, the API picks the best free seat. Each seat gets a score that decreases with its distance from the central row and from the centre of its row (`Seat.Score`). If the central row has a free seat, its most central one is taken; otherwise the free seat with the highest score in the other rows wins. If the screen is full, the request returns `409 Conflict`.

## Tests

```bash
dotnet test DavideDiDonato.CinemaBookingSystem/DavideDiDonato.CinemaBookingSystem.sln
```

Docker must be running: the integration tests start their own PostgreSQL container and reseed it after every test.

## Continuous integration

| Workflow | Trigger | What it does |
|---|---|---|
| `pr-checks.yml` | Pull request to `main` | Builds the solution, runs unit and integration tests |
| `docker-publish.yml` | Tag like `1.0.0` | Builds the API image and pushes it to GitHub Container Registry |

## Out of scope for now

- Login and user management. Outside Development the API only accepts bearer tokens, but no endpoint issues them yet.
- Managing roles: the admin role only comes from the mock token.
- Admins cancelling bookings of other users.
