@echo off
echo Starting Cafe Marahuyo Services...

start "Identity Service" cmd /k "cd src\Services\IdentityService && dotnet run"
start "Inventory Service" cmd /k "cd src\Services\InventoryService && dotnet run"
start "Order Service" cmd /k "cd src\Services\OrderService && dotnet run"
start "Transaction Service" cmd /k "cd src\Services\TransactionService && dotnet run"
start "API Gateway" cmd /k "cd src\ApiGateway && dotnet run"

echo All services are starting up in separate windows!
echo API Gateway will be at http://localhost:5100
pause
