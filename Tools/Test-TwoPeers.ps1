param([int]$PortA = 47870, [int]$PortB = 47871)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$exe = Join-Path $project 'Builds\Windows\LookUpOn.exe'
$reports = Join-Path $project 'Builds\Smoke'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Build the Windows prototype from the LookUpOn menu in Unity first.' }
if ($PortA -eq $PortB -or $PortA -lt 1024 -or $PortB -lt 1024 -or $PortA -gt 65535 -or $PortB -gt 65535) { throw 'Choose two different valid UDP ports above 1023.' }
New-Item -ItemType Directory -Path $reports -Force | Out-Null
$run = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$reportA = Join-Path $reports "$run-peer-a.json"
$reportB = Join-Path $reports "$run-peer-b.json"
$logA = Join-Path $reports "$run-peer-a.log"
$logB = Join-Path $reports "$run-peer-b.log"
$capture = Join-Path $reports "$run-preview.png"
$argsA = @('--smoke', '--expect-peer', '--place-beacon', '--port', $PortA, '--connect', '127.0.0.1', '--remote-port', $PortB, '--report', ('"' + $reportA + '"'), '-logFile', ('"' + $logA + '"'), '-screen-width', '1280', '-screen-height', '800', '-screen-fullscreen', '0')
$argsB = @('--smoke', '--expect-peer', '--port', $PortB, '--connect', '127.0.0.1', '--remote-port', $PortA, '--report', ('"' + $reportB + '"'), '--capture', ('"' + $capture + '"'), '-logFile', ('"' + $logB + '"'), '-screen-width', '1280', '-screen-height', '800', '-screen-fullscreen', '0')
$a = $null
$b = $null
try {
    $a = Start-Process -FilePath $exe -ArgumentList $argsA -WindowStyle Hidden -PassThru
    $b = Start-Process -FilePath $exe -ArgumentList $argsB -WindowStyle Hidden -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(50)
    while ([DateTime]::UtcNow -lt $deadline) {
        $a.Refresh(); $b.Refresh()
        if ((Test-Path -LiteralPath $reportA) -and (Test-Path -LiteralPath $reportB)) { break }
        if ($a.HasExited -and $b.HasExited) { break }
        Start-Sleep -Milliseconds 250
    }
    foreach ($report in @($reportA, $reportB)) {
        if (-not (Test-Path -LiteralPath $report)) { throw "Missing report: $report. Inspect the corresponding player log." }
        $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        if (-not $result.passed) { throw "Smoke test failed: $report" }
        Write-Output $result
    }
    foreach ($log in @($logA, $logB)) {
        if (Select-String -LiteralPath $log -Pattern 'Exception:|error CS|Shader error' -Quiet) { throw "Runtime errors found in $log" }
    }
    $shutdownDeadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        $a.Refresh(); $b.Refresh()
        if ($a.HasExited -and $b.HasExited) { break }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $shutdownDeadline)
    if (-not $a.HasExited -or -not $b.HasExited) {
        Write-Warning 'Functional synchronization passed, but a Unity test process did not exit promptly. The test runner will terminate only its own test processes.'
    }
    Write-Output "PASS: terrain loading and two-peer beacon synchronization. Preview: $capture"
} finally {
    # Stop only the test processes started above if they failed to exit.
    foreach ($process in @($a, $b)) { if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id } }
}
