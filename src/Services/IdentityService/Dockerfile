FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj files
COPY src/CafeMarahuyo.Shared/*.csproj ./src/CafeMarahuyo.Shared/
COPY src/Services/IdentityService/*.csproj ./src/Services/IdentityService/

# Restore dependencies
RUN dotnet restore src/Services/IdentityService/IdentityService.csproj

# Copy everything else
COPY src/CafeMarahuyo.Shared/ ./src/CafeMarahuyo.Shared/
COPY src/Services/IdentityService/ ./src/Services/IdentityService/

# Build and publish
WORKDIR /app/src/Services/IdentityService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
