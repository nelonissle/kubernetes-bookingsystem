# SkyBooker BookingService Dockerfile
# run docker build -t bookingservice:latest .

# Base image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "BookingService.csproj"
RUN dotnet publish "BookingService.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Designate /app/Logs as a volume so logs can be persisted externally
VOLUME ["/app/logs"]
ENTRYPOINT ["dotnet", "BookingService.dll"]
