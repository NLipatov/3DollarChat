#!/bin/bash

# Step 1: Pull the latest changes from Git
echo "INFO: Step 1: Pull the latest changes from Git"
git pull

# Step 2: Update and initialize submodules
echo "INFO: Step 2: Update and initialize submodules"
git submodule update --init

# Step 3: If the 'distro' folder exists, delete it
echo "INFO: Step 3: If the 'distro' folder exists, delete it"
if [ -d "distro" ]; then
    rm -rf distro
fi

# Step 4: Stop and remove any existing container with the same image
echo "INFO: Step 4: Stop and remove any existing container with the same image"
EXISTING_CONTAINER=$(docker ps -q -f ancestor=wasm-chat)
if [ "$EXISTING_CONTAINER" ]; then
    docker stop "$EXISTING_CONTAINER"
    docker rm "$EXISTING_CONTAINER"
fi

# Step 5: Remove the old 'auth-api' Docker image
echo "INFO: Step 5: Remove the old 'wasm-chat' Docker image"
EXISTING_IMAGE=$(docker images -q wasm-chat)
if [ "$EXISTING_IMAGE" ]; then
    docker rmi "$EXISTING_IMAGE"
fi

# Step 6: Publish the .NET app to 'distro' folder
echo "INFO: Step 7: Publish the .NET app to 'distro' folder"
dotnet publish -r linux-x64 -o distro

# Step 7: Create a Dockerfile in 'distro' folder
echo "INFO: Step 8: Create a Dockerfile in 'distro' folder"
cat <<EOL > distro/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY ./ ./
ENTRYPOINT ["dotnet", "Limp.Server.dll"]
EOL

# Step 8: Build the Docker image 'wasm-chat'
echo "INFO: Step 9: Build the Docker image 'wasm-chat'"
docker build -t wasm-chat distro

# Step 9: Run the Docker container with the new image and restart on failure
echo "INFO: Step 10: Run the Docker container with the new image and restart on failure"
docker run -d --restart=always --network etha-chat --name wasm-chat -p 1010:443 -p 1011:80 -e ASPNETCORE_URLS="https://+;http://+" -e ASPNETCORE_HTTPS_PORT=1010 -e ASPNETCORE_Kestrel__Certificates__Default__Password="YourSecurePassword" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/localhost.pfx -v /root/devcert:/https/ wasm-chat

# Step 10: Remove the 'distro' folder after deployment
echo "INFO: Step 11: Remove the 'distro' folder after deployment"
rm -rf distro