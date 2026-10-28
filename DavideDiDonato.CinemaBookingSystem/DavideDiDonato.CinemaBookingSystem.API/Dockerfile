FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DavideDiDonato.CinemaBookingSystem.API/DavideDiDonato.CinemaBookingSystem.API.csproj", "DavideDiDonato.CinemaBookingSystem.API/"]
COPY ["DavideDiDonato.CinemaBookingSystem.Infrastructure/DavideDiDonato.CinemaBookingSystem.Infrastructure.csproj", "DavideDiDonato.CinemaBookingSystem.Infrastructure/"]
COPY ["DavideDiDonato.CinemaBookingSystem.Domain/DavideDiDonato.CinemaBookingSystem.Domain.csproj", "DavideDiDonato.CinemaBookingSystem.Domain/"]
RUN dotnet restore "DavideDiDonato.CinemaBookingSystem.API/DavideDiDonato.CinemaBookingSystem.API.csproj"
COPY . .
WORKDIR "/src/DavideDiDonato.CinemaBookingSystem.API"
RUN dotnet build "./DavideDiDonato.CinemaBookingSystem.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DavideDiDonato.CinemaBookingSystem.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DavideDiDonato.CinemaBookingSystem.API.dll"]
