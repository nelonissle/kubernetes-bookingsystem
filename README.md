# SkyBooker – Microservices Flight Booking Platform

From a school project, I enhanced the solution with more features like what's app integration, RabbitMQ Messaging, Grafana / Elasticsearch monitoring and Kubernetes support.

**SkyBooker** is a global flight booking and ticket reservation platform. It enables the management of flights (flight schedule management) and the booking of these flights by passengers (flight booking). The microservices are implemented in **ASP.NET Core (C#)**, with different databases and **Docker containerization** for scalable deployment. My implementation also supports a monitoring suite based on **Grafana and Elasticsearch** and runs on **Kubernetes**. I also used **GitHub Actions** to automatically perform "build", "test", and "deploy". During "deploy", the containers are pushed to my Docker Hub account and can be used directly in Docker Compose or Kubernetes.

---

# Microservices (Overview)

1. **FlightService (Flight Schedule Management)**
    - Management of flight information (departure times, airlines, routes, availability)
    - Database: MongoDB (separate container)

2. **BookingService (Flight Booking)**
    - Management of passenger bookings
    - Database: MariaDB Server (separate container)

3. **AuthService (Authentication)**
    - User registration and login (JWT)
    - Database: SQLite

## Core Requirements

- **A1:** Flight schedule, booking, and authentication services fully implemented  
- **A2:** Databases for all three services implemented (MongoDB, MariaDB, SQLite)  
- **A3:** Docker container for each service + Docker Compose  
- **A4:** JWT authentication implemented  
- **A5:** API documentation (OpenAPI/Swagger)  
- **A6:** Unit tests for all microservices  
- **A7:** Git repository for the software project  
- **A8:** Project planning (Gantt) & time planning  

## Extensions

I have further enhanced the standard services with the following features:

- **API Gateway (Ocelot)** – central entry point for all microservices
- **RabbitMQ** for advanced communication between services 
- **WhatsApp API integration** when a booking is made

See [SkyBooker Code README for more information](SkyBooker/README.md)

---

# Infrastructure

- **Docker Compose**: For local development, spins up all services, databases, and observability stack. See more in the [README of SkyBooker](SkyBooker/README.md)
- **Kubernetes/Helm**: Production-ready deployment with charts for each service and infra component. See more in the [README of Kubernetes](Kubernetes/README.md)
- **CI/CD**: GitHub Actions workflow for build, test, and deployment. See further below.

---

# CI/CD

**CI/CD** stands for:
- **CI = Continuous Integration**  
  → Code is **constantly automatically tested and merged**, e.g., after every commit.
- **CD = Continuous Delivery / Deployment**  
  → Code is **automatically provided or directly published**, e.g., on a server or in the cloud.

## Why do you need CI/CD?
- **Errors are found faster**
- **Faster testing & publishing**
- **Less manual work**
- **More reliability in teams**

## ✅ GitHub Actions CI/CD
There is a template file for the GitHub Action: [GitHub Action Workflow example file](githubaction-template.yml).

You need 2 secrets for the docker hub publish:
- DOCKER_USERNAME
- DOCKER_PASSWORD

Use a token as password and save it as secrets on your repository.

`.github/workflows/dotnet.yml` includes:
- Automatic restore, build & test on push to `main`
- Docker builds and push to:
  - DockerHub: `nelonissle7/kub_<service>` (only support public hub repositories)
  - GHCR: `ghcr.io/nelonissle/kub_<service>` (works only on public repos or with license)

---

# 📊 Observability Stack / Monitoring
- **Serilog**: Logs to both global and per-service files.
- **Loki, Logstash, Elasticsearch**: Centralized log aggregation and search.
- **Prometheus & Grafana**: Metrics scraping and dashboard visualization.
- **Filebeat**: Ships logs to Elasticsearch.

See [Monitoring README](SkyBooker/Monitoring/README.md) for more details.

---

# Access Services
When all services are started, those would be the ULR's to access each service:

   - API Gateway: [http://localhost:8000](http://localhost:8000)
   - AuthService: [http://localhost:8003](http://localhost:8003)
   - BookingService: [http://localhost:8002](http://localhost:8002)
   - FlightService: [http://localhost:8001](http://localhost:8001)
   - MessagingService: [http://localhost:8004](http://localhost:8004)
   - Grafana: [http://localhost:3000](http://localhost:3000)
   - Prometheus: [http://localhost:9090](http://localhost:9090)
   - RabbitMQ: [http://localhost:15672](http://localhost:15672)
   - Elasticsearch: [http://localhost:9200](http://localhost:9200)

## API Examples

### Register User (via Gateway)
```http
POST http://localhost:8000/auth/register
Content-Type: application/json

{
  "username": "AdminV7",
  "eMail": "testuseradmin@example3.com",
  "password": "ABCDEFG-ab123456",
  "role": "Admin"
}
```

### Book a Flight (via Gateway)
```http
POST http://localhost:8000/booking
Authorization: Bearer {token}
Content-Type: application/json

{
  "flightId": "FL199",
  "passengerId": "P26",
  "passengerFirstname": "john",
  "passengerLastname": "Doe",
  "ticketCount": 2
}
```

### Send WhatsApp Notification (via RabbitMQ)
- BookingService sends a message to RabbitMQ.
- MessagingService consumes and sends WhatsApp notification via Twilio.

---

# 📄 Additional Resources
- [.Net](https://dotnet.microsoft.com/en-us/)
- [Microk8s](https://microk8s.io)
- [Docker Hub](https://www.docker.com)
- [Ocelot](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/implement-api-gateways-with-ocelot)
- [Mongo](https://www.mongodb.com)
- [SQLite](https://sqlite.org)
- [MariaDB](https://mariadb.org)
- [Twilio Whats App API](https://www.twilio.com/)
- [RabbitMQ](https://www.rabbitmq.com)
- [Grafna Dashboards](https://grafana.com)
- [Prometheus Metrics](https://prometheus.io)
- [ElasticSearch](https://www.elastic.co/)
- [GitHub](https://github.com)
---

# License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
