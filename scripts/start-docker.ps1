$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$backendPort = 5000

function Get-RunningTaxSyncContainers {
    try {
        return @(& docker ps --format "{{.Names}}" | Where-Object { $_ -in @("taxsync-backend", "taxsync-frontend", "taxsync-mysql") })
    }
    catch {
        return @()
    }
}

function Invoke-TaxSyncCompose {
    param([string[]]$Arguments)

    $composeV2Available = $false
    try {
        & docker compose version *> $null
        $composeV2Available = $LASTEXITCODE -eq 0
    }
    catch {
        $composeV2Available = $false
    }

    if ($composeV2Available) {
        & docker compose @Arguments
        return
    }

    if (Get-Command docker-compose -ErrorAction SilentlyContinue) {
        & docker-compose @Arguments
        return
    }

    throw "Docker Compose is not available. Install Docker Desktop or enable the docker compose plugin."
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is not installed or not available on PATH."
}

$runningContainers = @(Get-RunningTaxSyncContainers)
$backendContainerRunning = $runningContainers -contains "taxsync-backend"

if (-not $backendContainerRunning) {
    $listeners = Get-NetTCPConnection -LocalPort $backendPort -State Listen -ErrorAction SilentlyContinue
    if ($listeners) {
        throw "Port $backendPort is already in use. Stop local backend mode before starting Docker mode."
    }
}

Push-Location $workspaceRoot
try {
    Invoke-TaxSyncCompose -Arguments @("up", "--build", "-d")
}
finally {
    Pop-Location
}

Write-Host "TaxSync Docker mode is starting."
Write-Host "Frontend: http://localhost:3000"
Write-Host "Backend:  http://localhost:5000"
Write-Host "MySQL host port: 3307"