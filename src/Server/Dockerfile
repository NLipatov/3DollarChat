FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY ./ ./
ENTRYPOINT ["dotnet", "Ethachat.Server.dll"]