param([int]$Port = 8765)
$ErrorActionPreference = 'Stop'
if ($Port -lt 1024 -or $Port -gt 65535) { throw 'Choose a port between 1024 and 65535.' }
$node = (Get-Command node -ErrorAction Stop).Source
$script = Join-Path $PSScriptRoot 'server.mjs'
$url = "http://127.0.0.1:$Port"
$running = $false
try {
    $response = Invoke-RestMethod "$url/api/state" -TimeoutSec 2
    $running = $null -ne $response.sessions
} catch { }
if (-not $running) {
    $logRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'Builds\AdminLogs'
    New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
    $process = Start-Process -FilePath $node -ArgumentList @(('"' + $script + '"'), '--port', $Port) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logRoot 'server.log') -RedirectStandardError (Join-Path $logRoot 'server-error.log')
    $process.Id | Set-Content (Join-Path $logRoot 'server.pid')
    for ($i = 0; $i -lt 20; $i++) {
        try { $response = Invoke-RestMethod "$url/api/state" -TimeoutSec 1; $running = $null -ne $response.sessions; if ($running) { break } } catch { }
        Start-Sleep -Milliseconds 250
    }
    if (-not $running) { throw "Dashboard failed to start. Inspect $logRoot\server-error.log" }
}
Start-Process $url
Write-Output "LookUpOn dashboard: $url"
