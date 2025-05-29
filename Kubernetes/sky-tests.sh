#!/usr/bin/env bash

microk8s kubectl get pods -n skybooker

echo "starting tests \n"

url="http://localhost:8081"
pwd="ABCDEFG-ab123456"

# Test 1: create a new user
# ask for username
echo "Please enter a username:"
read username
tokenResponse=$(curl -s -X POST "$url/auth/register" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "{
    \"username\": \"$username\",
    \"eMail\": \"kubeuser1@example.com\",
    \"password\": \"$pwd\",
    \"role\": \"Admin\"
  }" | tee response.json)
echo "\n response: $tokenResponse"
# how to convert tokenResponse to a variable
token=$(echo $tokenResponse | jq -r '.token')
echo "token: $token"

# Test 2: login with the new user
#echo "Login"
#curl -X POST $url/auth/login \
#  -H "Content-Type: application/json" \
#  -H "Accept: application/json" \
#  -d '{
#    "username": $username,
#    "password": $pwd
#}'

# Test 3: get flights
echo "get flights"
#curl -X GET "$url/flight" \
#  -H "Content-Type: application/json" \
#  -H "Accept: application/json" \
#  -H "Authorization: Bearer $token"

curl -X POST "$url/flight/create" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Authorization: Bearer $token" \
  -d '{
    "flightId": "FL111",
    "airlineName": "Sky Airlines",
    "source": "New York",
    "destination": "Los Angeles",
    "departure_Time": "2025-10-22T10:00:00",
    "arrival_Time": "2025-11-22T14:00:00",
    "available_Seats": 150,
    "created_At": "2025-03-21T12:00:00",
    "updated_At": "2025-03-21T12:00:00"
  }'

curl -X GET "$url/flight" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Authorization: Bearer $token"



# Test 4: get bookings
echo "get bookings"
curl -X GET "$url/flight" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Authorization: Bearer $token"
