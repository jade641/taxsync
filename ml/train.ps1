param(
  [Parameter(Mandatory=$true)][string]$Target,
  [string]$Data = "data/property_tax.csv",
  [string]$ModelOut = "artifacts/model.joblib"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$venvPython = Join-Path $root "venv\Scripts\python.exe"

if (Test-Path $venvPython) {
  $py = $venvPython
} else {
  $py = "python"
}

Write-Host "Using python: $py"
& $py (Join-Path $root "train.py") --data (Join-Path $root $Data) --target $Target --model-out (Join-Path $root $ModelOut)
