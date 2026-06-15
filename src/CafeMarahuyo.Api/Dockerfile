FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj files
COPY src/CafeMarahuyo.Shared/*.csproj ./src/CafeMarahuyo.Shared/
COPY src/CafeMarahuyo.Api/*.csproj ./src/CafeMarahuyo.Api/

# Restore dependencies
RUN dotnet restore src/CafeMarahuyo.Api/CafeMarahuyo.Api.csproj

# Copy everything else
COPY src/CafeMarahuyo.Shared/ ./src/CafeMarahuyo.Shared/
COPY src/CafeMarahuyo.Api/ ./src/CafeMarahuyo.Api/

# Build and publish
WORKDIR /app/src/CafeMarahuyo.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# The app binds dynamically to PORT using Environment.GetEnvironmentVariable("PORT")
ENTRYPOINT ["dotnet", "CafeMarahuyo.Api.dll"]
