$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$venvPython = Join-Path $root "venv\Scripts\python.exe"

if (Test-Path $venvPython) {
  $py = $venvPython
} else {
  $py = "python"
}

Write-Host "Using python: $py"

& $py -m uvicorn serve:app --reload --host 127.0.0.1 --port 8000
