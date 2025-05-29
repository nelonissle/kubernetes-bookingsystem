FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["OcelotApiGateway.csproj", "./"]
RUN dotnet restore "OcelotApiGateway.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "OcelotApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OcelotApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy the ocelot.json file into the container
COPY ocelot.json /app/ocelot.json
# Persist logs by designating /app/Logs as a volume
VOLUME ["/app/logs"]
ENTRYPOINT ["dotnet", "OcelotApiGateway.dll"]
