$services = @(
    "src\Services\IdentityService",
    "src\Services\InventoryService",
    "src\Services\OrderService",
    "src\Services\TransactionService",
    "src\ApiGateway"
)

Write-Host "Starting Cafe Marahuyo Services..."

foreach ($service in $services) {
    Write-Host "Starting $service..."
    Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory $service -WindowStyle Minimized
}

Write-Host "All services started!"
Write-Host "You can access the API Gateway at http://localhost:5100"
