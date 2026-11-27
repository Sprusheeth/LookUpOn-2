# LookUpOn development roadmap

Current baseline: 10 km × 10 km, 42 regions × 64 tiles, recipe 03, protocol 04. Updated 13 November 2026.

## Implemented

- Procedural battlefield terrain, deterministic region seeds, continuous borders, local tile streaming, three mesh detail levels, and near/medium terrain collision.
- First-person exploration, shared beacons, trusted-LAN UDP/direct connection, local consistent-hash responsibility, and per-slot persistence/identities.
- Region manifest replication and explicit ON/OFF availability policy; missing-owner persistence and safe player holding in unavailable terrain.
- External admin with a live player and region seed map, world and local peer retention controls, disconnect and reconnect trials, and reported copy and save inventories.
- Isolated four-peer simulation with per-peer retention switches and owner/last-holder failure scenarios.
- Recorded Unity, Node, two-client synchronization, and real owner-loss validation. These are functional checks, not a scalability/performance study.

## 1. Durable region data and failure domains

Add per-region edit logs/snapshots, copy versions, durable write acknowledgements, and a defined replica factor. Distinguish copying, acknowledged, under-replicated, and recovering states. Full replication to connected peers is the current behavior; two preferred ring secondaries are not an enforced storage factor.

Acceptance: three separate machines preserve acknowledged edits after a primary crash and a process/machine restart. Recovery verifies versions and hashes, reports missing copies, and records transfer time and failure logs.

## 2. Ownership epochs and partition behavior

Add transfer epochs, stale-owner fencing, membership suspicion/refutation, and a documented partition policy. Decide which writes pause and which operations can merge. Consistent hashing alone is not consensus. Design deletions/capacity handling instead of claiming the current bounded/off-pruned beacon store is a general CRDT.

Acceptance: controlled partitions and rejoin/churn tests cannot silently overwrite acknowledged state; operator views show unresolved transfers and unavailable regions.

## 3. Measure and improve rendering/networking

Profile frame-time percentiles, memory, generation cost, bandwidth, and replica catch-up. Move generation into jobs, pool meshes/decorations, tune colliders, and add interest management. Introduce MTU-aware packet sizing, bounded retransmission, and congestion behavior.

Acceptance: publish hardware, workload, repeated measurements, and declared thresholds. Compare changes against the same baseline. A 60 FPS target setting is not measured 60 FPS.

## 4. Focused gameplay

Add decoration collision, team spawns, traversal validation, health, a basic weapon/objective loop, and spectator/respawn behavior. Player elimination must not implicitly remove the software's storage role. Preserve the original generated battlefield style without copying commercial assets.

Acceptance: peers complete an objective, synchronize permitted changes, and retain the chosen backup behavior during defeat, respawn, and disconnect.

## 5. Authenticated multi-network multiplayer

Add authenticated peer identities, signed/authorized administration, encrypted sessions, anti-abuse limits, rendezvous, NAT traversal, and relay fallback. Avoid exposing the current localhost control service publicly. A discovery/relay service can coexist with distributed world storage.

Acceptance: peers on different home networks connect/reconnect; forged identities, policies, and storage acknowledgements are rejected; documented security and network tests pass.

## Optional future world expansion

The current supported map stays 10 km square. A separate larger-world recipe would require 64-bit logical addresses, local offsets/floating origin, compatible packets, precision-safe sampling, and a save migration plan. Do not simply remove bounds and describe the result as infinite.

See [README.md](README.md), [architecture](docs/ARCHITECTURE.md), [testing](docs/TESTING.md), and [research paper](Research/LookUpOn-Research-Paper.md).
