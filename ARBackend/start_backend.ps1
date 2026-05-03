$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$pythonVersion = & python -c "import sys; print(f'Python{sys.version_info.major}{sys.version_info.minor}')"
$userSite = Join-Path $env:APPDATA "Python\$pythonVersion\site-packages"

if (Test-Path $userSite) {
    if ([string]::IsNullOrWhiteSpace($env:PYTHONPATH)) {
        $env:PYTHONPATH = $userSite
    }
    elseif (-not ($env:PYTHONPATH -split ';' | Where-Object { $_ -eq $userSite })) {
        $env:PYTHONPATH = "$userSite;$env:PYTHONPATH"
    }
}

python -m uvicorn main:app --host 0.0.0.0 --port 8000 --reload
