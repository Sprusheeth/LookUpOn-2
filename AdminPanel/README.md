# External battlefield admin panel

Run node AdminPanel/server.mjs from the LookUpOn project folder, then open http://127.0.0.1:8765. Or run Start-Admin.ps1 in PowerShell. Node.js is required; no third-party packages are needed. The foreground Node command stops with Ctrl+C.

The service binds to 127.0.0.1. The game writes telemetry every second; snapshots become stale after five seconds. Old recipe-01 snapshots are excluded. Choose the observer when multiple game sessions run.

## World backups control

The switch writes a seed-specific setting to AdminPanel/controls.json. The game reads it every half-second and propagates it to connected peers. The panel displays the requested setting until game telemetry acknowledges the policy revision. Do not close the service before the command is acknowledged.

ON retains region seed manifests and beacon changes across connected peers. OFF prevents active replica retention, removes departed peers' changes, and makes their primary regions unavailable after the eight-second peer timeout. The game unloads unavailable tiles and holds affected players safely in place. Turning ON after a loss cannot recreate missing changes.

The command applies to the selected world seed, not just the displayed observer. Newer revision/authority tuples win; this handles stale packets but is not authenticated administration or consensus. POST controls require a same-origin JSON request with a custom header. The service is intended for localhost/trusted-LAN development.

## Views

- Overview: the full 7 × 6 map, 42 availability states, world backup switch and live counts.
- Live world: all 42 regional seeds, live player coordinates, current primary and backup holders, and per-session retention, disconnect, and reconnect controls. Controls target local running clients with fresh telemetry.
- Map detail: select a region, then choose Selected region / 64 tiles to see its 8 × 8 subdivision.
- Peers: world X/Y/Z, ownership region, heartbeat age, current beacon count and convergence.
- Connections & backups: observer-to-peer UDP links, fresh seed coverage, each peer's primary copies versus retained backups, exact region inventory, and conservative loss exposure. Remote-to-remote links are unknown. Assigned owners are not counted as holders without manifest evidence.
- Failure simulation: four virtual peers, the game's 64-node-per-peer FNV hash ring and 42-region layout, per-peer backup retention switches, disconnect/reconnect, an eight-second timeout fast-forward, copy matrix, availability map, event log, and an ON-versus-OFF comparison. It runs entirely in the browser and works without game telemetry. It never calls the live control API or changes real save files.
- Regions: individual seed, primary, actual reported seed holders, loaded tiles and availability.
- Replication & saves: each peer's applied backup policy, seed copy count, matching beacon save reports, local save path and previous archive status.
- Activity: policy changes, peer joins, owner timeouts and traffic totals.

Remote reports are not independently verified disk proofs. Two processes on one laptop do not protect against laptop failure. Gameplay elimination is separate from closing a storage peer. Read ../BATTLEFIELD.md for the current architecture and limits.

## Try the simulation

1. Open Failure simulation. All four peers initially hold 42 regions each, including their primary regions.
2. Uncheck Retain backups on a peer: its backup count becomes zero, while it keeps its primary copies.
3. Reset, then disconnect Peer A while backups remain ON: all 42 regions stay available on surviving peers.
4. Reset, choose All backups OFF, then disconnect Peer A. Its 11 regions become unavailable, leaving 31 of 42 available.
5. Choose All backups ON. Those 11 remain unavailable. Reconnect Peer A to make its retained local state available again.
6. Compare ON vs OFF runs the two cases side by side without changing the current experiment.

The model assumes synchronized copies before each action and uses one example change record per region. For a live trial, choose a connected observer in **Live world**, apply a command to the other peer, and wait for its acknowledgement. Disconnect closes that peer's UDP socket while its game and telemetry continue. The observer removes the departed peer after eight seconds. Reconnect reopens the same port. The world OFF switch overrides all individual retention settings.

Run `node --test AdminPanel/server.test.mjs AdminPanel/simulation.test.mjs` from the project root. `Tools/Test-BackupPolicy.ps1` tests actual two-process manifests and owner loss in isolated world seeds. Optional `-InspectSeconds 60` holds the connected ON case for dashboard inspection.
