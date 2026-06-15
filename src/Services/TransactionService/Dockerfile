FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj files
COPY CafeMarahuyo.Shared/*.csproj ./CafeMarahuyo.Shared/
COPY Services/TransactionService/*.csproj ./Services/TransactionService/

# Restore dependencies
RUN dotnet restore Services/TransactionService/TransactionService.csproj

# Copy everything else
COPY CafeMarahuyo.Shared/ ./CafeMarahuyo.Shared/
COPY Services/TransactionService/ ./Services/TransactionService/

# Build and publish
WORKDIR /app/Services/TransactionService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransactionService.dll"]
