FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj files
COPY CafeMarahuyo.Shared/*.csproj ./CafeMarahuyo.Shared/
COPY Services/IdentityService/*.csproj ./Services/IdentityService/

# Restore dependencies
RUN dotnet restore Services/IdentityService/IdentityService.csproj

# Copy everything else
COPY CafeMarahuyo.Shared/ ./CafeMarahuyo.Shared/
COPY Services/IdentityService/ ./Services/IdentityService/

# Build and publish
WORKDIR /app/Services/IdentityService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
