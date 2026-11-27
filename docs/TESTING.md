# Testing and reproduction

Use the project root as the working directory. The recipe 03 terrain build was validated on 13 November 2026. Recorded two-process and backup inventory trials below describe recipe 02.

## 1. Establish the environment

Record Windows version, CPU/GPU/RAM, Unity editor version, build hash, Node version, source hashes, network topology, seed, peer IDs, and policy revision for a new experimental run. The existing reports do not contain a complete hardware inventory, latency trace, or frame-time distribution; do not infer those fields.

The pinned editor is 6000.5.2f1, with recipe 03 and protocol 04. The provided build is Windows/Mono development. Node scripts use only built-in modules. Avoid comparing unlike builds, recipes, seeds, or network conditions.

## 2. Unity validation

Open the project, wait for compilation, and run **LookUpOn > Validate prototype**. The current `Builds/validation.txt` reports **521 passing assertions**, including river and track elevation, tile coverage, seam matching, mixed LOD boundaries, ring behavior, UDP synchronization, live policy ordering and propagation, owner loss, and saved missing-owner restoration.

See `Assets/Editor/ProjectSetup.cs` for the checks. **Build Windows prototype** recreates the generated scene, validates, and builds. Preserve custom scene work before invoking it. Recipe 03 also passed an Editor Play mode check for grounded spawn, supported terrain and scenery shaders, river rendering, and 121 loaded tiles. Windows Application Control blocked the standalone smoke runner on the development machine.

## 3. Admin and simulation checks

```powershell
node --test .\AdminPanel\server.test.mjs .\AdminPanel\simulation.test.mjs
```

Expected **10 tests** cover telemetry freshness, same-origin controls, per-run peer commands, revision ordering, backup failure cases, hash-ring ordering, and holder evidence.

These tests exercise a deterministic model and localhost service. They do not validate real Unity performance, disk durability, or a WAN deployment.

## 4. Actual two-process smoke test

Build first, then:

```powershell
& .\Tools\Test-TwoPeers.ps1
```

The runner starts two hidden Windows game instances on 47870/47871. Smoke mode disables persistent saves, explicitly connects the clients, places a beacon, checks their reports, inspects logs, and cleans up its children. A shutdown cleanup warning is separate from a functional synchronization result.

Recorded reports `Builds/Smoke/b787309b-peer-a.json` and `b787309b-peer-b.json` both show:

| Measure | Client A | Client B |
| --- | ---: | ---: |
| Passed / above ground | true / true | true / true |
| Loaded tiles | 121 | 121 |
| Known remote peers | 1 | 1 |
| Beacons | 1 | 1 |
| Received packets | 123 | 126 |
| Player feet Y, m | 95.150856 | 95.150856 |
| Terrain hit Y, m | 95.070831 | 95.070831 |

Grounding uses a terrain-layer raycast, not a raycast that can hit the player's own collider. These packet counts are observed totals, not a throughput/latency benchmark.

## 5. Actual backup inventory and failure trial

Start the admin service in a separate terminal:

```powershell
node .\AdminPanel\server.mjs
```

Then run:

```powershell
& .\Tools\Test-BackupPolicy.ps1
# Optional inspection window in the connected ON case:
& .\Tools\Test-BackupPolicy.ps1 -InspectSeconds 60
```

The runner uses seeds 741901 (OFF) and 741902 (ON), plus ports 47880/47881. For each policy it writes the isolated world's command, starts two clients, waits for both policy acknowledgements and mutual membership, checks both observers' actual holder manifests, records pre-failure inventories, kills its second client, and checks the survivor after expiry. It cleans up its own clients. Test policy entries remain in the control file; the default seed 2026 policy is not modified.

Inventory success criteria:

- ON: both peers report all 42 manifests and each has non-primary copies.
- OFF: no peer reports non-primary retained copies; held counts match advertised counts.
- Failure ON: no unavailable regions when synchronized surviving manifests exist.
- Failure OFF: unavailable count equals the departed owner's former region count.

Latest recorded run: `Builds/BackupPolicy/78b4b1a9-results.json`.

| Policy | Departed-owner regions | Unavailable afterward | Survivor loaded tiles |
| --- | ---: | ---: | ---: |
| OFF | 12 | 12 | 110 |
| ON | 16 | 0 | 121 |

Pre-failure evidence: `78b4b1a9-inventory-False.json` and `78b4b1a9-inventory-True.json`. In the inspected ON case, one peer held 16 primary + 26 backup copies and the other 26 primary + 16 backups. Ownership counts vary because peer IDs change between isolated runs; the meaningful assertion compares unavailable regions to that run's departed ownership. There is one selected trial per policy here; no statistical uncertainty, failure probability, or speedup estimate is available.

Both lifecycle clients reported `Save failed: Access to the path is denied.` in the execution sandbox. Therefore these trials establish live retention/availability, not successful disk backup or restart durability. Persistence code paths and missing-owner restoration are separately covered by source/Unity validation; a durable multi-machine experiment remains necessary.

## 6. Browser checks

Open the panel and select a fresh real observer. Under Connections & backups, verify the direct link, 42 regions with two fresh holders, primary versus backup counts, exact inventory selection, stale transitions, and the difference between matching memory and nonmatching/failed saves.

In Failure simulation:

1. Reset: 4 online peers, 42 available, 42 protected by at least two copies.
2. Peer C retention OFF: backup count changes from 24 to 0 for the fixed fixture.
3. Reset, choose all OFF, and disconnect A. Simulated time advances to 8 s, with 31 available and 11 unavailable.
4. Turn all ON. The 11 remain unavailable until A reconnects.
5. Reset; ON; disconnect A: all 42 remain available.
6. Compare ON vs OFF shows 0 versus 11 unavailable without issuing a live command.

The simulator assumes completed synchronization and uses one example change record per region. The fixed peer IDs give A 11 regions.

### Live world trial

Two Unity Editor Play mode clients ran recipe 03 and protocol 04 over loopback with world seed 20814091. The browser showed both player markers and all 42 distinct seeds. Each client initially held 42 copies. Turning off individual retention left the targeted client with its 30 primary copies. A disconnect with world backups ON preserved all 42 regions and the shared beacon on the observer. With world backups OFF, the observer lost exactly the departing client's 30 primary regions and its beacon after timeout. Turning ON while the owner was away left the missing regions unavailable. Reconnecting restored all 42 regions and the beacon. Persistence was disabled for this trial.

## 7. Research evidence package

`Research/evidence/manifest.json` lists selected source and report SHA-256 hashes, the evidence scope, and environment versions captured during packaging. These hashes bind the report to files at packaging time; they are not a public release signature or proof of independent replication.

The paper's source ledger records online reference URLs, access dates, and the narrow claims each supports. Local observed results come from the named project artifacts, not from the cited external papers. The PDF and Markdown describe the same study.

## 8. Next experimental protocol

Use at least three separate machines. Fix peer identities/topology per trial; randomize policy order and repeat a declared number of runs. Introduce measured loss, delay, jitter, partitions, graceful departures, crashes, simultaneous failures, and cold restarts. Log durable acknowledgements, transfer progress, convergence time, unavailable duration, owner reassignment, and false failure declarations. Measure frame-time percentiles, CPU/GPU memory, bytes/s, and per-peer traffic as peer count grows. Compare full replication to a specified replica factor under the same workload. Implement missing acknowledgement/epoch instrumentation before claiming those properties are tested.

Keep all raw logs, identify failure domains, and report unsuccessful trials alongside successes. An intentional OFF-loss experiment demonstrates policy behavior; it is not evidence that backups are unnecessary or that an internet-scale world has been validated.
