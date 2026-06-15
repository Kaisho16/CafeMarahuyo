FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj files
COPY src/ApiGateway/*.csproj ./src/ApiGateway/

# Restore dependencies
RUN dotnet restore src/ApiGateway/ApiGateway.csproj

# Copy everything else
COPY src/ApiGateway/ ./src/ApiGateway/

# Build and publish
WORKDIR /app/src/ApiGateway
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
