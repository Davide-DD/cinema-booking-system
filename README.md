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

## Domains

The API is a single service split into three domains:

| Domain | Owns | Endpoints | Service |
|---|---|---|---|
| Programming | Movies and their screenings | `/movies` | `MovieService` |
| Venue | Screens and their rows | `/screens` | `ScreenService` |
| Booking | Bookings | `/bookings` | `BookingService`, `BookedSeatService` |

Each service reads and writes only the tables of its own domain. Booking needs the date of a screening and the rows of its screen: it asks `MovieService.GetScreeningAsync` and `ScreenService.GetScreenAsync` for them instead of querying their tables. In the same way, Programming asks `ScreenService.GetScreenAsync` whether the screen of a screening exists. Before a change that could leave booked seats behind, Programming and Venue ask Booking through `BookedSeatService`, which is separate from `BookingService` because that one already depends on their services. Entities refer to other domains only by id (`Booking.ScreeningId`, `Booking.RowId`, `Screening.ScreenId`), never through navigation properties.

A single service with one database is enough for this size: a booking is saved in one transaction and there is nothing to keep in sync. To split it into separate services:

- Each domain gets its own service and database, and the foreign keys between domains (bookings to screenings and rows, screenings to screens) go away.
- Booking keeps its own copy of screening dates and screen rows, updated by events from the other two services, such as a screening scheduled or the rows of a screen changed. Bookings then keep working when the other services are down. The simpler alternative is calling them at booking time, behind the same two methods used today.
- Programming checks that the screen of a new screening exists, from its own copy of the screens or by asking Venue.
- Deleting a screen or a movie now removes its screenings and bookings through cascade deletes in the database. Across services this becomes an event, such as a screening cancelled, that Booking handles by cancelling its bookings.

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

## Running with Docker

```bash
docker compose up --build
```

`docker-compose.yml` builds the API image and runs it in Development next to its own PostgreSQL, so mock authentication and the sample data work as described above. Swagger UI is at http://localhost:8080/swagger. As in Development, the database is reset every time the API starts.

The image published by the CI runs in Production: there the API doesn't apply migrations, seed data or accept mock tokens. In a real deployment the pipeline would apply the migrations before starting the new version, for example with an EF Core migration bundle (`dotnet ef migrations bundle`), so that several instances never migrate the database at the same time.

## Authentication

In Development the API uses a mock authentication scheme, enabled by `MockAuthentication` in `appsettings.Development.json`. The bearer token is the numeric user id, optionally followed by a role:

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
| `GET` | `/bookings?page=1&pageSize=10` | Any user | Bookings of the current user |
| `POST` | `/bookings` | Any user | Books one or more seats |
| `GET` | `/bookings/availability?screeningId=1&rowId=1&seatNumber=5` | Any user | Tells whether a seat is still free |
| `GET` | `/bookings/seats?screeningId=1` | Any user | Rows of the screening's screen with their booked seats, to draw the seat map |
| `DELETE` | `/bookings/{id}` | Booking owner | Cancels a booking of an upcoming screening |

Updating a movie or a screen replaces its screenings or rows with the ones in the body: the ones left out are deleted. New or rescheduled screenings must be in the future, while past screenings can stay in the body unchanged. Screening dates must include a time zone (`Z` or an offset like `+01:00`) and are stored in UTC.

Mistakes can be corrected freely until they affect bookings. Moving a screening with bookings to another screen, removing a row with bookings or lowering the seats of a row below a booked seat returns `409 Conflict` instead of cancelling those bookings or leaving them on seats that don't exist: the customers holding them need a decision, not a silent change. Deleting the screening and creating it again cancels its bookings.

Lists are ordered by id. `page` defaults to 1 and `pageSize` to 10. A `page` lower than 1 is treated as 1, a page past the end returns an empty list and `pageSize` is kept between 1 and 100.

Errors are returned as [problem details](https://www.rfc-editor.org/rfc/rfc9457) with the matching status code.

## Booking rules

- Every booking request has a `screeningId` and either both `rowId` and `seatNumber` or neither of them. Seats are numbered from 1 in each row, and a row has from 1 to 100 seats.
- A request books from 1 to 10 seats, and it is all or nothing: if one of them can't be confirmed, none is saved.
- Past screenings can't be booked or cancelled.
- A seat already taken returns `409 Conflict`.
- Without a row and a seat, the API picks the best free seat. Each seat gets a score that decreases with its distance from the central row and from the centre of its row (`Seat.Score`). The free seat with the highest score wins. Rows are searched from the central one outwards, and the search stops as soon as no row left can beat the best seat found, since a seat scores at most 1 more than its row. If the screen is full, the request returns `409 Conflict`.

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
- Multiple cinemas: the API manages a single cinema. Supporting more would add a `Cinema` to the venue domain owning its screens, with screenings and bookings reaching it through their screen.
