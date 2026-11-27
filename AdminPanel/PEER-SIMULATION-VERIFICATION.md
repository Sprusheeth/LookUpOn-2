# Peer connections and backup simulation

Verified 7 November 2026. This change updates the external Node/browser admin panel; the existing Unity executable remains compatible and requires no rebuild.

## What was verified

- Nine Node tests passed: telemetry collection, live policy persistence/origin restrictions, all JavaScript asset routes, replicated owner failure, OFF pruning and non-retroactive recovery, individual retention switches, last-holder loss, all peers offline, reset isolation, deterministic hash-ring ownership, and evidence-based live inventories.
- Browser controls were exercised: individual Peer C backup OFF reduced its backup inventory from 24 to 0; all OFF plus Peer A disconnection left 37/42 available; enabling ON kept five unavailable; reconnecting A restored 42/42. ON/OFF comparison displayed the expected two outcomes. No browser warning/error logs were observed.
- The new Connections & backups view was inspected against two real connected Windows game processes. The observer held 16 primary + 26 backup copies; its remote held 26 primary + 16 backup copies. All 42 regions had two fresh reported holders, and both reported matching in-memory change-store fingerprints. Exact remote region lists and the direct link were verified through the UI.
- The real game lifecycle script now asserts primary/backup inventories on both observers before disconnecting a peer. With OFF, no non-primary replicas were reported. With ON, both peers reported all 42 region copies and nonzero backups.

Latest actual two-process run: `Builds/BackupPolicy/78b4b1a9-results.json`.

| World policy | Departed peer's regions | Regions unavailable after timeout | Loaded tiles on survivor |
| --- | ---: | ---: | ---: |
| OFF | 12 | 12 | 110 |
| ON | 16 | 0 | 121 |

Pre-failure manifests are saved beside the report as `78b4b1a9-inventory-False.json` and `78b4b1a9-inventory-True.json`. Actual region allocations differ between runs because test peer identities differ. The browser simulation uses fixed identities, so Peer A owns five regions reproducibly.

## Scope and limits

Individual backup switches exist in the isolated simulation. Live game backup control still applies to the entire world seed. The simulation uses the same FNV/64-virtual-node ownership algorithm, but assumes synchronization completes between actions. Its example region change record represents continuity; it does not emulate real transport, partial replication, disk persistence, or gameplay.

Real test clients reported an AppData save access error under the execution sandbox. This run verifies live seed retention and region availability, not disk durability. Remote manifest and fingerprint reports are trusted-LAN claims, not authenticated disk proofs. Copies in multiple processes on the same laptop do not protect against laptop loss.

The isolated test clients were stopped after verification. The default seed 2026 backup control was not changed. Start the local panel with `Start-Admin.ps1`; the simulation can be used without a running game.
