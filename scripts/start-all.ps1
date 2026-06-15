$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\.."

Write-Host "Starting API Gateway..." -ForegroundColor Cyan
Start-Process dotnet -ArgumentList "run --project src\ApiGateway\ApiGateway.csproj" -WorkingDirectory $root

Write-Host "Starting IdentityService..." -ForegroundColor Yellow
Start-Process dotnet -ArgumentList "run --project src\Services\IdentityService\IdentityService.csproj" -WorkingDirectory $root

Write-Host "Starting InventoryService..." -ForegroundColor Green
Start-Process dotnet -ArgumentList "run --project src\Services\InventoryService\InventoryService.csproj" -WorkingDirectory $root

Write-Host "Starting TransactionService..." -ForegroundColor Magenta
Start-Process dotnet -ArgumentList "run --project src\Services\TransactionService\TransactionService.csproj" -WorkingDirectory $root

Write-Host "Starting DashboardService..." -ForegroundColor Blue
Start-Process dotnet -ArgumentList "run --project src\Services\DashboardService\DashboardService.csproj" -WorkingDirectory $root

Write-Host "Starting OrderService..." -ForegroundColor DarkCyan
Start-Process dotnet -ArgumentList "run --project src\Services\OrderService\OrderService.csproj" -WorkingDirectory $root

Write-Host "All services started!" -ForegroundColor White
Write-Host "API Gateway is running at: http://localhost:5000"
Write-Host "Press any key to close..."
$Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
