#!/bin/bash

if [ $# -ne 1 ]; then
    echo "Usage: $0 <folder that contains appsettings.json and FCMConfiguration.json> $1 <folder that contains a dev certificates>"
    exit 1
fi

CONFIGURATION_PATH=$1
DEV_CERTIFICATE_PATH=$2

if [ ! -f $CONFIGURATION_PATH/appsettings.json ]; then
    echo "Error: appsettings.json not found in the provided configuration folder."
    exit 1
fi

if [ ! -f $CONFIGURATION_PATH/FCMConfiguration.json ]; then
    echo "Error: FCMConfiguration.json not found in the provided configuration folder."
    exit 1
fi

if [ ! -f $DEV_CERTIFICATE_PATH/localhost.pfx ]; then
    echo "Error: localhost.pfx not found in given dev certificate folder."
    exit 1
fi

mkdir -p distro

rm -f ./distro/appsettings.Development.json
rm -f ./distro/appsettings.json

# Replace configuration file in distro folder
cp $CONFIGURATION_PATH/appsettings.json ./distro/appsettings.json
cp $CONFIGURATION_PATH/FCMConfiguration.json ./distro/FCMConfiguration.json
cp /root/ethachat/devcerts/localhost.pfx ./distro/localhost.pfx

dotnet publish -r linux-x64 -o distro

cat <<EOL > distro/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY ./ ./
ENTRYPOINT ["dotnet", "Limp.Server.dll"]
EOL

docker build -t wasm-chat ./distro