# Optional: Build Docker images for each microservice (this can be skipped if docker-compose builds them automatically)
docker build -t flightservice ./FlightService
docker build -t bookingservice ./BookingService
docker build -t authservice ./AuthService
docker build -t ocelotapigateway ./OcelotApiGateway
docker build -t messagingservice ./MessagingService

# Run the Docker cluster using docker-compose and force a rebuild of images
docker compose up --build -d

# Connect to the MongoDB container and list databases
docker exec mymongo mongosh -u mongoadmin -p secret --eval "show dbs"

# To stop and remove the containers, uncomment the following line:
# docker compose down
