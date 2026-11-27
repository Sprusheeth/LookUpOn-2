# Battlefield verification

## Automated checks

- Unity 6000.5.2f1: 631 assertions covering the 10 km square, 42 distinct region seeds, 2,688 rendering tiles, regional border seams, mixed-LOD edge stitching, hash-ring behavior, UDP synchronization, live policy propagation, ON recovery, OFF region/state loss, revision ordering, and persistence of missing ownership.
- Three Node tests: snapshot freshness, partial/malformed telemetry handling, HTTP assets, read-only route protection, same-origin control validation, control persistence and increasing revisions.
- JavaScript syntax validation passed.
- Final two-player smoke test passed on both clients with 121 loaded tiles, one connected peer and one shared beacon. The terrain-only raycast confirmed both players above the ground (player Y 95.151 m, terrain Y 95.071 m). Report: Builds/Smoke/b787309b-peer-a.json and the matching peer-b report. One development-player process required test-runner cleanup after completing its functional report.

## Actual process failure checks

Tools/Test-BackupPolicy.ps1 launched independent Windows game processes and applied controls through the admin HTTP API in isolated test worlds:

| Policy | Regions owned by killed peer | Unavailable after timeout | Result |
| --- | ---: | ---: | --- |
| OFF | 29 | 29 | Passed |
| ON | 16 | 0 | Passed |

The OFF survivor unloaded all locally visible tiles because its local area belonged to the departed owner. The ON survivor retained 121 visible tiles. Report: Builds/BackupPolicy/b8c0a798-results.json.

The sandboxed process-failure run could not write to the application's normal save directory; it validates live replica/availability behavior rather than disk durability. A normal game launched through Windows separately reported successful saves under both OFF and ON modes. Missing-owner persistence is covered by the Unity catalog restore assertions.

## Browser control checks

The World backups switch was used in the browser. The normal game reported OFF, the same policy revision, and 'Own data saved · backups OFF'. Switching back to ON produced a new revision and 'Saved with replicas'. The panel distinguishes pending commands from the game's acknowledged policy.

The browser map was checked to contain exactly 42 selectable regions. The selected-region view showed the 8 × 8 rendering subdivision. Final dashboard preview: ../LookUpOn-battlefield-admin.jpg. The normal world was left with backups ON.

## Practical limits

Both peers ran on one laptop, so these checks do not establish multi-machine durability, internet connectivity or authenticated storage. Backups currently copy to all directly connected peers. Combat gameplay and collision on ruined structures are not implemented.
