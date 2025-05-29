# SkyBooker AuthService Dockerfile
# run docker build -t authservice:latest .

# Base image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "AuthService.csproj"
RUN dotnet publish "AuthService.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
VOLUME ["/app/data"]
VOLUME ["/app/logs"]
ENTRYPOINT ["dotnet", "AuthService.dll"]
