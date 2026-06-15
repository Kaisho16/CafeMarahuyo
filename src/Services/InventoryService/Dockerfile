FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj files
COPY src/CafeMarahuyo.Shared/*.csproj ./src/CafeMarahuyo.Shared/
COPY src/Services/InventoryService/*.csproj ./src/Services/InventoryService/

# Restore dependencies
RUN dotnet restore src/Services/InventoryService/InventoryService.csproj

# Copy everything else
COPY src/CafeMarahuyo.Shared/ ./src/CafeMarahuyo.Shared/
COPY src/Services/InventoryService/ ./src/Services/InventoryService/

# Build and publish
WORKDIR /app/src/Services/InventoryService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.dll"]
