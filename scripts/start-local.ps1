$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$backendScript = Join-Path $workspaceRoot "backend\run-dev.ps1"
$frontendScript = Join-Path $workspaceRoot "frontend\run-dev.ps1"
$backendHealthUrl = "http://localhost:5000/api/health"

function Wait-ForBackendHealth {
    param([int]$Retries = 90)

    for ($attempt = 1; $attempt -le $Retries; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $backendHealthUrl -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                Write-Host "Backend is healthy at $backendHealthUrl"
                return
            }
        }
        catch {
            if ($attempt -eq $Retries) {
                throw "Backend did not become healthy at $backendHealthUrl. Check the backend terminal for database or port errors."
            }
        }

        Start-Sleep -Seconds 1
    }
}

Write-Host "Opening TaxSync backend terminal..."
Start-Process powershell.exe -ArgumentList @("-NoExit", "-ExecutionPolicy", "Bypass", "-File", $backendScript) | Out-Null

Wait-ForBackendHealth

Write-Host "Opening TaxSync frontend terminal..."
Start-Process powershell.exe -ArgumentList @("-NoExit", "-ExecutionPolicy", "Bypass", "-File", $frontendScript) | Out-Null

Write-Host "TaxSync local mode is starting."
Write-Host "Backend:  http://localhost:5000"
Write-Host "Frontend: http://127.0.0.1:5173"