$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot

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

    throw "Docker Compose is not available."
}

Push-Location $workspaceRoot
try {
    Invoke-TaxSyncCompose -Arguments @("down")
}
finally {
    Pop-Location
}