#!/usr/bin/env bash
# run build-clean and build-init first

# set the environment variables reading .env file
set -o allexport
source .env
set +o allexport

# set more environment variables for local docker usage
export AUTHSERVICE_SERVICE_HOST=localhost
export FLIGHTSERVICE_SERVICE_HOST=localhost
export BOOKINGSERVICE_SERVICE_HOST=localhost
export MESSAGINGSERVICE_SERVICE_HOST=localhost
export RABBITMQ_SERVICE_HOST=localhost
export OCELOTAPIGATEWAY_SERVICE_HOST=localhost
export OCELOTAPIGATEWAY_SERVICE_PORT=5112

# build docker images
docker compose build

# start common core services (dependencies like databases)
docker compose up -d mariadb
docker compose up -d rabbitmq
docker compose up -d mongo

# build .net project
# loop through AuthService, AuthService.Tests, BookService, BookService.Tests, FlightService, FlightService.Tests, MessageService and run dotnet clean, dotnet build
for project in FlightService MessagingService OcelotApiGateway; do
   echo "Building $project..."
   cd $project
   dotnet clean
   dotnet build
   cd ..
done

# loop through AuthService, BookService and run dotnet clean, dotnet build
echo "Start building"
for project in AuthService BookingService; do
   echo "Building $project..."
   cd $project
   mkdir data
   dotnet clean
   dotnet restore
   dotnet ef database update
   dotnet build
   cd ..
done

# build unit tests
for project in AuthService.Tests BookingService.Tests FlightService.Tests; do
   echo "Building $project..."
   cd $project
   dotnet clean
   dotnet build
   dotnet test
   cd ..
done


