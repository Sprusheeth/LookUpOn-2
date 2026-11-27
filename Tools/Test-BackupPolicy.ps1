param([string]$AdminUrl = 'http://127.0.0.1:8765', [int]$InspectSeconds = 0)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$exe = Join-Path $project 'Builds\Windows\LookUpOn.exe'
$reports = Join-Path $project 'Builds\BackupPolicy'
New-Item -ItemType Directory -Path $reports -Force | Out-Null
$run = [Guid]::NewGuid().ToString('N').Substring(0,8)
$processes = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
$results = @()
function Wait-Check([scriptblock]$Check, [string]$Message, [int]$Seconds = 25) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    do { $value = & $Check; if ($value) { return $value }; Start-Sleep -Milliseconds 500 } while ([DateTime]::UtcNow -lt $deadline)
    throw "Timed out: $Message"
}
function Get-Sessions([int]$WorldSeed) { @((Invoke-RestMethod "$AdminUrl/api/state").sessions | Where-Object { $_.seed -eq $WorldSeed -and $_.snapshotAgeSeconds -lt 4 }) }
function Set-Backups([int]$WorldSeed, [bool]$Enabled) {
    Invoke-RestMethod "$AdminUrl/api/control" -Method Post -ContentType 'application/json' -Headers @{Origin=$AdminUrl;'X-Lookup-Control'='1'} -Body (@{seed=$WorldSeed;backupsEnabled=$Enabled}|ConvertTo-Json)
}
function Start-Peer([int]$WorldSeed,[int]$Port,[int]$Remote,[string]$Label) {
    $log = Join-Path $reports "$run-$Label.log"
    $peerProcess = Start-Process -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList @('--seed',$WorldSeed,'--port',$Port,'--connect','127.0.0.1','--remote-port',$Remote,'-logFile',('"'+$log+'"'))
    $processes.Add($peerProcess); return $peerProcess
}
try {
    foreach ($enabled in @($false,$true)) {
        # Isolated test worlds do not change the user's default world or its policy.
        $worldSeed = if ($enabled) { 741902 } else { 741901 }
        $policy = Set-Backups $worldSeed $enabled
        $a = Start-Peer $worldSeed 47880 47881 ("a-"+$enabled)
        $b = Start-Peer $worldSeed 47881 47880 ("b-"+$enabled)
        $sessions = Wait-Check {
            $s = Get-Sessions $worldSeed
            if ($s.Count -eq 2 -and @($s | Where-Object { $_.peers.Count -eq 2 -and $_.backupsEnabled -eq $enabled -and $_.backupRevision -ge $policy.revision }).Count -eq 2) { ,$s }
        } 'both peers applying policy and connecting'
        $first = @($sessions | Where-Object { $_.udpPort -eq 47880 })[0]
        $other = @($sessions | Where-Object { $_.udpPort -eq 47881 })[0]
        if ($enabled) {
            Wait-Check { $s=Get-Sessions $worldSeed; if (@($s | Where-Object { $_.peers[0].seedCopies -eq 42 }).Count -eq 2) { $true } } 'seed replicas reaching both peers' | Out-Null
        }
        # Evidence for the admin inventory: inspect actual holder manifests, not ring assignments.
        $sessions = Wait-Check {
            $s = Get-Sessions $worldSeed
            if ($s.Count -ne 2) { return $null }
            foreach ($observer in $s) {
                foreach ($peer in $observer.peers) {
                    $held = @($observer.regions | Where-Object { $peer.id -in $_.holders })
                    $replicas = @($held | Where-Object { $_.owners[0] -ne $peer.id })
                    if ($enabled -and ($held.Count -ne 42 -or $replicas.Count -eq 0)) { return $null }
                    if (-not $enabled -and ($replicas.Count -ne 0 -or $held.Count -ne $peer.seedCopies)) { return $null }
                }
            }
            ,$s
        } 'both observers reporting correct primary and backup inventories'
        $inventoryFile = Join-Path $reports "$run-inventory-$enabled.json"
        $sessions | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $inventoryFile
        Write-Output "Verified inventories: backups=$enabled; seed=$worldSeed; peers=$($sessions.peerId -join ','); snapshot=$inventoryFile"
        if ($enabled -and $InspectSeconds -gt 0) { Start-Sleep -Seconds ([Math]::Min($InspectSeconds,60)) }
        $first = @($sessions | Where-Object { $_.udpPort -eq 47880 })[0]
        $other = @($sessions | Where-Object { $_.udpPort -eq 47881 })[0]
        $expectedMissing = @($first.regions | Where-Object { $_.owners[0] -eq $other.peerId }).Count
        if ($expectedMissing -eq 0) { throw 'Test peer owns no regions.' }
        $b.Kill(); $b.WaitForExit(5000) | Out-Null
        $survivor = Wait-Check {
            $s = @(Get-Sessions $worldSeed | Where-Object { $_.peerId -eq $first.peerId })
            if ($s.Count -eq 1 -and $s[0].peers.Count -eq 1 -and @($s[0].regions | Where-Object { -not $_.available }).Count -eq $(if($enabled){0}else{$expectedMissing})) { $s[0] }
        } 'owner loss reaching expected terrain availability' 25
        $missing = @($survivor.regions | Where-Object { -not $_.available }).Count
        $results += [pscustomobject]@{backups=$enabled;passed=$true;departedOwnerRegions=$expectedMissing;unavailableAfterFailure=$missing;loadedTiles=$survivor.loadedChunks;saveStatus=$survivor.saveStatus}
        $a.Kill(); $a.WaitForExit(5000) | Out-Null
    }
    $file = Join-Path $reports "$run-results.json"
    $results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $file
    $results | Format-Table
    Write-Output "PASS: live admin policy controls actual game region availability. Report: $file"
} finally {
    foreach($p in $processes) { $p.Refresh(); if(-not $p.HasExited) { $p.Kill() } }
}
