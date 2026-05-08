$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $projectRoot "backend.csproj"
$backendPort = 5000
$taxSyncContainers = @("taxsync-backend", "taxsync-frontend", "taxsync-mysql")

function Get-MySqlService {
    $service = Get-Service -Name "MySQL80", "MySQL", "MariaDB" -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if ($service) {
        return $service
    }

    return Get-Service -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -match "mysql|mariadb" -or $_.DisplayName -match "mysql|mariadb"
        } |
        Sort-Object Name |
        Select-Object -First 1
}

function Get-RunningTaxSyncContainers {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        return @()
    }

    try {
        return @(& docker ps --format "{{.Names}}" | Where-Object { $taxSyncContainers -contains $_ })
    }
    catch {
        return @()
    }
}

function Get-ProcessCommandLine([int]$processId) {
    $processInfo = Get-CimInstance Win32_Process -Filter "ProcessId = $processId" -ErrorAction SilentlyContinue
    return $processInfo.CommandLine
}

$runningContainers = @(Get-RunningTaxSyncContainers)
if ($runningContainers.Count -gt 0) {
    throw "TaxSync Docker containers are running ($($runningContainers -join ', ')). Run scripts\stop-docker.ps1 before starting local backend mode."
}

$mysqlService = Get-MySqlService
if ($null -ne $mysqlService) {
    if ($mysqlService.Status -ne 'Running') {
        Write-Host "Starting MySQL service '$($mysqlService.Name)'..."

        try {
            Start-Service -Name $mysqlService.Name -ErrorAction Stop
            $mysqlService.WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
        }
        catch {
            throw "Failed to start MySQL service '$($mysqlService.Name)'. Start MySQL manually on localhost:3306 and try again. $($_.Exception.Message)"
        }
    }

    Write-Host "MySQL service '$($mysqlService.Name)' is running."
}
else {
    Write-Warning "No Windows MySQL service was found. Ensure MySQL is already running on localhost:3306 before starting TaxSync backend."
}

$listeners = Get-NetTCPConnection -LocalPort $backendPort -State Listen -ErrorAction SilentlyContinue
foreach ($listener in $listeners) {
    $process = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
    $commandLine = Get-ProcessCommandLine $listener.OwningProcess
    $isTaxSyncBackend = $commandLine -like "*backend.csproj*" -or $commandLine -like "*backend.dll*"

    if ($isTaxSyncBackend) {
        Write-Host "Stopping existing TaxSync backend process $($listener.OwningProcess)..."
        Stop-Process -Id $listener.OwningProcess -Force
        continue
    }

    $processName = if ($process) { $process.ProcessName } else { "unknown" }
    throw "Port $backendPort is already in use by process $($listener.OwningProcess) ($processName). Stop it before starting TaxSync backend."
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://0.0.0.0:$backendPort"
$env:TaxSync__RuntimeMode = "Local"

Write-Host "Starting TaxSync backend on http://localhost:$backendPort (also reachable via http://127.0.0.1:$backendPort)"
Push-Location $projectRoot
try {
    dotnet restore $projectPath
    dotnet run --no-launch-profile --project $projectPath
}
finally {
    Pop-Location
}