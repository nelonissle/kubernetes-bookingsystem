# SkyBooker ✨

A modern, containerized microservice-based flight booking system built with **.NET 9**, featuring JWT authentication, message-based notifications via **RabbitMQ + Twilio**, and full observability using **Prometheus, Loki, Grafana, Filebeat, and Logstash**.

SkyBooker simulates a production-grade flight booking platform. It offers secure authentication, booking logic, real-time seat updates, and messaging services for customer notifications.

---

# Table of contents

- [SkyBooker ✨](#skybooker-)
- [Table of contents](#table-of-contents)
- [📊 Architecture](#-architecture)
  - [Directory Architecture](#directory-architecture)
  - [🔐 Authentication \& Authorization](#-authentication--authorization)
  - [Environment](#environment)
- [🚀 Microservices](#-microservices)
  - [OcelotApiGateway](#ocelotapigateway)
    - [🌐 API Gateway (Ocelot)](#-api-gateway-ocelot)
    - [🔧 Example Gateway Requests](#-example-gateway-requests)
    - [Ocelot or KONG?](#ocelot-or-kong)
  - [AuthService](#authservice)
  - [BookingService](#bookingservice)
    - [MariaDB (mysql) functions](#mariadb-mysql-functions)
  - [FlightService](#flightservice)
    - [MongoDb](#mongodb)
  - [MessagingService](#messagingservice)
- [Documentation \& Tests](#documentation--tests)
  - [API / Swagger](#api--swagger)
  - [Tests \& Validation](#tests--validation)
- [📊 Observability Stack / Monitoring](#-observability-stack--monitoring)
- [Build environment](#build-environment)
  - [Local Development](#local-development)
  - [📁 Docker](#-docker)
  - [Service links while running within local docker](#service-links-while-running-within-local-docker)
  - [Kubernetes](#kubernetes)
  - [⚡ Development Tips](#-development-tips)
  - [Upgrade .net](#upgrade-net)

---

# 📊 Architecture

```
                        +-----------------------+
                        |     Ocelot Gateway    |
                        |(JWT + Routing + Logs) |
                        +-----------------------+
                                    |
          +------------+-------------------------+------------+
          |            |                         |            |
  +--------------+ +----------------+ +---------------+  +-----------------+
  | AuthService  | | BookingService | | FlightService |  | MessagingService|
  +--------------+ +----------------+ +---------------+  +-----------------+
          |                   |              |                       |
       SQLite              MariaDB        MongoDB              RabbitMQ + Twilio
                                       (Flight data)
                                         RabbitMQ
```

- **API Gateway**: Ocelot routes and aggregates requests, handling authentication and logging.
- **Microservices**: Each core domain (auth, booking, flight, messaging) is a separate .NET service.
- **Databases**: Uses SQLite (auth), MariaDB (booking), MongoDB (flights).
- **Messaging**: RabbitMQ for asynchronous communication and WhatsApp notifications.
- **Observability**: Centralized logging (Serilog, Loki, Elasticsearch), metrics (Prometheus, Grafana).
- **Deployment**: Docker Compose for local dev, Kubernetes/Helm for production.

## Directory Architecture
```
SkyBooker (.net Solution setup)
│
├── AuthService                 # User-Login, Registrierung (SQLite)
│   └── AuthService.Tests       # Unit Tests für AuthService
│
├── BookingService              # Buchen von Flügen (MariaDB)
│   └── BookingService.Tests    # Unit Tests für BookingService
│
├── FlightService               # Fluginfos, Sitzplätze verwalten (MongoDB)
│   └── FlightService.Tests     # Unit Tests für FlightService
│
├── MessagingService            # RabbitMQ Consumer + WhatsApp (Twilio)
│
├── OcelotApiGateway            # API Gateway via Ocelot + JWT Auth
│
├── Monitoring                  # Configuration of Grafana, Prometheus ...
│
├── .env                        # ✅ Secrets: JWT-Key, DB, Twilio, ...
├── env-templ                   # ✅ Secrets: template, to copy to .env
│
├── build-*.ps1                 # Some build scripts as PowerShell
├── build-*.sh                  # Some build scripts as Unix Shell
│
├── docker-compose.yml          # Docker Compose config
│
└── SkyBookerSolution.sln       # Visual Studio Solution file
```

## 🔐 Authentication & Authorization
- JWT-based authentication via `AuthService`
- Roles: `Admin`, `Client`
- Role-based route protection via Ocelot Gateway
- Password policy: 15+ chars, with number, letter & special char

## Environment
We control all dynamic aspects of the system by environment variables. For development we can use .env.

The variable definition is within the file [env-template.txt](env-template.txt).

---

# 🚀 Microservices

Each microservice is build on .NET 9+. Use `dotnet new webapi -n YourService` to create an empty service.

## OcelotApiGateway
- Routes requests to respective downstream services
- Central entry point for all API requests, handles routing, JWT validation, and logging.
- Delegating Handler for logging
- JWT auth and Prometheus metrics support
- Install **Ocelot** via NuGet.  
- Configure `ocelot.json` to map internal routes to FlightService, BookingService, and AuthService.
- **Tech**: Ocelot, ASP.NET Core, Serilog, Prometheus.
- Dynamic config via environment variables, logs all requests/responses, exposes `/metrics` for Prometheus.

Key dependencies:
```sh
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Ocelot
```

Build commands:
```sh
dotnet clean
dotnet build
dotnet run
```

### 🌐 API Gateway (Ocelot)
- Configured via `ocelot.json`
- Environment variable placeholders (`{{VAR}}`) replaced at runtime
- Protected routes:
  - `/flight/**`
  - `/booking/**`
  - `/messaging/**`

### 🔧 Example Gateway Requests
- **Register:** `POST /auth/register`
- **Login:** `POST /auth/login`
- **Get Flights:** `GET /flight`
- **Create Flight:** `POST /flight/create` _(Admin only)_
- **Book Ticket:** `POST /booking`
- **Send Notification:** `POST /messaging/send`

Use Postman or HTTP clients with `Authorization: Bearer <token>`.

### Ocelot or KONG?

This is a question I asked myself relatively late in the project, as I realized that Ocelot is not as good as I initially thought.

Ocelot:
- Suitable for small projects
- .NET only
- No options for add-ons
- Easy to use

KONG:
- One of the most powerful tools in this area
- Well scalable
- Offers many add-ons such as OAuth2, Prometheus
- Open source
- Can be configured with DB or config files
- Steep learning curve



## AuthService
- User Registration & Login with roles "Admin" and "Client"
- On initialization a default user "admin" with role "Admin" is created with password AUTH_ADMIN_PWD
- Handles user registration, login, and JWT token issuance
- Secure password hashing with BCrypt
- JWT Token generation & validation
- Use SQLITE to store credentials (with EF), with table `User`
- Simple models for user data
- **Swagger** configuration
- **Tech**: ASP.NET Core, SQLite, Serilog, JWT.
- **Endpoints**:
  - `POST /api/register`: Register a new user (validates strong password, unique username/email, role).
  - `POST /api/login`: Authenticate and receive JWT.
  - `GET /api/admin/users`: With role "Admin" you will get a list of all users.
- **Security**: Passwords hashed with BCrypt, strong password policy enforced.

Key dependencies:
```sh
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
```

Build commands:
```sh
dotnet clean
dotnet build
dotnet ef database update
dotnet run
```
Use the `AuthLocalTest.http` to run RESTful tests on the started server.

If you change the core database, you can run `dotnet ef migrations add InitialCreate` to recreate the initial migration. Else the update is enough.


## BookingService
- Manages flight bookings, validates seat availability, and triggers notifications.
- MariaDB-based booking persistence (with EF)
- Flight seat check + update (via Gateway)
- RabbitMQ send message after booking
    - [ ] **Models** for booking data  
    - [ ] **MariaDB Server** integration (table: `Booking`)  
    - [ ] CRUD endpoints (POST, GET, GET/{id})  
    - [ ] **Seat validation** (no overbooking)  
    - [ ] **JWT protection** for API routes (via AuthService)  
    - [ ] **Swagger** configuration  
- **Tech**: ASP.NET Core, MariaDB, Serilog, RabbitMQ.
- **Endpoints**:
  - `GET /api/booking`: List all bookings.
  - `GET /api/booking/{id}`: Get booking by ID.
  - `POST /api/booking`: Create a new booking (checks for duplicates, seat availability, updates FlightService, sends notification).
- **Features**: Prevents duplicate bookings, integrates with RabbitMQ for messaging.

Key dependencies:
```sh
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
```

Build commands:
```sh
docker compose up -d mariadb
dotnet clean
dotnet build
dotnet ef database update
dotnet run
docker compose stop mariadb
```

If you change the core database, you can run `dotnet ef migrations add InitialCreate` to recreate the initial migration. Else the update is enough.

### MariaDB (mysql) functions
You can use the  `mysql -p root -u` in the container to access the MariaDB server with a SQL shell.

Make sure the EF Migration run before first use!

Run the following command to list all databases in the MariaDB Server instance:

```sql
SHOW DATABASES;
USE BookingDB;
SHOW TABLES;
select * from Bookings;
```
If the table is empty, insert some test data manually:

```sql
INSERT INTO Bookings (FlightId, PassengerId, PassengerFirstname, PassengerLastname, TicketCount, CreatedAt, UpdatedAt)
VALUES ('FL123', 'P001', 'John', 'Doe', 2, GETDATE(), GETDATE());
```

## FlightService
- Manages flight data, including CRUD operations and seat management.
- MongoDB-based flight management
- Seat reservation endpoint for bookings
- Admin-only flight creation
    - [ ] **Models** for flight data  
    - [ ] **MongoDB** integration (collection: `flights`)  
    - [ ] CRUD endpoints (POST, GET, GET/{id})  
    - [ ] **JWT protection** for API routes (via AuthService)  
    - [ ] **Swagger** configuration  
- For **FlightService** (MongoDB), optionally create collections & indexes initially.
- **Tech**: ASP.NET Core, MongoDB, Serilog.
- **Endpoints**:
  - `GET /api/flight`: List all flights.
  - `GET /api/flight/{id}`: Get flight by MongoDB ID.
  - `GET /api/flight/{flightId}`: Get flight by FlightId.
  - `POST /api/flight/create`: Create a new flight (admin only, with validation).
  - `PUT /api/flight/updateSeats/{flightId}`: Update available seats after booking.
- **Features**: Validates input, prevents duplicate flights, seeds dummy data.

### MongoDb

**Check MongoDB with mongosh**
```bash
# to enter the docker container with User Rights
docker exec -it mymongo mongosh -u mongoadmin -p secret

# to enter the docker with no Rights
docker exec -it mymongo bash

# to use mongosh in the container 
mongosh --host localhost --port 27017 -u mongoadmin -p secret

# to show the dbs 
show dbs

# to show the collections
show collections

# to view the data of the collection 
db.flights.find().pretty()
```

**I have tried this to create the flight collection**

first this into the shell

```bash
docker exec -it mymongo mongosh -u mongoadmin -p secret
```

then inside of the mongosh I have done this

```sql
-- use the db 
use FlightDb

-- show the collection
show collections

-- create the flightsdb and 
db.flights.insertOne({
  flightId: "FL123",
  airlineName: "Sky Airlines",
  source: "New York",
  destination: "Los Angeles",
  departure_Time: ISODate("2025-10-22T10:00:00Z"),
  arrival_Time: ISODate("2025-11-22T14:00:00Z"),
  available_Seats: 150,
  created_At: ISODate("2025-03-21T12:00:00Z"),
  updated_At: ISODate("2025-03-21T12:00:00Z")
})

-- to check its data inside
db.flights.find().pretty()
```


Add relevant dependencies:
```bash
dotnet add package MongoDB.Driver
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
```


Build commands:
```sh
docker compose up -d mongo
dotnet clean
dotnet build
dotnet run
docker compose stop mongo
```

## MessagingService
- Consumes messages from RabbitMQ and sends WhatsApp notifications via Twilio.
- RabbitMQ consumer
- Sends WhatsApp messages via Twilio API
- BackgroundService running as a daemon
    - [ ] **RabbitMQ** as message broker  
    - [ ] Events between FlightService & BookingService (e.g., seat updates)  
    - [ ] Advanced communication for booking confirmations
    - [ ] Integration of a WhatsApp API (e.g., Twilio)  
    - [ ] Sending a confirmation message when a new booking is made
- **Tech**: ASP.NET Core, RabbitMQ, Twilio, Serilog.
- **Features**: Reads Twilio credentials from environment, can be toggled on/off, logs all activity.


1. **RabbitMQ Integration** (if selected)  
   - Add RabbitMQ container to `docker-compose.yml`.  
   - In the services (FlightService, BookingService), set up the RabbitMQ client to exchange events.

2. **WhatsApp API** (if selected)  
   - Choose a provider (e.g., Twilio).  
   - Securely store API keys and secrets (e.g., in `appsettings.json` or via user secrets).  
   - Implement logic to send a WhatsApp message upon successful booking.


Build commands:
```sh
dotnet clean
dotnet build
dotnet run
```

---

# Documentation & Tests

## API / Swagger
   - In each microservice, enable **Swagger** configuration (in `Program.cs` or `Startup.cs`).  

## Tests & Validation

- Create **unit tests** for controllers and services (e.g., xUnit, NUnit, or MSTest).  
- Implement **FluentValidation** in controllers (e.g., validate request DTOs).  

- Each service has its own unit tests (see `*.Tests` projects).
- Example:  
  - `AuthService.Tests`: Validates registration, login, JWT logic.
  - `BookingService.Tests`: Validates booking logic, seat checks, duplicate prevention.
  - `FlightService.Tests`: Validates flight CRUD, seat updates.

Run tests with:
```bash
dotnet test
```

---

# 📊 Observability Stack / Monitoring
See [Montioring README](Monitoring/README.md)

---

# Build environment

## Local Development
The local development must be mixed with docker containers (like database, message broker). The own .net projects can be run locally or as docker or mixed as needed.

1. **Set up `.env`**: Copy `Env-template.txt` to `.env` and fill in secrets.

2. **Build & Run**:  
  ```powershell
  # In the SkyBooker folder
  ./deploy.ps1
  ```
  Or use on linux:
  ```bash
  ./build-clean.sh
  ./build-init.sh
  ./build-run.sh
  ```
Now you can continue with each service. Run them with `dotnet run` as needed.

## 📁 Docker

Create the local container images with `docker compose build`

The docker setup is documented as [docker-compose.yml](docker-compose.yml)
```bash
docker compose up --build -d
```

Services:
- `authservice`, `bookingservice`, `flightservice`, `messagingservice`, `ocelotapigateway`
- `rabbitmq`, `mariadb`, `mongo`, `grafana`, `prometheus`, `loki`, `logstash`, `promtail`

**Secrets via `.env` See Env-template.txt**

```bash
# Replace mariadb with the container name you want to watch
docker logs mariadb 
```

```bash
# change the name of the service to the docker you like to restart 
docker-compose down flightservice
docker-compose up -d --no-deps --build flightservice
```

## Service links while running within local docker
| Tool         | URL/Port |
|--------------|----------|
| API Gateway  | http://localhost:8000 |
| Prometheus   | http://localhost:9090 |
| Grafana      | http://localhost:3000 |
| RabbitMQ UI  | http://localhost:15672 |
| MongoDB      | localhost:27017 |
| MariaDB      | localhost:3306 |
| Loki         | http://localhost:3100 |

## Kubernetes
Kubernetes Dokumentation: [README.md](../Kubernetes/README.md)

## ⚡ Development Tips
- Use VS Code or JetBrains Rider with Docker plugin
- Logs are persisted under `./logs` on host machine
- `docker compose down -v` to clean everything

## Upgrade .net
dotnet tool install --global dotnet-outdated-tool

- update your .csproj "TargetFramework" to netN.N
- run `dotnet outdated` to see dependencies
- run `dotnet outdated -u` to upgrade
