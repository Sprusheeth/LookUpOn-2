> Historical pre-battlefield verification. For current recipe 02/protocol 03 results, use [docs/TESTING.md](docs/TESTING.md), [BATTLEFIELD-VERIFICATION.md](BATTLEFIELD-VERIFICATION.md), and [AdminPanel/PEER-SIMULATION-VERIFICATION.md](AdminPanel/PEER-SIMULATION-VERIFICATION.md).

# Verification — 7 November 2026

## Unity and game

- Unity 6000.5.2f1 compiled and built the Windows prototype successfully.
- 467 assertions passed: deterministic terrain, exact chunk seams, negative chunk coordinates, consistent-hash placement/remapping, idempotent beacon merges, state fingerprints, actual UDP loopback synchronization, world mismatch rejection, late join recovery, timeout handling, and monitoring telemetry.
- The two-process Windows smoke test completed successfully. Both instances loaded 81 chunks, observed one peer, and held the shared beacon. They received 125 and 126 packets in this run. The runner now refreshes process state and reports functional results separately from slow shutdown.

## External dashboard

- Two Node tests passed: multiple local observers, stale snapshot ages, malformed/partial snapshot isolation, real HTTP assets/API, read-only methods, and origin checks.
- JavaScript syntax check passed.
- Browser verification with two normal game instances confirmed live X/Y/Z, chunk coordinates, matching beacon data, matching remote saved fingerprint, local save timestamp/path, and `.bak` presence.
- Peer search filtered the directory correctly.
- Selecting chunk 1,0 updated the region inspector with its coordinates and owner/backup assignments.
- Closing the second game caused peer expiry and region reassignment to appear in Activity. The remaining observer stayed live. The departed observer was marked offline, with stale values clearly labelled.
- Switching observers now updates displayed data and freshness immediately.
- JSON snapshot export produced a downloadable file.
- No browser console errors were observed during these checks.

`LookUpOn-admin-preview.jpg`, in the parent outputs folder, was captured during the two-peer verification. The monitor service must be running to use the browser dashboard. See `AdminPanel/README.md` for launch instructions.

## Limits of this verification

Both peers ran on one laptop. These checks do not establish multi-machine durability, internet traversal, authenticated remote storage, or global network consistency. The current hash-ring backup assignments remain advisory, and remote save hashes remain self-reports.
