FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MessagingService.csproj", "./"]
RUN dotnet restore "MessagingService.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "MessagingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MessagingService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Designate /app/Logs as a volume so logs are persisted externally
VOLUME ["/app/logs"]
ENTRYPOINT ["dotnet", "MessagingService.dll"]
