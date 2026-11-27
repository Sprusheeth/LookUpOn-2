# LookUpOn — 10 km battlefield

This document supersedes the earlier 190 km prototype description.

## World structure

- Fixed bounds: X and Z from −5,000 m to +5,000 m, covering 100 km².
- Exactly 42 ownership regions in a 7-column × 6-row grid.
- Each region is about 1,428.571 m wide × 1,666.667 m deep.
- Every region has its own deterministic seed derived from the world seed and region coordinates.
- Each region contains 8 × 8 rendering tiles: 64 per region, 2,688 for the map.
- A rendering tile is about 178.571 m × 208.333 m. These are rectangular because 42 does not form a square grid.

The terrain contains a winding river, gravel banks, rolling hills, woodland, and a dirt track. The world seed sets the course of the valley. Regional detail blends into shared border elevation so neighboring regions meet continuously.

## Rendering

Only a local area of up to 11 × 11 requested tiles is streamed, with a one-tile retention margin (up to 169 retained). Tiles use 32, 16, or 8 mesh cells per side according to distance. Mixed LOD borders share an eight-segment edge. Far tiles omit terrain collision and reduce decorations. One tile is generated per frame.

Streaming and LOD bound visible geometry. Mesh generation runs on the main thread. Scenery meshes are batched per tile, and near trees use more detailed foliage. Terrain provides ground collision. Trees, grass, rocks, and the water surface are visual scenery. Weapons, health, damage, and elimination gameplay are future work.

## Live backup control

Start `node AdminPanel/server.mjs` from the project folder, then open `http://127.0.0.1:8765`. Select the active observer. The **World backups** button changes the policy for that world seed. The panel shows a pending request until the running game reports the new revision.

The local game reads `AdminPanel/controls.json` every half-second. The setting propagates to connected peers in the game's UDP protocol. Revision and authority ordering prevent old packets or stale local files from undoing a newer choice. This is a trusted-LAN operator control, not authenticated internet administration.

### ON

- Connected peers copy region seed manifests and beacon state.
- Seed copies and state are saved in their local save slots.
- When a primary disappears, a surviving seed holder keeps its regions available and ownership is reassigned.
- Departed peers' beacon changes remain in replica stores.
- The dashboard lists reported seed holders and matching beacon save hashes.
- Replication currently copies to all directly connected peers. The hash ring's secondary preferences are not a strict two-backup storage limit.

### OFF

- Each peer retains seed manifests only for its primary regions.
- Peers still exchange their own live beacon changes while connected; imported copies are not relayed or saved as backups.
- When a peer expires after eight seconds, its beacon changes are removed from surviving live stores.
- Its former primary regions become unavailable and their render tiles/colliders are unloaded.
- A player in an unavailable region is held in place with an explicit message, rather than falling through missing terrain.
- Missing-owner state is persisted with the survivor's catalog; restarting that survivor does not silently recover the missing regions.
- Returning original owners or a remaining valid seed holder can restore availability. Turning ON after a loss does not invent missing changes.
- Old archival `.bak` files from earlier ON periods are not deleted, but are inactive in OFF mode; no new `.bak` files or remote backup saves are created. OFF mode does not restore remote changes from an old archive.

Closing a peer's network process is a disconnect. Future gameplay elimination will remain separate: a defeated player can continue providing seed/backup storage while its software is running.

## Saves and compatibility

World recipe 03 and network protocol 04 are required. Update all peers before joining the same session. Recipe 03 uses its own save slots. The archived recipe 02 build can still read its own saves.

Each local instance leases a separate slot:

`%USERPROFILE%/AppData/LocalLow/LookUpOn/LookUpOn/battlefield-2026-slot0-v3.json`

Saved peer identity is stable within a slot, so returning owners can be recognized. Save slots hold beacon data, retained region manifests and region ownership records. Persistence is disabled in automated smoke mode.

## Verification

The Unity validation includes full map bounds, 42 distinct region seeds, 2,688 tile coverage, cross-region seams, mixed LOD seams, actual UDP synchronization, policy propagation, ON recovery, OFF terrain/state loss, policy revision ordering, and persistence of missing ownership. Node tests cover command persistence, monotonic policy revisions, same-origin control protection, and telemetry handling.

## Next work

1. Test separate computers and packet loss, then add durable storage acknowledgements and region-specific edit logs.
2. Add real combat collision, traversal, health, weapons, team spawns, and elimination/spectator behavior.
3. Move terrain generation into jobs and pool tile/decor objects; measure frame time and memory on target laptops.
4. Add authenticated peer identities and administration before exposing controls beyond localhost/trusted LAN.
