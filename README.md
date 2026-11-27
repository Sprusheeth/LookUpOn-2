# LookUpOn

A Unity 6 prototype for a procedurally generated multiplayer battlefield, peer-held world data, and an external administration dashboard.

**Current baseline:** Unity **6000.5.2f1**, world recipe **03**, UDP protocol **04**, telemetry schema **2**. Documentation updated **13 November 2026**.

The playable world is a bounded **10 km × 10 km** square. It has **42 ownership regions**, each divided into **64 rendering tiles**, for **2,688 tiles**. The generator creates a river valley with rolling hills, woodland, gravel banks, and a dirt track. It streams tiles as players explore. This is an exploration and distributed-systems prototype. Combat, health, weapons, and gameplay elimination are not implemented.

The [original project and Windows build](../LookUpOn-Original-20261113-112213/) are archived beside this project. To open the previous map, use that copy in Unity Hub or run its own Windows build.

## Documentation

| Document | Purpose |
| --- | --- |
| [Architecture](docs/ARCHITECTURE.md) | Components, generation, ownership, synchronization, persistence, and failure semantics |
| [Testing and reproduction](docs/TESTING.md) | Exact checks, commands, evidence, success criteria, and measurement limits |
| [Admin guide](AdminPanel/README.md) | Live controls, connection/backup inventories, and isolated simulations |
| [Development roadmap](ROADMAP.md) | Implemented features and prioritized next work |
| [Research paper](Research/LookUpOn-Research-Paper.pdf) | Formatted prototype study with online scholarly references |
| [Editable paper](Research/LookUpOn-Research-Paper.md) | Editable text, tables, citations, and reproduction notes |
| [Bibliography](Research/references.bib) | Reusable BibTeX references |
| [Research package guide](Research/README.md) | Evidence files, source ledger, and PDF rebuild instructions |
| [Battlefield details](BATTLEFIELD.md) | Map geometry and detailed backup behavior |
| [Game verification](BATTLEFIELD-VERIFICATION.md) | Historical Unity and two-client checks for recipe 02 |
| [Admin/simulation verification](AdminPanel/PEER-SIMULATION-VERIFICATION.md) | Recorded live inventories and browser checks |

## 1. Requirements

- Windows for the supplied executable and PowerShell test runners. Other build targets have not been verified.
- Unity Editor **6000.5.2f1** and its Windows build support to edit or rebuild. Unity is not needed just to run the supplied game.
- Node.js with built-in `fetch`, `node:test`, and ES modules. Node **20 or newer** is the project baseline for the admin scripts; no third-party npm packages are required.
- Two game instances for a local multiplayer demonstration; separate computers on the same IPv4 LAN for a machine-to-machine test.
- Write access to the project telemetry/control directories and the game's per-user save directory.

Check Node with `node --version`. Run the following commands from this project folder, the directory containing `Assets`, `AdminPanel`, and `Tools`.

## 2. Start the game

### Supplied Windows build

Open `Builds/Windows/LookUpOn.exe`, then click **Enter the frontier**. Keep the executable, `LookUpOn_Data`, `UnityPlayer.dll`, and the other build files together.

### Unity editor

1. In Unity Hub, add this project folder and open it with 6000.5.2f1.
2. Wait for asset import and script compilation to complete.
3. Open `Assets/Scenes/Frontier.unity` and enter Play mode.
4. If the prototype scene is missing, use **LookUpOn > Create or reset prototype scene**. This recreates the generated scene, so preserve custom scene edits first.

| Input | Action |
| --- | --- |
| WASD / mouse | Move / look |
| Shift / Space | Sprint / jump |
| E | Place a shared survey beacon |
| Tab or Esc | Release/capture the mouse and open/close the connection panel |

## 3. Connect peers

### Two instances on this laptop

Start two instances. Each normal instance leases a separate local save slot and normally selects a distinct port starting at 47770. In the connection panel, use ports **47770** and **47771**, then start LAN networking on both. For direct connection, enter **127.0.0.1** and the other instance's port.

For repeatable startup from PowerShell:

```powershell
Start-Process -FilePath '.\Builds\Windows\LookUpOn.exe' -ArgumentList @('--seed','2026','--port','47770','--connect','127.0.0.1','--remote-port','47771')
Start-Process -FilePath '.\Builds\Windows\LookUpOn.exe' -ArgumentList @('--seed','2026','--port','47771','--connect','127.0.0.1','--remote-port','47770')
```

### Two computers on a LAN

