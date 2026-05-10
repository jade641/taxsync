$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $projectRoot "backend.csproj"
$developmentSettingsPath = Join-Path $projectRoot "appsettings.Development.json"
$backendPort = 5000
$taxSyncContainers = @("taxsync-backend", "taxsync-frontend", "taxsync-mysql")
$developmentSettings = $null

if (Test-Path $developmentSettingsPath) {
    $developmentSettings = Get-Content $developmentSettingsPath -Raw | ConvertFrom-Json
}

$databaseProvider = if ([string]::IsNullOrWhiteSpace($env:Database__Provider)) {
    if ($null -ne $developmentSettings -and $null -ne $developmentSettings.Database -and -not [string]::IsNullOrWhiteSpace($developmentSettings.Database.Provider)) {
        [string]$developmentSettings.Database.Provider
    }
    else {
        "MySql"
    }
}
else {
    $env:Database__Provider.Trim()
}

$connectionString = if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
    if ($null -ne $developmentSettings -and $null -ne $developmentSettings.ConnectionStrings -and -not [string]::IsNullOrWhiteSpace($developmentSettings.ConnectionStrings.DefaultConnection)) {
        [string]$developmentSettings.ConnectionStrings.DefaultConnection
    }
    else {
        ""
    }
}
else {
    $env:ConnectionStrings__DefaultConnection.Trim()
}

function Test-TcpPort {
    param(
        [string]$HostName,
        [int]$Port,
        [int]$TimeoutMs = 1500
    )

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $asyncResult = $client.BeginConnect($HostName, $Port, $null, $null)
        if (-not $asyncResult.AsyncWaitHandle.WaitOne($TimeoutMs, $false)) {
            return $false
        }

        $client.EndConnect($asyncResult)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Get-ConnectionStringSettings([string]$ConnectionString) {
    $settings = @{}

    foreach ($segment in ($ConnectionString -split ';')) {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            continue
        }

        $parts = $segment -split '=', 2
        if ($parts.Count -ne 2) {
            continue
        }

        $settings[$parts[0].Trim().ToLowerInvariant()] = $parts[1].Trim()
    }

    return $settings
}

function Get-ConnectionEndpoint([string]$ConnectionString) {
    $settings = Get-ConnectionStringSettings $ConnectionString
    $hostName = if ($settings.ContainsKey('server')) { $settings['server'] } elseif ($settings.ContainsKey('host')) { $settings['host'] } elseif ($settings.ContainsKey('data source')) { $settings['data source'] } else { '' }
    $portValue = if ($settings.ContainsKey('port')) { $settings['port'] } else { '3306' }
    $port = 3306
    [void][int]::TryParse($portValue, [ref]$port)

    return @{
        Host = $hostName
        Port = $port
        Database = if ($settings.ContainsKey('database')) { $settings['database'] } elseif ($settings.ContainsKey('initial catalog')) { $settings['initial catalog'] } else { '' }
    }
}

function Test-PlaceholderConnectionString([string]$ConnectionString) {
    return $ConnectionString -match '(?i)(pwd|password)\s*=\s*(YOUR_|\[INSERT_MY_PASSWORD_HERE\]|<monsterasp-password>|<password>)'
}

function Test-LoopbackHost([string]$HostName) {
    return $HostName.Equals('localhost', [System.StringComparison]::OrdinalIgnoreCase) -or
        $HostName.Equals('127.0.0.1', [System.StringComparison]::OrdinalIgnoreCase) -or
        $HostName.Equals('::1', [System.StringComparison]::OrdinalIgnoreCase)
}

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

if ($databaseProvider.Equals("MySql", [System.StringComparison]::OrdinalIgnoreCase)) {
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        throw "Database provider is MySql but no connection string is configured. Set ConnectionStrings__DefaultConnection or update backend/appsettings.Development.json."
    }

    if (Test-PlaceholderConnectionString $connectionString) {
        throw "Database provider is MySql but the connection string still contains a placeholder password. Replace it with your real MySQL or MonsterASP database password first."
    }

    $databaseEndpoint = Get-ConnectionEndpoint $connectionString
    if ([string]::IsNullOrWhiteSpace($databaseEndpoint.Host)) {
        throw "Database provider is MySql but the connection string does not contain a database host."
    }

    if (Test-LoopbackHost $databaseEndpoint.Host) {
        $mysqlService = Get-MySqlService
        if ($null -ne $mysqlService) {
            if ($mysqlService.Status -ne 'Running') {
                Write-Host "Starting MySQL service '$($mysqlService.Name)'..."

                try {
                    Start-Service -Name $mysqlService.Name -ErrorAction Stop
                    $mysqlService.WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
                }
                catch {
                    throw "Failed to start MySQL service '$($mysqlService.Name)'. Start MySQL manually on $($databaseEndpoint.Host):$($databaseEndpoint.Port) and try again. $($_.Exception.Message)"
                }
            }

            Write-Host "MySQL service '$($mysqlService.Name)' is running."
        }
    }

    if (-not (Test-TcpPort -HostName $databaseEndpoint.Host -Port $databaseEndpoint.Port)) {
        if (Test-LoopbackHost $databaseEndpoint.Host) {
            throw "Database provider is MySql but nothing is reachable at $($databaseEndpoint.Host):$($databaseEndpoint.Port). Update the connection string or start the database first."
        }

        Write-Warning "MySQL endpoint $($databaseEndpoint.Host):$($databaseEndpoint.Port) is not reachable from this machine. Starting the API anyway; /api/health will report degraded until the database is reachable from the running environment."
    }
    else {
        Write-Host "MySQL is reachable on $($databaseEndpoint.Host):$($databaseEndpoint.Port) for database '$($databaseEndpoint.Database)'."
    }
}
else {
    Write-Host "Development database provider: $databaseProvider. Local startup will not require MySQL."
}

$listeners = Get-NetTCPConnection -LocalPort $backendPort -State Listen -ErrorAction SilentlyContinue
foreach ($listener in $listeners) {
    $process = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
    $commandLine = Get-ProcessCommandLine $listener.OwningProcess
    $normalizedCommandLine = if ($commandLine) { $commandLine.ToLowerInvariant() } else { "" }
    $expectedBackendExe = (Join-Path $projectRoot "bin\Debug\net10.0\backend.exe").ToLowerInvariant()
    $isTaxSyncBackend = $normalizedCommandLine.Contains("backend.csproj") -or
        $normalizedCommandLine.Contains("backend.dll") -or
        $normalizedCommandLine.Contains($expectedBackendExe)

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