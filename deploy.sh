#!/bin/bash

BRANCH="dev"

# Redis configuration
REDIS_CONTAINER_NAME="etha-chat-redis"
NETWORK_NAME="etha-chat"
REDIS_PORT="6379"

echo -e "\nSTEP 1: checking out to target branch and pulling the latest changes."

if [ $# -eq 1 ]; then
    BRANCH="$1"
else
    echo "INFO: No branch specified. Using the default branch '$BRANCH'."
fi

if ! git clean -xdf; then
  echo "ERROR: Failed to clean the untracked files present in a git working directory."
  exit 1
fi

if ! git reset --hard; then
  echo "ERROR: Failed to reset the Git branch. Aborting script."
  exit 1
fi

if ! git checkout "$BRANCH"; then
  echo "ERROR: Failed to checkout the $BRANCH. Aborting script."
  exit 1
fi

if ! git pull; then
  echo "ERROR: Failed to pull the latest changes from Git. Aborting script."
  exit 1
fi

if ! git submodule update --init; then
  echo "ERROR: Failed to update and initialize submodules. Aborting script."
  exit 1
fi

echo -e "\nSTEP 2: Removing previous build files."
if [ -d "distro" ]; then
    echo "INFO: Found a distro folder from the previous build. Deleting it."
    rm -rf distro
    echo "INFO: distro folder is deleted."
fi

echo -e "\nSTEP 3: Stop and remove any existing container with the same image."
EXISTING_CONTAINER=$(docker ps -q -f ancestor=wasm-chat)
if [ "$EXISTING_CONTAINER" ]; then
    if ! docker stop "$EXISTING_CONTAINER"; then
      echo "ERROR: Failed to stop existing container, that shares the same image - '$EXISTING_CONTAINER'."
      exit 1
    fi
    if ! docker rm "$EXISTING_CONTAINER"; then
      echo "ERROR: Failed to remove existing container - '$EXISTING_CONTAINER'."
      exit 1
    fi
fi

EXISTING_IMAGE=$(docker images -q wasm-chat)
if [ "$EXISTING_IMAGE" ]; then
    if ! docker rmi "$EXISTING_IMAGE"; then
      echo "ERROR: Failed to remove an old image - '$EXISTING_IMAGE'."
      exit 1
    fi
fi

echo -e "\nSTEP 4: Building and publishing project."
if ! dotnet publish -c Release -r linux-x64 -o distro; then
  echo "ERROR: Failed to build and publish the project."
  exit 1
fi

rm distro/appsettings.Development.json
if ! cp /root/EthaChat/Configuration/ChatApp/appsettings.json distro/appsettings.json; then
  echo "ERROR: Failed to copy appsettings.json."
  exit 1
fi

if ! cp /root/EthaChat/Configuration/ChatApp/FCMConfiguration.json distro/FCMConfiguration.json; then
  echo "ERROR: Failed to copy FCMConfiguration.json."
  exit 1
fi

# Grabbing a password from distro/appsettings.json
PASSWORD=$(cat distro/appsettings.json | jq -r '.Redis.Password')

if [ -z "$EXISTING_REDIS" ]; then
    echo "No existing Redis container found on port $REDIS_PORT in network $NETWORK_NAME. Starting a new Redis container..."
    # Starting a Redis container
    docker run --restart always --network $NETWORK_NAME --name $REDIS_CONTAINER_NAME -p $REDIS_PORT:6379 -d redis redis-server --save 60 1 --loglevel warning --requirepass "$PASSWORD"
else
    echo "Redis container already exists on port $REDIS_PORT in network $NETWORK_NAME. Skipping Redis container creation."
fi

echo -e "\nSTEP 5: Create a Dockerfile in 'distro' folder."
cat <<EOL > distro/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY ./ ./
ENTRYPOINT ["dotnet", "Limp.Server.dll"]
EOL

echo -e "\nSTEP 6: Build the Docker image 'wasm-chat'."
if ! docker build -t wasm-chat distro; then 
  echo "ERROR: Failed to build the Docker image."
  exit 1
fi

echo -e "\nSTEP 7: Run the Docker container with the new image and restart on failure."
if ! docker run -d --restart=always --network etha-chat --name wasm-chat -p 1010:443 -p 1011:80 -e ASPNETCORE_URLS="https://+;http://+" -e ASPNETCORE_HTTPS_PORT=1010 -e ASPNETCORE_Kestrel__Certificates__Default__Password="YourSecurePassword" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/localhost.pfx -v /root/devcert:/https/ wasm-chat; then
  echo "ERROR: Failed to Run the Docker container."
  exit 1
fi

echo -e "\nSTEP 8: Making a deploy.sh executable."
if ! chmod +x deploy.sh; then
    echo "ERROR: Failed to make deploy.sh executable."
    exit 1
fi

echo "Removing unused Docker resources."
docker system prune -f