1. Copy the complete project/build layout to each computer. Preserve `AdminPanel` beside `Builds`, so the game can locate its local control file.
2. Run the same world recipe, network protocol, and world seed on both computers.
3. Start networking and use the other computer's LAN IPv4 address and UDP port. `127.0.0.1` only reaches the same computer.
4. If Windows asks, allow the game on the intended private network. Do not disable the firewall; check the game's private-network rule if packets are blocked.
5. Check that both game windows and both observers report the other peer. No NAT traversal or public-internet matchmaking is provided.

LAN discovery broadcasts to UDP **47770-47777**. Explicit direct connections can use another valid UDP port. There is no general peer-list gossip or guaranteed remote-to-remote connectivity; an observer reports its own known direct peers.

## 4. Open the external admin panel

```powershell
node .\AdminPanel\server.mjs
```

Open [the local admin panel](http://127.0.0.1:8765/). Keep this terminal running; Ctrl+C stops it. Alternatively, launch the hidden local service and open the browser with:

```powershell
& .\AdminPanel\Start-Admin.ps1
```

The launcher records its server PID and logs under `Builds/AdminLogs`. To use a different port, run `node .\AdminPanel\server.mjs --port 8766` or pass `-Port 8766` to the launcher. The admin service binds to localhost; it is separate from the game and does not host gameplay.

Select the current **Observer**. Game snapshots update roughly every second and become stale after five seconds. A fresh remote holder report requires a recent peer heartbeat; an expired game peer is removed after eight seconds. Observer age, heartbeat age, and network latency are different measurements.

| View | What it shows |
| --- | --- |
| Overview | 42-region world, player coordinates, loaded tiles, beacons, and policy acknowledgement |
| Live world | All 42 seeds on the map, player markers, fresh copy counts, and local peer retention/disconnect controls |
| Peers | Endpoints, coordinates, region coordinates, heartbeat age, and state fingerprints |
| Connections & backups | Direct links, seed coverage, each peer's primary/backup inventory, exact region lists, and conservative loss exposure |
| Failure simulation | Four virtual peers, individual retention switches, disconnect/reconnect, copy matrix, and ON/OFF comparison |
| Regions | Independent seeds, primary responsibility, reported holders, loaded tiles, and availability |
| Replication & saves | Memory/save fingerprints, local save status, path, and archive presence |
| Activity | Membership events, policy events, and traffic counters |

The service reads telemetry written on its own computer. It does not automatically collect exports from every laptop. Remote copy claims arrive through the selected game's UDP peer reports. A matching hash is not proof of a durable remote disk write.

## 5. Change live backup behavior

Open [Live world](http://127.0.0.1:8765/#live-world) with two game clients connected. Choose one as **Observer** and use the other player's row to **Turn retention OFF**, **Disconnect**, or **Reconnect**. A retention override keeps that player's primary copies and drops its backup copies. **World backups OFF** takes precedence over individual retention settings. Check the row for acknowledgement before running the next action.

To compare policies, first leave world backups ON and wait for two copies per region. Disconnect the other player and watch the observer through the eight-second timeout. Reconnect, turn world backups OFF, and repeat. The departed peer's primary regions become unavailable. Reconnect it to restore its regions. These controls act on updated game clients that write telemetry to this dashboard's machine. Players on other computers can use their own local dashboard or the in-game connection menu.

Use **World backups** and wait for the game to acknowledge the requested policy revision. The setting applies to that **world seed across connected peers**, not just to the selected observer. The game polls its local control file every 0.5 seconds; propagation and acknowledgement require active game frames and network traffic.

| Behavior | Backups ON | Backups OFF |
| --- | --- | --- |
| Seed retention | Copies available manifests from connected peers | Retains primary manifests only |
| Beacon exchange | Repeatedly exchanges retained changes, including replicas | Sends own authored changes; accepts other authors from their active peer |
| Local saves | Own state plus retained replicas; previous-save `.bak` on replacement | Own-authored changes and primary catalog; no new backup archive |
| Owner expires | Surviving copies can preserve terrain and changes | Its former terrain regions become unavailable; its remote live changes are pruned |
| Player in missing terrain | Normal if region remains available | Held safely in place with an unavailable-region message |
| Enable after data loss | Copies surviving data; cannot invent missing changes | No retroactive recovery until a valid holder/owner returns |

OFF deliberately enforces terrain unavailability, even though the procedural formula could mathematically regenerate base geometry. This is a project policy for demonstrating ownership and availability; seed secrecy is not the mechanism. Surviving missing-owner records are persisted so a restart does not silently bypass the policy.

ON is conditional protection: another peer must actually receive and retain the required data before the owner disappears. There are no durable replica acknowledgements, quorum commits, or consensus. Full replication currently reaches directly connected peers; the ring's secondary preferences do not enforce a strict two-backup storage factor. Gameplay death will be separate from storage-peer failure when combat is added.

## 6. Run the isolated simulation

The simulation works without a running game. Its controls never call the live backup API or change real save files.

1. Open **Failure simulation**, then **Reset**. Four virtual peers each hold all 42 regions after assumed synchronization.
2. Uncheck **Retain backups** on one peer. Its non-primary backup count becomes zero.
3. Reset, keep backups ON, and disconnect Peer A. The simulator advances through the eight-second timeout immediately; all 42 regions remain available.
4. Reset, choose **All backups OFF**, then disconnect A. Its 11 regions become unavailable and 31/42 remain available.
5. Choose **All backups ON**. The 11 missing regions stay unavailable. Reconnect A to make its retained local copies available again.
6. Use **Compare ON vs OFF** for the side-by-side result. Experiment with the last remaining backup holder to see the effect of losing all online copies.

The model assumes completed synchronization and uses fixed virtual identities and an example change record per region. The Live world tab shows actual peer reports.

## 7. World generation and rendering

| Property | Current value |
| --- | --- |
| Playable bounds | X and Z: -5,000 to +5,000 m; 100 square kilometres |
| Ownership grid | 7 columns × 6 rows = 42 regions |
| Region size | About 1,428.571 × 1,666.667 m |
| Rendering subdivision | 8 × 8 tiles per region; 56 × 48 tiles world-wide |
| Tile size | About 178.571 × 208.333 m |
| Requested neighborhood | Up to 11 × 11 = 121 tiles |
| Retention envelope | Up to 13 × 13 = 169 tiles while moving |
| Mesh detail | 32, 16, or 8 cells per side; shared eight-segment borders |
| Generation scheduling | One queued tile per frame after the initial warm start |

Each regional seed is derived from the world seed and region coordinates using the existing seed derivation. Shared valley and hill elevations blend with region-specific detail at borders. Procedural materials distinguish grass, rock, gravel banks, and dirt ruts. Trees, rocks, grass, and animated river surfaces are built with each streamed tile. The 7 × 6 layout makes regions rectangular.

The map is **generated on demand but finite**. Subdivision by itself does not solve rendering cost: streaming, LOD, and limited colliders bound local work. Terrain colliders exist at near/medium detail; decorations currently have no combat collision. Frame-rate targets are settings, not performance measurements.

## 8. Saves, files, and configuration

Normal Windows saves use Unity's persistent data folder:

```text
%USERPROFILE%\AppData\LocalLow\LookUpOn\LookUpOn\battlefield-2026-slot0-v3.json
```

The world seed and slot vary. Up to 32 local slots can be leased. A slot keeps a stable peer ID when its save is successfully written and loaded. Each save includes recipe/version, seed, identity, owned/retained manifests, ownership records, and permitted beacon state. Saves are attempted every three seconds when serialized state changes and on normal quit; telemetry is separate. A `.bak` is a prior local save, not another machine's replica. Historical archives remain inactive in OFF mode. Recipe 03 uses separate save files and starts fresh. Earlier saves remain available to their corresponding builds.

| File/directory | Role |
| --- | --- |
| `Assets/Scripts/Core` | World addressing, hash ring, manifests, beacon state |
| `Assets/Scripts/World` | Streamed meshes, LOD, decorations, terrain collision |
| `Assets/Scripts/Networking` | UDP validation, direct peers, timeout, anti-entropy, policy propagation |
| `Assets/Scripts/Monitoring` | Telemetry export and local control-file reader |
| `Assets/Scripts/LookUpOnGame.cs` | Runtime setup, save slots, input/HUD integration, smoke mode |
| `Assets/Editor/ProjectSetup.cs` | Scene creation, 631-assertion validation, Windows build |
| `AdminPanel/dist` | Dashboard and browser-only simulation |
| `AdminPanel/server.mjs` | Local HTTP service, telemetry API, live control writer |
| `AdminPanel/controls.json` | Runtime per-world control document; generated and ignored by Git |
| `Telemetry` / `Builds/Windows/Telemetry` | Editor / standalone peer exports |
| `Tools` / `Builds` | Test runners / generated binaries, logs, and evidence |
| `Research` | Paper, references, source ledger, and reproducibility evidence manifest |

Map dimensions and recipe live in `Core/WorldMath.cs`; streaming/LOD in `World/ChunkWorld.cs`; network limits in `Networking/PeerNetwork.cs`; storage policy in `Core/RegionCatalog.cs`. Changing geometry or recipe requires compatible peers and a deliberate save migration/version change. Moving only the executable away from the expected project layout breaks local admin path discovery.

## 9. Validate and rebuild

In Unity, use **LookUpOn > Validate prototype**. Use **LookUpOn > Build Windows prototype** to recreate the prototype scene, validate, and build under `Builds/Windows`. This build action resets the generated scene; preserve custom edits before running it. Unity editor/import/licensing must be working first.

```powershell
# Dashboard API, holder evidence, and deterministic simulation checks
node --test .\AdminPanel\server.test.mjs .\AdminPanel\simulation.test.mjs

# Two actual Windows clients: terrain grounding and shared beacon synchronization
& .\Tools\Test-TwoPeers.ps1

# Start the admin service in another terminal before this real failure test
& .\Tools\Test-BackupPolicy.ps1

# Optional: pause the connected ON case for live dashboard inspection
& .\Tools\Test-BackupPolicy.ps1 -InspectSeconds 60
```

The backup runner uses isolated seeds 741901/741902 and ports 47880/47881; it does not change the default world's policy. It verifies actual holder inventories, terminates its own test peer, and waits for the expected terrain outcome. The smoke runner uses ports 47870/47871 and disables persistence. Runners clean up their own child processes. Inspect reports and logs under `Builds/Smoke` and `Builds/BackupPolicy`.

Recorded evidence: **631 Unity assertions**, **9 Node tests**, and a two-client smoke pass with **121 tiles, one remote, and one beacon per client**. In the latest recorded owner-loss run, OFF lost availability for 12/12 departed-owner regions; ON lost availability for 0/16. Different identities produced different ownership counts; these are functional scenarios, not statistical benchmarks. Sandbox test saves reported access denied, so those failure trials do not verify disk durability. See [testing details](docs/TESTING.md).

## 10. Troubleshooting

| Symptom | Check/action |
| --- | --- |
| Admin page refuses connection | Start the Node service; use its printed localhost port; inspect `Builds/AdminLogs` if using the launcher |
| No game or stale observer | Run the updated build/Play mode, select the fresh observer, verify telemetry directory write access; old sessions remain historical |
| No peers | Same seed/recipe/protocol, distinct local ports, correct remote IPv4/port, active LAN mode, private-network firewall rule |
| Port already in use | Stop the intended old instance or select another port; check the game status text |
| Control stays pending | Keep the service/game running, inspect game admin status, ensure expected folder layout and writable control file; newer remote revisions override stale requests |
| Region turns red / player freezes | Check OFF policy and expired owner; restore that owner or a valid surviving copy. Enabling ON cannot recreate lost edits |
| Save failed / persistence disabled | Inspect the exact reported save path and permissions. Close conflicting instances normally. Do not treat an in-memory match as a successful save |
| Two peers have copies but no resilience | Processes on one machine share a failure domain; use distinct machines and verified durable writes |
| Unity compile/shader errors | Use the pinned editor, wait for import, inspect Console; build only after validation passes |
| PowerShell blocks a runner | Review the script and use your organization's permitted execution method; no global execution-policy change is required by this project |

## 11. Known limits and next work

This trusted-LAN prototype has no authenticated identities/admin, encryption, anti-cheat, NAT traversal, relay, global membership consensus, owner epochs, or durable replica acknowledgements. Peers can advertise false state. The local HTTP origin/host checks are not internet authentication. Do not expose the admin port publicly.

Each node admits at most **16 remote peers** and **4,096 beacons**. Repeated JSON UDP pages are bounded to 24 beacons and 42 manifests; a 12,000-byte receive cap is not an MTU-safe transport design. Congestion control and internet-safe packet sizing remain future work. Full divergent stores at capacity are not guaranteed to converge. OFF pruning is intentionally not a monotonic CRDT deletion scheme. A fixed timeout can misclassify a delayed or partitioned peer.

Next priorities: separate-machine/packet-loss trials; explicit durable-copy acknowledgements and region edit logs; ownership epochs and partition policy; performance profiling and generation jobs/pooling; terrain/decor collision and a focused combat loop; authenticated internet connectivity. See [ROADMAP.md](ROADMAP.md) for acceptance criteria.

The supplied work does not assign a new open-source license or claim a publication venue. Add the actual authors, affiliation, and applicable project license before external release or submission.
