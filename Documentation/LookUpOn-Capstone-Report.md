# LookUpOn Capstone Project Report

## Chapter 1 Academic context and project organization

### 1.1 University project context

LookUpOn is a university capstone project associated with the School of Computer Science and Artificial Intelligence at SR University. The team has confirmed SR University as the organization for this work. The university's official school page identifies computing and intelligent technologies as its academic domain [13]. This report therefore describes an academic software engineering artifact. It does not assert employment at an external company, production deployment for a client, or organizational acceptance of the software.

The project brings together several subjects that are often taught separately: mathematical terrain generation, real-time rendering, network communication, distributed responsibility, data retention, and software observation. Their interaction produces the central engineering problem. A peer may render geometry successfully while holding no durable copy of another player's changes. A region may remain mathematically reconstructible while the application's availability policy intentionally blocks access to it. A monitor may show an assigned secondary node without evidence that this node has received anything. These distinctions make the project useful as an implementation and evaluation exercise.

The deliverable baseline consists of a Unity Windows game, its source project, a separate Node/browser admin panel, automated test runners, recorded reports, and research/documentation artifacts. The supported game world is bounded rather than infinite. The network implementation targets a cooperative local network rather than arbitrary untrusted internet participants. Those boundaries are part of the project definition and guide every claim in the later chapters.

### 1.2 Team and guide

| Team member | Enrollment number |
| --- | --- |
| M.Sprusheeth Rao | 2303A51206 |
| A.Akshith reddy | 2303A51177 |
| T.Snuhith | 2303A510H9 |
| A.Sai Varshith | 2303A51831 |

The project guide is MATHIVANA D. These names, enrollment numbers, the guide name, and the university organization were supplied directly by the team. In particular, the enrollment number 2303A510H9 includes the letter H. No professional title, faculty designation, signature, approval date, or individual work allocation has been inferred. The report presents the integrated artifact as team work without inventing a division of responsibilities.

The authoring date of this documentation is 8 November 2026. Named game-test artifacts were recorded on 7 November 2026. These dates identify evidence preparation and do not imply a sixteen-week execution history. A chronological internship diary, attendance record, offer letter, completion certificate, or employer assessment would require authentic records outside the material available for this report.

### 1.3 Documentation scope and reading paths

The report has two complementary purposes. An evaluator can follow the problem, design, implementation, results, and learning outcomes as a capstone narrative. A developer or operator can use the later chapters to locate data structures, start the system, reproduce a failure scenario, interpret telemetry, and understand the limits of an observed result. The appendices retain detailed schemas and selected source evidence so that a short claim in the main narrative remains traceable.

Three reading paths are useful. The design path covers Chapters 2 through 8 and explains why generation, ownership, synchronization, and persistence remain separate. The evidence path covers Chapters 9 through 11 and distinguishes actual process behavior from the deterministic browser model. The operational path begins with Chapter 12 and the data dictionaries, where commands, paths, status messages, and recovery decisions become concrete.

The separate research paper is a concise technical study with its own abstract, related work, methods, findings, and references. It is not a substitute for this longer engineering report. The presentation is a sixteen-slide summary with a planned speaking duration of approximately six minutes. A viewer should be able to move from a presentation result to the corresponding report section and then to an actual named test file.

### 1.4 Status vocabulary

This report uses implemented for behavior visible in source or a runnable artifact, observed for a result present in a named record, derived for a value calculated from the design, and proposed for work that has not been completed. These categories prevent a future architecture from being mistaken for the existing software. For example, durable replica acknowledgement is proposed; a reported seed holder is implemented; the count of two reported holders per region in an inspected trial is observed.

A passed functional check is narrower than a reliability or performance conclusion. A valid geometry assertion supports the checked boundary relationship. A two-process synchronization report supports that particular state exchange. Neither result establishes behavior for thousands of players, independent machines, network partitions, or malicious peers. Later discussion uses the same vocabulary when explaining both successful results and unresolved risks.

## Chapter 2 Introduction problem definition and objectives

### 2.1 Motivation for generation on demand

A conventional map asset contains substantial pre-authored geometry or terrain data. LookUpOn explores a different representation: a compact world recipe and coordinates generate the base landscape during execution. The player can start near a small loaded neighborhood while the remainder of the supported world exists as addressable procedural space. This reduces the need for a complete prebuilt terrain package within this prototype, although it does not eliminate executable assets, code, materials, or generated runtime memory.

Generation on demand changes where work happens. The software exchanges storage and distribution of a complete base map for local computation, deterministic addressing, and rendering management. A seed alone is insufficient unless every participating build interprets it through a compatible recipe. A later code change can alter terrain even when the seed remains unchanged. Recipe versioning is therefore part of the world identity rather than merely a development label.

The implemented world is a height field with rolling ground, roads, shell craters, vegetation, and simple ruined concrete structures. It supports exploration through a first-person controller. It does not include authored commercial map assets, caves generated below an overhanging terrain surface, combat mechanics, destructible buildings, inventory, or a mission economy. These exclusions keep the initial distributed-state problem small enough to inspect and test.

### 2.2 The distributed continuity problem

Suppose peer A places a survey beacon and then disappears. Base terrain can be regenerated from the recipe, seed, and coordinates. The beacon is different: its identity, author, and location are an event in the shared world. If no participant retains that event, the terrain formula cannot reconstruct it. The first continuity question is therefore whether another peer actually received the mutable record before A left.

A second question concerns responsibility. Even if all nodes can calculate a region's height, the application may require an active primary or a retained manifest before allowing that region to remain available. LookUpOn implements precisely such a rule. The OFF policy intentionally blocks former primary regions after an owner timeout. This makes responsibility and retention observable, but should not be explained as computational loss of a secret terrain seed.

A third question concerns evidence. A ring can nominate backup candidates without transferring any bytes. A packet can advertise a digest without proving a durable write. A local save can succeed while all replicas remain on the same laptop. The project needs a way to show these different facts separately so that a visually healthy dashboard does not imply stronger resilience than the software actually provides.

### 2.3 Problem statement

The problem is to build and evaluate a small multiplayer world in which independently seeded regions generate compatible terrain, only nearby tiles consume rendering work, peers exchange a limited shared state, and an operator can observe how a chosen backup policy affects region availability after owner loss. The implementation must remain explainable through deterministic calculations, explicit states, recorded reports, and repeatable local workflows.

The project is successful at this stage when it demonstrates these functions within declared conditions. It does not need to prove a new distributed algorithm or complete a production online game. A large requested map area is a spatial specification, while network scalability is a separate property that requires measurements. Likewise, a storage-role assignment is a policy decision, while successful replication requires observable receipt of data.

### 2.4 Objectives and acceptance boundaries

| Objective | Present implementation | Evaluation boundary |
| --- | --- | --- |
| Generate a world from code | Deterministic height, roads, craters and decorations | Same pinned recipe and supported bounds |
| Divide the battlefield | 42 ownership regions and 64 tiles per region | Fixed 7 by 6 arrangement |
| Bound local rendering | Streaming, three detail bands and limited colliders | No measured frame-time guarantee |
| Synchronize peers | Direct/LAN UDP poses and beacon pages | Cooperative LAN or local processes |
| Distribute responsibility | Locally reconstructed consistent-hash ring | Local membership, no global agreement |
| Make backups observable | Reported manifests, fingerprints and live policy | Trusted self-reported evidence |
| Demonstrate owner loss | Actual process trial and isolated model | Receipt-dependent availability, not disk proof |

The objectives deliberately avoid claims that the map is infinite or that subdivision solves every rendering problem. They also avoid describing the current backups as an enforced one-primary/two-backup protocol. The implementation forwards retained state to directly connected peers when enabled. Preferred secondary positions in the ring describe placement preferences rather than a strict replica count enforced by durable acknowledgements.

### 2.5 Research questions

RQ1 asks whether separately seeded regions can share continuous boundaries while meshes use different resolutions and stream locally. RQ2 asks whether the ON and OFF policies produce the intended availability outcome after an actual peer process disappears. RQ3 asks whether the external panel distinguishes assigned responsibility, reported stored manifests, memory equality, and saved-state evidence. Each question maps to a testable behavior rather than an open-ended claim of scalability.

RQ1 is supported by source-defined boundary interpolation and recorded seam assertions. RQ2 is supported by a selected owner-loss trial for each policy. RQ3 is supported by telemetry construction, holder-accounting tests, and browser inspection of real inventories. None of these questions substitutes for future measurements of tactical map quality, human usability, internet connection success, long-term reliability, or durable recovery after machine failure.

## Chapter 3 Research background and design comparison

### 3.1 Procedural generation context

The procedural-generation literature distinguishes content representation and generation/evaluation approaches. The taxonomy of Togelius and colleagues supplies background for that distinction [1]. LookUpOn uses deterministic rule and interpolated-noise construction, with no search-based quality optimization. Its evaluated properties concern address coverage and boundaries rather than player enjoyment, aesthetic preference, or battlefield balance.

A generated landscape needs a contract for shared edges. Independent interior variation alone does not guarantee compatible adjacent meshes. The code therefore combines a world-wide broad height function with locally seeded detail that fades near a region boundary. This is a design trade-off: variation remains regional in the interior, while continuity constrains the border. It is incorrect to describe the regions as completely unrelated random surfaces.

### 3.2 Placement and distributed lookup

Consistent hashing provides a placement context for changing participant sets [2]. LookUpOn uses a sorted ring with virtual peer positions, reconstructed independently at each node. Chord adds distributed lookup routing and associated routing state [9]. LookUpOn does not implement Chord finger tables, successor stabilization, or a network-wide lookup service. Its direct endpoint exchanges cannot inherit Chord's routing results merely because both designs use a ring.

The implementation distinction matters when membership differs. A deterministic ring produces the same assignment for the same input set. It says nothing about whether two nodes currently have the same input set or whether they have received a region's latest changes. Placement determinism and data convergence are therefore separate evaluation targets. The validator checks order independence and restricted remapping examples; it does not prove global membership agreement.

### 3.3 Distributed game architecture

Colyseus separates object placement, discovery, and replica synchronization for distributed games [3]. That separation informs the way this report describes the prototype, but LookUpOn does not implement its query/prefetch system or reproduce its player-count evaluation. The present mutable world state is a bounded survey-beacon collection, a much simpler workload than a complete shooter simulation.

This choice creates a useful intermediate artifact. It allows the team to observe ownership changes and state exchange before introducing damage, ammunition, competitive hit validation, or interactive construction. Once those actions exist, they will need clearly defined authority and conflict rules. The current beacon merge rule should not be generalized automatically to mutually exclusive gameplay outcomes.

### 3.4 Replication and convergence

Dynamo supplies background on partitioning, replication, and recovery choices in an available storage service [4]. The current project has no equivalent durable write quorum or version-history system. Shapiro and colleagues formalize convergence conditions for replicated data types [5]. LookUpOn's deterministic same-ID rule helps repeated beacon exchange, while finite capacity and deliberate OFF-policy pruning limit any unconditional convergence statement.

A deterministic conflict rule can resolve two values with the same identity after both are observed. It cannot cause an unreceived value to appear. A capacity check can also cause nodes with different full stores to reject different new identities. These implementation facts motivate bounded claims and a future design for compaction, deletion, causal history, and retention limits rather than calling the whole world a solved CRDT.

### 3.5 Failure detection and partitions

SWIM distinguishes failure detection and membership dissemination and uses probing/suspicion mechanisms [6]. LookUpOn currently receives direct updates and expires a peer after a fixed threshold. Gilbert and Lynch establish a conflict between atomic consistency and availability during partitions in their model [10]. The prototype has neither a formal CAP classification nor a partition-safe owner protocol. The relevant lesson is to state what should happen when membership evidence becomes incomplete.

Raft provides a replicated-log consensus reference [11]. A monotonically ordered policy revision is simpler: it helps reject stale settings but does not elect a leader, commit a log, or fence an old owner. These mechanisms must not be treated as interchangeable. Future work may use a consensus mechanism for selected control decisions, but the present implementation does not include one.

### 3.6 Datagram and engine foundations

RFC 8085 discusses UDP application responsibilities, including congestion behavior and message sizing [7]. RFC 8899 describes datagram packetization-layer MTU discovery [12]. A 12,000-byte receive limit in the prototype is a parser/resource bound rather than a delivery guarantee or proof of path-safe datagram size. Unity's Mesh API provides the runtime geometry foundation [8]; it supplies capability, not measured project performance.

### 3.7 Comparative position

| Topic | Background system or concept | LookUpOn baseline | Unimplemented extension |
| --- | --- | --- | --- |
| Generated content | Procedural-generation taxonomy | Deterministic rule/noise height field | Search or gameplay-quality optimization |
| Placement | Consistent hashing | Local virtual-node ring | Measured balancing under large churn |
| Lookup | Chord | Known direct endpoints | Distributed routing service |
| Distributed game | Colyseus | Poses and simple beacon state | Interest queries and prediction |
| Replication | Dynamo design context | Received full connected-peer copies | Durable quorum acknowledgements |
| Convergence | CRDT conditions | Deterministic merge with limits | Formal editable-world data types |
| Membership | SWIM | Fixed direct heartbeat expiry | Indirect probing and suspicion |
| Agreement | Raft | Revision/authority ordering | Replicated-log consensus |
| Transport | UDP guidelines and DPLPMTUD | Bounded JSON UDP messages | Congestion and MTU-aware delivery |

The contribution is an integrated, inspectable prototype and a reproducible owner-loss workflow. It does not claim a new hashing technique, a complete distributed database, or performance superiority over the cited systems. External sources support design context. Actual LookUpOn outcomes come from local code and named project reports.

## Chapter 4 Requirements risk and feasibility analysis

### 4.1 Functional requirements

Requirements are stated at the level of observable behavior. A requirement to display a holder means that a manifest report must support the display; it does not mean that a preferred ring assignment is enough. A requirement to preserve a region with ON is conditional on a surviving received copy, because no implementation can recover an event that was never transferred and is no longer retained anywhere accessible.

| ID | Functional requirement | Implemented mechanism | Evidence category |
| --- | --- | --- | --- |
| FR01 | Start a generated world | Runtime scene composition | Playable build |
| FR02 | Enforce supported map bounds | WorldMath and Explorer guards | Source and validation |
| FR03 | Address 42 ownership regions | 7 by 6 region grid | Validation |
| FR04 | Address 64 tiles per region | Eight subdivisions per axis | Validation |
| FR05 | Generate repeatable base height | Recipe/seed/coordinate function | Source and assertions |
| FR06 | Join compatible direct peers | UDP packet admission | Actual smoke trial |
| FR07 | Reject incompatible world packets | Protocol, recipe and seed checks | Validator coverage |
| FR08 | Share player poses | Periodic packet state | Runtime implementation |
| FR09 | Exchange beacons repeatedly | Bounded state pages | Smoke and merge checks |
| FR10 | Assign region responsibility | HashRing | Ring assertions |
| FR11 | Track actually advertised copies | RegionCatalog and remote manifest IDs | Inventory records |
| FR12 | Apply a live world policy | Control file and UDP revision ordering | Actual policy trial |
| FR13 | Preserve received copies with ON | Catalog and store retention | Selected owner-loss case |
| FR14 | Gate former regions with OFF | Missing-owner records | Selected OFF case |
| FR15 | Hold players over missing terrain | Explorer availability gate | Implemented controller path |
| FR16 | Save permitted local state | Leased JSON slot | Source and normal-run status |
| FR17 | Mark stale observations | Snapshot and heartbeat age | Node/browser checks |
| FR18 | Export an observer snapshot | Browser download | Dashboard implementation |
| FR19 | Isolate model actions | Browser-only Simulation object | Model tests |
| FR20 | Expose primary and backup inventories | Holder-based accounting | Real browser inspection |

The evidence category column does not imply that every row has the same verification strength. The source establishes that a branch exists. A validator exercises a defined path. A recorded process trial demonstrates an integrated scenario. A disk-write guarantee would need a different experiment and acknowledgement semantics. This distinction should remain visible when an evaluator selects requirements for a demonstration.

### 4.2 Nonfunctional requirements and constraints

| ID | Requirement or constraint | Current treatment | Limit |
| --- | --- | --- | --- |
| NF01 | Bound local geometry | Streaming and LOD | Main-thread work still unprofiled |
| NF02 | Bound retained neighborhood | Radius and retention envelope | Depends on location and availability |
| NF03 | Bound mutable records | Capacity 4,096 | Divergent full stores may not converge |
| NF04 | Bound admitted remotes | At most 16 | Capacity is not a benchmark result |
| NF05 | Bound packet parsing | Array and datagram limits | No congestion/MTU guarantee |
| NF06 | Keep operator plane separate | Local HTTP/file interface | Local directory layout matters |
| NF07 | Preserve basic compatibility | Recipe/protocol fields | No old-recipe migration |
| NF08 | Show errors honestly | Save/control/status telemetry | Self-reported remote data remains trusted |
| NF09 | Avoid unsafe player fall-through | Terrain-layer and availability checks | No combat/decor collision |
| NF10 | Support reproducible checks | Scripts and named reports | Hardware/timing baseline incomplete |
| NF11 | Keep data inspectable | JSON and readable schemas | No authenticated peer proofs |
| NF12 | Preserve source provenance | Reference/source/evidence ledger | Not a signed release |

No target percentile frame time, maximum memory consumption, packet-loss tolerance, or WAN connection rate is supplied as an achieved result. Such quantities require controlled measurement. Application.targetFrameRate equal to 60 expresses a request to the runtime and does not prove that frames finish within 16.7 milliseconds. Similarly, a ten-kilometre extent describes geometry coverage and not simultaneous player capacity.

### 4.3 Risk register

| Risk | Consequence | Current response | Remaining action |
| --- | --- | --- | --- |
| Owner exits before replication | Mutable record can be absent | Explicit receipt-dependent claims | Add durable acknowledgements |
| All copies share one laptop | Machine failure removes service | Failure-domain warning | Use independent machines |
| Heartbeats are delayed | False expiry and missing regions | Display ages/status | Add suspicion and loss trials |
| Nodes see different membership | Conflicting responsibilities | Document local-view scope | Add epochs and partition policy |
| Save directory is unwritable | Memory state is not durable | Clear error and saved digest | Verify write/restart experiments |
| Old policy arrives late | Settings can regress without ordering | Revision/authority comparison | Authenticate policy authority |
| Full stores diverge | New records can be rejected differently | Capacity disclosure | Define compaction/admission semantics |
| Datagram exceeds path support | Loss or fragmentation | Receive-size/resource cap | MTU-aware packetization |
| Peer lies about storage | False healthy dashboard state | Trusted-LAN boundary | Signed identity and storage protocol |
| Recipe changes without versioning | Same seed yields changed terrain | Recipe field | Explicit migration procedures |
| New instance shares a save | Identity/data conflict | Exclusive slot lease | Test lease crash/restart behavior |
| Admin folder layout changes | Local control path fails | Document deployment layout | Explicit configuration discovery |
| Decorations lack collision | Gameplay expectations mismatch | State exploration scope | Implement collision and traversal tests |
| Report overstates results | Invalid engineering conclusion | Evidence classifications | Review every claim against records |

### 4.4 Technical operational and economic feasibility

Technical feasibility has been demonstrated for a local exploration prototype. The code generates the requested bounded layout, starts direct peers, synchronizes a beacon, changes a world policy, and observes the intended failure outcomes. This is a useful proof of integration. It does not establish feasibility for internet-scale competitive combat, because authority, network connectivity, abuse control, and transport mechanisms remain incomplete.

Operational feasibility depends on a small set of understandable prerequisites. The supplied Windows build must keep its data and DLL files together. The local admin must be able to read telemetry and write controls. The game must write its per-user save directory for persistence. Operators need to choose a fresh observer and distinguish a solo session from an active network. These conditions can be checked through explicit status messages and the runbook rather than implicit assumptions.

The project uses Unity built-in modules, C#, Node built-in libraries, browser JavaScript, and PowerShell scripts. The report does not invent a monetary budget, hosting bill, or commercial licensing entitlement. Local execution avoids the need to operate a dedicated gameplay server for the selected trials, but it does not imply zero deployment cost. Independent test machines, network instrumentation, identity services, relays, and future storage overhead would require a separate resource assessment.

## Chapter 5 Spatial addressing and procedural world design

### 5.1 The supported address space

The world occupies the closed horizontal bounds X and Z from -5,000 to +5,000 metres. The total side length is 10,000 metres, giving an area of 100,000,000 square metres, or 100 square kilometres. This extent is a supported playable square. Sampling a procedural function outside it does not make those coordinates a supported game world. Movement and packet-admission checks use the same bounded-world intent.

The hierarchy separates responsibilities from drawing units. There are seven region columns and six region rows. Each region has eight tile subdivisions on each axis. Consequently, the world has 56 tile columns and 48 tile rows. A region spans approximately 1,428.571 metres along X and 1,666.667 metres along Z. A tile spans approximately 178.571 by 208.333 metres. Rectangular regions are an arithmetic consequence of fitting 42 cells inside a square with a 7-by-6 arrangement.

| Quantity | Derivation | Exact count or approximate dimension |
| --- | --- | --- |
| Map side | Configured MapSize | 10,000 m |
| Map area | 10,000 multiplied by 10,000 | 100 square kilometres |
| Region count | 7 multiplied by 6 | 42 |
| Tiles per region | 8 multiplied by 8 | 64 |
| Total tile count | 42 multiplied by 64 | 2,688 |
| Tile columns and rows | 7 times 8; 6 times 8 | 56 by 48 |
| Region width and depth | 10,000/7; 10,000/6 | 1,428.571 by 1,666.667 m |
| Tile width and depth | 10,000/56; 10,000/48 | 178.571 by 208.333 m |

The choice of 42 ownership regions comes from the project requirement. There is no experiment comparing that count with 36, 49, or a dynamic partition tree. The implementation therefore treats the layout as a baseline and avoids an unsupported claim that it is an optimal chunk count. Rendering efficiency depends on the loaded neighborhood and mesh/collider work, not only on the number of ownership cells.

### 5.2 Position to tile and region conversion

Address conversion first shifts a coordinate by the half-map size, scales it by the number of cells, floors the result, and clamps it to the valid index interval. Clamping is important at the exact positive edge. Without it, +5,000 multiplied by the grid scale would produce an index equal to the cell count, which lies outside the last valid cell. The negative edge maps to index zero.

The conceptual tile formula along X is floor((X + 5000) times 56 / 10000), clamped to 0 through 55. The Z formula uses 48 and clamps to 0 through 47. Region coordinates follow by integer division of tile coordinates by eight. Region identity is seven times the region Z index plus the region X index. Tile/region origins use the reciprocal conversion from an index back to the world boundary coordinate.

| Position example | Tile X | Tile Z | Region X | Region Z | Region ID |
| --- | --- | --- | --- | --- | --- |
| X=-5000, Z=-5000 | 0 | 0 | 0 | 0 | 0 |
| X=0, Z=0 | 28 | 24 | 3 | 3 | 24 |
| X=0, Z=-90 | 28 | 23 | 3 | 2 | 17 |
| X=5000, Z=5000 | 55 | 47 | 6 | 5 | 41 |

These examples are arithmetic illustrations of the address functions, not movement traces from a user study. The spawn neighborhood uses the X=0, Z=-90 example. Exact region/tile boundary positions require particular care because floor chooses the next cell on one side of a border. Shared height evaluation and border interpolation must therefore make neighboring choices compatible.

### 5.3 Regional seed derivation

The seed function hashes a string containing the recipe marker, world seed, region X, and region Z. For recipe 02 the prefix is battlefield-v2. The function uses the FNV-1a update pattern with 32-bit wraparound and interprets the result as a signed integer. The number is a deterministic recipe input. It is not an encryption key, secret authorization token, unique global object identifier, or collision-proof hash.

The tested world seed 2026 yields 42 distinct regional seeds in the validator. That finite observation does not prove collision freedom across arbitrary world seeds and coordinates. A later scheme requiring a collision-free region identity should use the explicit recipe/world/coordinate tuple as identity and treat the seed only as generated data. The present RegionId already supplies a unique grid index within one fixed world.

Decorations combine tile coordinates and the region-derived seed to initialize a local pseudorandom sequence. This makes the placement repeatable for the same build and inputs. It avoids retaining every decorative mesh position as a downloaded map. The implementation does not claim identical results across arbitrary runtime-library changes, and recipe compatibility remains important when distributing updated builds.

### 5.4 Broad elevation regional detail and roads

The broad landscape combines low-frequency world-seeded interpolated value-noise samples at different coordinate scales. Regional detail uses a higher-frequency locally seeded sample. A smooth edge factor reduces that regional contribution near the boundary. Four deterministic craters per region modify the detail term. This construction creates visible local variation while allowing a shared broad height to control the edge.

The noise interpolation uses a cubic smoothing function between grid values. This is a value-noise implementation, not a claim that the project implements Perlin's gradient-noise reference algorithm. Giving the correct name matters because a similar visual result does not make two algorithms identical. The evaluation targets deterministic sampling and compatible edges rather than a perceptual comparison between noise families.

Road placement uses the minimum distance to repeated X/Z road lines, with a smooth falloff. The road elevation follows the broad terrain with a small lowering, rather than forcing a fixed absolute road height. A constant road height could cut implausibly deep trenches through high terrain. The final recipe avoids that issue by tying road height to the shared broad landscape. Colors interpolate earth/olive ground, rocky slopes, and gray road surfaces.

### 5.5 Geometry mesh construction and seams

For a tile with n cells per side, the mesh contains two triangles per cell. The three configured detail levels therefore contain 2,048, 512, and 128 terrain triangles per tile. Flat shading duplicates vertices per triangle, so the terrain mesh has six vertices per cell before decorations. These values are exact derived counts for the current mesh builder. They are not estimates of memory use or rendering time.

| Cells per side | Cells | Triangles | Flat-shaded vertices | Terrain collider |
| --- | --- | --- | --- | --- |
| 32 | 1,024 | 2,048 | 6,144 | Yes |
| 16 | 256 | 512 | 1,536 | Yes |
| 8 | 64 | 128 | 384 | No |

Different resolutions normally introduce extra boundary vertices on the denser mesh. LookUpOn constrains every LOD boundary to the same eight-segment profile, interpolating denser edge samples along that profile. This keeps shared edges consistent while allowing interiors to retain different densities. Validation compares both neighboring regional boundaries and mixed-LOD interpolation. It does not amount to an exhaustive proof over every world seed and floating-point platform.

### 5.6 Streaming and collider scope

The renderer requests a radius-five square around the current tile, at most 11 by 11 tiles. Existing tiles can remain within a radius-six envelope while the player moves, bounding retention by 13 by 13, or 169 tiles. Near-first ordering prioritizes useful work. A warm start prepares nine tiles near the spawn location, after which the queue builds at most one tile per frame. Availability changes also trigger periodic refresh so that missing regions unload without requiring movement.

Near and medium tiles receive terrain colliders. Far tiles omit them and use fewer decorations. The Explorer callback checks that the destination tile exists and that its region remains available. This reduces the chance that a player enters unready terrain. It does not turn decorative structures into physical cover. Ruins and vegetation remain noncolliding visual objects in this build, and no combat-traversal claim should rely on them.

## Chapter 6 Component architecture and data flow

### 6.1 Runtime composition

LookUpOnGame composes the prototype at runtime. It establishes the world seed and identity, acquires a normal save slot when persistence is enabled, initializes the beacon store and peer network, reads local backup controls, restores a saved catalog when compatible, and creates the terrain, controller, camera, lighting, and materials. This centralized composition file wires otherwise separate responsibilities without acting as a centralized network server.

The scene primarily provides the application component and shader references. The editor utility can recreate the generated prototype scene, validate it, and build the Windows executable. This is convenient for reproducibility but means that the build command can overwrite custom changes to that generated scene. An operator should preserve scene edits before using the reset/build utility rather than assuming it is a nondestructive scene migration.

| Component | Main responsibility | Inputs | Outputs or effects |
| --- | --- | --- | --- |
| WorldMath | Geometry addresses and recipe | Seed and coordinates | Indices, origins and heights |
| ChunkWorld | Local mesh lifetime | Observer and availability | Meshes, colliders and decorations |
| Explorer | Local movement | Input and CanStand callback | Pose and safe holding |
| HashRing | Responsibility preference | Known membership | Ordered owners |
| RegionCatalog | Retained manifests and missing owners | Ring, policy and records | Availability and owner records |
| BeaconStore | Mutable record collection | Local creation and merge | Canonical state and digest |
| PeerNetwork | Direct communication and membership | Socket packets and local pose | Remote views, state and events |
| AdminControl | Local world command reader | controls.json | Ordered policy application |
| TelemetryWriter | Observer export | Runtime component snapshots | Per-peer JSON file |
| Node/browser admin | Observation and operator input | Telemetry and chosen command | Views, exports and control file |

### 6.2 Generation path and availability gate

The generation path begins with coordinates and a world recipe. ChunkWorld chooses a tile and detail level, samples heights, builds flat-shaded geometry, assigns colors and optional colliders, and adds deterministic decorations. The catalog does not supply a downloaded mesh. It supplies the availability decision that determines whether a tile is eligible for this generation path.

This dependency is visible in the World.RegionAvailable callback. A tile outside the map or inside an unavailable region is not built. Existing tiles in newly unavailable regions are removed on refresh. The same availability condition feeds the controller's CanStand check. Renderer and player therefore share an explicit operational rule instead of independently guessing whether a missing mesh is safe.

FIGURE: architecture

Figure 1. Principal component dependencies. The local operator plane observes state and sends a world command through a control file; it does not forward gameplay between peers.

### 6.3 Shared state and local persistence path

The state path has two different data forms. A region manifest identifies a deterministic regional seed that the receiving node can validate. A beacon record contains mutable author-created state. The catalog receives valid manifests only when retention permits it. The beacon store accepts records according to the current policy and deterministic merge rule. Save serialization then selects the state permitted by ON or OFF.

The persisted snapshot includes the recipe/version, world seed, saved identity, policy flag, beacon records, region manifests, and ownership records. The ownership array matters because a missing prior owner must not silently become a healthy newly assigned primary after restart. This is distinct from merely saving a list of seeds. Missing-owner restoration preserves the application's availability rule across a survivor restart.

The local save path follows Unity's persistent-data location and a per-world slot number. Stable identity depends on successfully saving and loading that slot. A process that cannot write the directory does not gain durable identity merely because an in-memory GUID remains stable during its lifetime. Save status and the reported saved digest therefore form part of the evidence path.

### 6.4 Networking and operator path

PeerNetwork owns the UDP socket, a bounded dictionary of known remote peers, the local ring, the catalog, and references to mutable state. It validates compatible packets, refreshes direct peer information, applies ordered backup policies, merges permitted data, expires old peers, prunes OFF live state, and sends another bounded page at the next scheduled opportunity. Each node executes this logic with its own membership information.

TelemetryWriter publishes a per-peer JSON snapshot about once a second. The Node server reads eligible local files from editor and standalone directories and exposes a collection to the browser. It does not discover every machine's telemetry automatically. The selected observer can describe its directly known remote peers, so the UI's connection graph is a star around that observer and makes no assertion about links among the other nodes.

Control travels in the other direction through a world-specific local file. The browser submits a same-origin command, the server writes a new revision/authority tuple, the game polls it, and the peer network forwards the newer policy in normal packets. A pending state remains visible until the selected game advertises acknowledgement. A stopped dashboard does not stop the UDP game, but a new command cannot be written through a stopped HTTP service.

### 6.5 Boundary between modules

The architecture keeps geometry generation independent of a particular endpoint, which makes terrain deterministic and locally testable. It keeps policy handling outside the renderer, which prevents rendering code from choosing durability semantics. It keeps state comparison outside disk assumptions, which allows the UI to report matching memory alongside a failed save. These separations simplify testing and make failure explanations more precise.

They do not eliminate coupling altogether. The world recipe version must agree with packet compatibility. Save restoration must agree with catalog semantics. The admin service must locate the expected directories. The controller must consult the same availability gate as the renderer. Future changes should therefore be reviewed across component boundaries rather than tested only as isolated classes.

## Chapter 7 Peer communication and shared-state protocol

### 7.1 Startup discovery and direct connection

A normal instance can start LAN networking on a chosen UDP port. Local save slots usually choose distinct ports beginning at 47770 when several instances run on one laptop. LAN discovery broadcasts to ports 47770 through 47777. Direct connection stores an IPv4 bootstrap endpoint and begins sending compatible packets to it. A direct port can differ from the discovery range, provided it is a valid available UDP port.

The bootstrap endpoint is a convenience for reaching a known peer, not a rendezvous server or NAT traversal service. On one laptop, 127.0.0.1 addresses the same machine. On two machines it does not address the remote computer; an operator must use the other machine's LAN IPv4 address. Matching world seed, recipe, and protocol are necessary before nodes treat each other as compatible members.

There is no general peer-list gossip mechanism. If A knows B and B knows C, the code does not automatically establish every A-C direct link merely because the names exist somewhere in the wider network. The operator and researcher must record the actual direct topology. Full replication over a complete known mesh and state exchange over a partial chain can have different behavior, even when every node runs the same executable.

### 7.2 Packet envelope

The packet is a JSON document carrying identity, compatibility fields, player pose, state pages, fingerprints, manifests, and backup-policy metadata. It is not a cryptographically authenticated message. GUID syntax and digest shape reject malformed inputs, but they do not prove who created the packet. A malicious node can produce well-shaped values and false claims in the current trust model.

| Packet field | Type or shape | Meaning |
| --- | --- | --- |
| game | String LookUpOn | Application discriminator |
| protocol | Integer 3 | Network recipe compatibility |
| worldVersion | Integer 2 | Procedural world recipe |
| seed | Signed integer | Chosen world seed |
| id | GUID in N format | Sender identity claim |
| x, y, z | Finite numbers | Player position |
| yaw | Finite number | Player heading |
| beacons | Array, at most 24 | Current outgoing page |
| beaconCount | Integer, 0–4096 | Sender's total store count |
| stateDigest | 64 lowercase hexadecimal characters | Reported memory fingerprint |
| savedDigest | Empty or compatible fingerprint | Reported completed-save fingerprint |
| backupsEnabled | Boolean | Sender's applied policy |
| backupRevision | Ordered integer | Policy revision |
| backupAuthority | GUID in N format | Equal-revision tie-breaker |
| regionSeeds | Array, at most 42 | Retained regional manifests |

The per-field bounds address basic compatibility and resource handling. They do not guarantee acceptable traffic for every network. JSON overhead, repeated manifests, bootstrap duplication, discovery broadcasts, and full connected-peer forwarding all contribute to actual bytes. A future benchmark should measure complete packet traffic rather than multiplying beacon payload size by a nominal send rate and calling that observed bandwidth.

### 7.3 Admission and processing order

The receive loop processes at most 64 available datagrams during a tick. Datagrams larger than 12,000 bytes are ignored. After JSON parsing, compatibility checks reject a wrong game marker, protocol, recipe, seed, self identity, malformed identity, nonfinite pose, out-of-world horizontal position, extreme vertical value, invalid counts/digests, or oversized arrays. A newly accepted identity is admitted only if the bounded remote-peer dictionary has room.

The code then updates endpoint, pose, last-seen time, packet count, beacon count, fingerprints, and advertised seed information. Valid manifest IDs are derived from records whose region and seed match the local expected recipe. Policy application can change retention before data merge. With ON, received permitted manifests and replicas can enter local stores. With OFF, only author-linked live beacon state is accepted from the sender.

This order has implications for tests. A packet can be compatible without proving the sender's storage claims. A malformed manifest is excluded from valid seed-region IDs even when an advertised raw array count exists. The admin inventory derives holder evidence from validated region IDs rather than treating an owner assignment or a raw count as a complete list. Keeping those representations separate prevents overcounting healthy coverage.

### 7.4 Periodic exchange and bounded pages

The sender nominally schedules updates every 0.15 seconds when the game loop advances. This is approximately 6.7 opportunities per second, not a measured delivery frequency under load. Every packet carries the current pose and metadata. Beacon pages take at most 24 eligible records, advancing a cursor through the sorted store and wrapping when needed. Repetition permits a compatible peer to receive records lost in an earlier datagram.

A full 4,096-record collection would require 171 pages to visit every record once if each page is full except the last. At the nominal interval that is about 25.65 seconds of send opportunities before accounting for loss, scheduling, topology, contention, or new records. This is a derived pagination-cycle illustration. It is not a measured convergence bound, because reception, repeated page ordering, conflicts, and capacity behavior also matter.

| Store size | Pages at 24 records | Nominal cycle illustration |
| --- | --- | --- |
| 1 | 1 | 0.15 s |
| 24 | 1 | 0.15 s |
| 25 | 2 | 0.30 s |
| 240 | 10 | 1.50 s |
| 4,096 | 171 | 25.65 s |

The receive loop and page bound avoid treating network input as an unbounded per-frame workload. They do not provide a reliable, ordered, congestion-controlled transport. There is no application acknowledgement for each page, no retransmission timer tied to a particular missing record, and no durable commit result returned to the author. Future transport work should preserve these distinctions when introducing reliability.

### 7.5 Beacon merge semantics

A beacon contains an identity, author, and X/Z position. The store rejects malformed GUIDs, nonfinite coordinates, and out-of-world locations. Records sort by identity. For a duplicate identity carrying a conflicting value, the implementation selects a deterministic ordering by author and then coordinates. If the incoming value does not win that ordering, the store does not change. This makes repeated identical packets idempotent for that record.

New identities are rejected once capacity is reached. This creates an important limit: two nodes that became full with different identity sets cannot necessarily accept each other's missing records. A deterministic duplicate rule cannot solve this admission divergence. OFF also intentionally removes remote records whose authors are no longer active. The data structure should therefore be described through its actual operations instead of being labeled a fully general grow-only replicated world.

The digest hashes canonical sorted records, writing identity, author, and coordinates in a defined binary sequence and normalizing signed zero. The hash is cached until the store revision changes. Two equal digest reports provide practical equality evidence for that representation. They do not prove authenticity, completeness relative to a missing author, absence of malicious collision attempts, or physical persistence to another machine's disk.

### 7.6 Membership expiry and topology implications

A remote peer whose last accepted packet becomes older than eight seconds is removed. The ring and catalog refresh after each removal, and OFF live-state pruning follows. The timeout is a local decision based on received traffic. A partition, stalled frame loop, or sustained packet loss may trigger the same branch as a crashed process. The event label expired therefore describes observed liveness evidence rather than proving a remote machine has physically failed.

Local membership also affects placement. The same region key can select different primaries on nodes that currently know different peer sets. The prototype has no epoch/fencing protocol to resolve every such disagreement. A future design should separate the authority to accept an edit from a locally calculated preference for who ought to own the region. This report does not claim that ring recomputation alone prevents stale or conflicting writers.

## Chapter 8 Backup policy availability and persistence

### 8.1 Responsibility does not prove possession

HashRing returns an ordered set of preferred peers. RegionCatalog stores actual retained manifests and remembers prior missing owners. These are different objects for a reason. Assigning B as a secondary for A's region does not ensure that B has received its manifest or beacon changes. The operator must see actual advertised holder evidence rather than a replication factor inferred only from assignments.

With ON, the present implementation retains copies from directly connected peers rather than enforcing exactly one primary and two backups. A two-node connected world can therefore give both nodes all 42 manifests. With more nodes, storage can grow toward full replication over the connected peer set. This is convenient for a small demonstrator but is not a measured scalable placement protocol.

### 8.2 Catalog state

The catalog has a held-record dictionary, a current primary array, a missing-owner array, and a membership set. A primary refresh computes the next preferred owner from the ring. If the previous primary departed and retention is OFF or no local received copy exists, the catalog records that owner as missing. A returning original owner can clear that missing condition. A valid received manifest can also restore a missing region while retention permits it.

Availability requires a valid region and no pinned missing owner, together with a retained local record or an active primary in the local view. The renderer uses this answer directly. This is why a deterministic terrain function does not silently bypass OFF policy. The code could compute a height, but the application decides that the region must remain unavailable until permitted state/ownership returns.

| Catalog concept | Purpose | Evidence represented |
| --- | --- | --- |
| Held manifest dictionary | Retained valid regional seeds | Local possession of recipe metadata |
| Primary array | Current ring preference | Local responsibility calculation |
| Missing-owner array | Preserve an unresolved owner loss | Application availability policy |
| Membership set | Recognize locally active identities | Recent direct-peer liveness |
| OwnerRecords | Save unresolved ownership context | Restart continuity of policy |

### 8.3 ON behavior

When enabled, peers can merge valid received manifests and beacon replicas. They repeatedly relay retained beacon pages to known peers and include their held manifests in outgoing packets. A normal save can contain these received records. If the primary disappears after required data has reached a surviving node, the survivor can keep the region available and responsibility can change according to its refreshed ring.

This description is intentionally conditional. It does not say that a newly placed change survives immediately at the moment of creation. A process can disappear before its next send, during a lost datagram, or before another node has persisted anything. There is no durable acknowledgement that tells the author a specified number of independent disks have committed an operation. ON means replica retention is enabled, not that all possible failure windows have disappeared.

### 8.4 OFF behavior

When disabled, each peer trims manifest retention to its primary regions. Outgoing beacon pages contain only its own authored records. A node may display compatible changes from currently connected authors, but it does not relay those remote records as backups or persist them as remote replicas. Once an author expires, surviving live stores remove that author's remote data. Former primary regions become unavailable when the catalog pins the missing owner.

The game unloads render tiles and colliders belonging to unavailable regions. The controller holds a player safely rather than allowing movement into absent terrain. An explicit unavailable-region message explains the condition. This behavior distinguishes deliberate policy enforcement from an unexplained rendering fault. Operators should first check owner/policy status rather than repeatedly rebuilding meshes or changing the seed.

FIGURE: backup_states

Figure 2. Owner-loss state transitions. Receiving a surviving copy before failure and retaining it afterward are distinct from enabling retention after data has already disappeared.

### 8.5 Policy transitions and recovery

| Starting condition | Action | Expected catalog outcome | Mutable-state implication |
| --- | --- | --- | --- |
| Owners active, ON copies received | Primary expires | Surviving copied regions can stay available | Received replicas remain |
| Owners active, OFF | Primary expires | Former regions become unavailable | Departed author's remote live changes prune |
| Region missing, no online copy | Enable ON | Missing state is not invented | Absent edits do not reappear |
| Region missing, original holder returns | Reconnect valid holder | Retained valid data can restore availability | Only retained records can return |
| Survivor with missing-owner records | Save and reload | Missing ownership is preserved | Restart must not silently invent state |
| Different policy revisions | Receive newer ordered tuple | Apply the newer policy | Retention follows applied policy |
| Stale control file or packet | Receive older tuple | Ignore stale policy | Avoid reverting a newer choice |

Recovery from a returning owner assumes that it still retains useful state. Closing and restarting a process with an unsuccessful save may not restore its prior data or identity. A copy that exists only in an inactive old archive is also different from a copy participating in current retention. OFF does not delete historical archives, but it does not use those archives to resurrect remote changes automatically.

Gameplay elimination is not implemented. A future defeated or spectating player should be distinguished from a closed storage process. Death can change avatar behavior while the application continues serving manifests and changes. Treating combat elimination as an automatic disk/network failure would create avoidable data loss and should require an explicit future design decision rather than an accidental coupling.

### 8.6 Save-slot design

Normal instances attempt to lease one of 32 per-world local slots through an exclusive lock stream. The slot name contains the world seed and recipe version. A successfully loaded saved identity lets an original owner be recognized after restart. The default Windows location is under the user's AppData LocalLow LookUpOn directory. Smoke mode intentionally disables persistence to keep its automated synchronization checks separate from normal saves.

A save snapshot records version, world version, seed, identity, policy flag, beacon collection, retained regional manifests, and 42 owner records. Under ON, permitted replicas are included. Under OFF, own-authored changes and primary catalog data are selected. Serialization compares the new content with the last saved content, avoiding unnecessary writes when nothing relevant changed. The game attempts saves periodically and on normal exit.

Writing a temporary file and replacing the current save reduces partial-file exposure. ON retains the previous local version as a .bak file on replacement. This local archive is not another machine's backup. The code does not provide a demonstrated power-loss/fsync guarantee, and the current trial records do not establish restart durability after every crash pattern. The reported saved digest clears on failure so the UI can avoid presenting a failed write as a confirmed save.

### 8.7 Selected persistence limitation

Both selected actual lifecycle trials recorded an AppData save-access error in the execution sandbox. The live tests nevertheless observed correct policy application, holder inventories, peer expiry, and availability changes. These facts can coexist: the survivor can retain data in memory while its disk save fails. This is precisely why telemetry separates current memory and last saved fingerprints.

A normal game launched through Windows had previously reported successful saves in ON and OFF modes, and catalog restoration is covered by recorded Unity assertions. Those observations are useful but do not replace a controlled independent-machine durability test. A future protocol should acknowledge a specific content version only after the required durable write, then test crash/restart and verify recovered data rather than infer durability from a healthy running view.

## Chapter 9 External administration and isolated simulation

### 9.1 Why a separate operator interface

The external admin panel gives an observer a view of state without putting every diagnostic control inside the game window. The game can continue rendering and exchanging UDP packets while a browser displays coordinates, responsibility, copies, saves, and membership events. The Node service runs on localhost and reads local telemetry exports. It is not a central server that simulates the world or forwards every player action.

The separation also creates an explicit control boundary. The operator requests a world policy through HTTP; the service writes a control document; the local game reads it and reports the applied revision. The browser does not assume that a successful HTTP response means every connected peer has already changed. Pending acknowledgement remains visible until the selected game snapshot provides evidence of application.

### 9.2 Snapshot collection and freshness

The server examines the editor Telemetry directory and the standalone Build/Windows telemetry directory. It orders candidate files by modification time, limits collection, skips old schema-1 snapshots, and handles malformed/partial files without discarding every other valid observer. Temporary filenames do not appear as completed JSON snapshots. The response includes server time, session snapshots, unreadable-file count, and the current control document.

A selected observer becomes stale after five seconds without a fresh export. For remote-holder coverage, the UI also considers the peer's heartbeat age. A recent local file does not automatically make every remote claim current. Conversely, an atomic file replacement can briefly omit a selected session during collection; the UI preserves selection rather than silently switching the operator to a different observer. This behavior avoids confusing one player's state with another's during a transient read.

Three time quantities must remain distinct. Snapshot age describes the local export's freshness. Heartbeat age describes how long since the observer accepted a remote update. The eight-second expiry threshold controls membership removal. None of these is a measured round-trip network latency, and the interface avoids labeling heartbeat age as ping or RTT.

### 9.3 Views and evidence semantics

| View | Principal information | Important interpretation |
| --- | --- | --- |
| Overview | Map, poses, loaded tiles, beacons and policy | Selected observer rather than a global network view |
| Peers | Endpoint, coordinates, age and fingerprints | Identity and remote state remain claims |
| Connections and backups | Direct links and per-peer inventories | Remote-to-remote links are unknown |
| Regions | Seeds, primaries, holders and availability | Assignment is separate from possession |
| Replication and saves | Memory/save equality and save path/status | Equality is separate from durable acknowledgement |
| Activity | Joins, expiry, policy and byte counters | Local observer event history |
| Failure simulation | Four virtual nodes and copy matrix | Isolated model, not a live process controller |

The world map draws all 42 ownership regions, not only the currently rendered tile set. A selected-region view exposes its 8-by-8 rendering subdivision. Primary ownership, reported holder lists, independent seeds, world bounds, loaded-tile counts, and missing-owner status support investigation of a red region. A low loaded-tile count can result from position, streaming progress, or unavailability and should not be treated as a direct performance score.

The Connections and backups view counts a peer's retained manifests from actual holder arrays. It divides them into primary copies and non-primary backup copies. Selecting a peer displays the exact region list. The loss-exposure warning conservatively identifies a region without another fresh reported holder. This is evidence scarcity, not a guaranteed prediction that the region will fail, because reports can be stale or incomplete and remote data remains unverified.

FIGURE: peer_inventory

Figure 3. Actual inspected peer inventories. In the selected ON case, the two peers reported 16 primary plus 26 backup copies and 26 primary plus 16 backups. The screenshot is evidence of reported manifests, not remote disk attestation.

### 9.4 Local HTTP interface

| Route | Method | Function | Mutation |
| --- | --- | --- | --- |
| / | GET | Dashboard entry page | None |
| /api/state | GET | Collected sessions and controls | None |
| /api/control | POST | World backup command | Writes local controls document |
| Allowlisted JS/CSS assets | GET | Dashboard and model modules | None |

Control requests require the expected localhost host/origin, JSON content type, a custom X-Lookup-Control header, a bounded request body, an integer world seed, and a boolean backupsEnabled. The service serializes writes, calculates a new revision above old local or observed values, and retains a GUID authority. These checks reduce accidental cross-origin control and malformed requests in a development environment. They are not user authentication, access control for an internet service, or signed peer policy.

The following command body is illustrative rather than a live request: a seed of 2026 and backupsEnabled equal to false request OFF for that world. The client should use the browser control and wait for game acknowledgement rather than manually invent revision values. The service owns revision creation so stale local commands can be recognized and ordered consistently by the existing tuple rule.

### 9.5 The deterministic failure model

The browser model contains four fixed GUID-shaped peer identities and the same region-key/FNV virtual-position construction used for local responsibility. It assumes that available copies synchronize between actions. Each region carries an example continuity record in the model, which stands in for the fact that mutable state must survive somewhere. The model does not execute the actual UDP code, save files, frame scheduling, or partial-transfer paths.

Each virtual peer has online and retention flags. A region has a current owner and a set of holder identities. Only online holders count toward availability. Disabling retention removes a peer's non-primary copies. Disconnecting a peer advances model time through the eight-second threshold immediately; where surviving copies exist, a refreshed owner can receive responsibility. Reconnecting a holder can expose its retained local copies again.

| Fixed model peer | Initial primary regions | Initial backup regions with all ON | Total held |
| --- | --- | --- | --- |
| Peer A | 5 | 37 | 42 |
| Peer B | 15 | 27 | 42 |
| Peer C | 18 | 24 | 42 |
| Peer D | 4 | 38 | 42 |

These are fixture-specific derived counts for the four fixed IDs, not a promise of balanced ownership in every live run. The unequal counts also show why having virtual positions does not make a tiny 42-key example perfectly balanced. The model is useful because the fixture remains reproducible, letting an operator repeat exactly the same owner-loss story without guessing which live peer owns the spawn region.

### 9.6 Model scenarios and boundaries

With all retention ON, the departure of Peer A leaves all 42 regions available from surviving copies. With all retention OFF, its five regions lose online availability, leaving 37. Enabling ON after that loss replicates the 37 surviving regions but does not create the missing five. Reconnecting A permits recovery from data it still holds. Disabling only Peer C retention reduces its backup count from 24 to zero while retaining its 18 primaries.

A last-holder scenario is more informative than a single primary failure. If one surviving node is the only accessible backup for a region, the region remains available while that node stays online. Losing it can remove the last serving copy even though the primary failed earlier. The matrix makes that dependency visible. All copies offline means no available online copy, even if local bytes still exist on disconnected machines.

Individual retention switches are proposed-policy experiments confined to this model. The real protocol presently applies a world-wide policy, including propagation of the applied setting. Exposing an individual live opt-out would need defined transfer, acknowledgement, capacity, and availability behavior. It should not be implemented by changing a label in the dashboard while leaving the game unaware.

FIGURE: simulation

Figure 4. Actual browser-model screenshot after all backups are disabled and Peer A disconnects. Five regions are unavailable. This is model evidence and remains separate from actual process trials.

## Chapter 10 Implementation walkthrough and integration decisions

### 10.1 Build baseline and source layout

The pinned editor version is Unity 6000.5.2f1. The game uses world recipe 02, network protocol 03, and telemetry schema 2. Packages/manifest.json lists Unity built-in modules required for audio, IMGUI, image conversion, JSON serialization, and physics. The external admin uses Node built-in HTTP, filesystem, path, URL, and crypto facilities rather than a third-party web framework. This small dependency surface makes local setup and source inspection straightforward.

Core classes live under Assets/Scripts/Core. World contains the streamed mesh builder, Player contains the Explorer controller, Networking contains PeerNetwork, and Monitoring contains the local control reader and telemetry writer. Assets/Editor/ProjectSetup.cs supplies scene creation, validation, and build commands. AdminPanel/dist holds browser code, including the isolated model. Tools contains the PowerShell trial runners and research-report builder.

### 10.2 Startup sequence

Startup reads executable arguments, chooses a world seed, generates an in-memory identity, and disables normal persistence for smoke mode. Normal mode acquires a save slot and loads compatible saved records/identity. The peer network initially constructs a local-only ring, then the admin reader can apply the newest local world policy. A compatible saved catalog restores manifests and owner records before normal runtime behavior continues.

The composition then creates materials, light, terrain manager, character controller, camera, and marker/avatar integration. The spawn uses X=0 and Z=-90 with an elevated analytical starting height. After initial terrain tiles exist, physics transforms synchronize and a terrain-layer-only downward raycast locates ground. This layer restriction matters: an unrestricted raycast can hit the player's own controller or an unrelated object and produce misleading grounding evidence.

If the executable receives a port argument, it starts networking. A supplied direct address/remote port then sets the bootstrap destination. Smoke mode starts its timed coroutine with optional beacon placement, screenshot, report path, and expected-peer requirement. Those parameters are test instrumentation, not normal player controls, and smoke persistence is intentionally disabled.

### 10.3 Runtime frame integration

Each frame updates the direct peer network using the current pose. The game polls admin settings before network processing so a recent local choice can propagate through outgoing packets. The world independently refreshes desired tiles when the observer changes tile or a periodic check notices an availability/LOD condition. Marker/avatar objects reflect stored beacon data and known remote peer state. Expired or pruned data removes corresponding display objects rather than leaving stale markers indefinitely.

The controller handles capture toggling, mouse orientation, normalized horizontal input, sprint selection, gravity, jumping, and safety checks. It limits movement integration steps and consults CanStand for both current and target terrain. If the current region is unavailable, it returns with vertical motion cleared. A separate fallback restores spawn if the player falls sufficiently below analytical terrain or leaves the supported map. These checks complement the collider system; they are not a general anti-cheat authority model.

### 10.4 Policy application pseudocode

CODE:
ApplyBackupPolicy(enabled, revision, authority)
    reject invalid revision range or authority syntax
    reject tuple older than the applied tuple
    accept newer tuple and clear saved-state claim
    update catalog retention policy
    prune forbidden remote live state when OFF
    append an operator-visible policy event
ENDCODE

The tuple orders revision numerically and authority ordinally for equal revisions. A valid tuple must have a positive bounded revision and a GUID-shaped authority. The comparison suppresses stale updates. It does not authorize the sender to act as an administrator. Any future secure policy system must establish who may issue an update and what happens when legitimate authorities disagree, rather than rely on the syntax of a GUID.

Clearing the saved digest after a policy change avoids continuing to advertise an old save as if it already matched the newly selected retention state. Subsequent successful saves can establish a fresh local saved fingerprint when allowed. The UI can then show a pending policy, a current applied policy, a matching memory fingerprint, and a save result as separate facts.

### 10.5 Catalog refresh pseudocode

CODE:
RefreshCatalog(ring, activePeers)
    for every region
        nextOwner = first preferred ring owner
        if previous owner departed and
           (backups OFF or no retained local copy)
            pin previous owner as missing
        if pinned owner has returned
            clear the missing condition
        record next ring preference
        generate own manifest only if region is eligible
        when OFF remove non-primary retained manifests
ENDCODE

The missing-owner branch is essential. Without it, recomputing a local-only ring after a remote departure could make the survivor the new primary and immediately regenerate every seed, contradicting the requested OFF behavior. Saving the missing-owner records preserves this rule after a restart. A valid received manifest may later clear the condition when retention permits merging, but merely toggling a boolean does not recreate absent changes.

### 10.6 Terrain safety and smoke evidence

The selected smoke report records player-feet height and terrain-hit height independently. Both clients report an above-ground condition, not just a successful mesh allocation. The final terrain-only raycast avoids treating a self-collider hit as ground. The recorded difference of approximately 0.080 metres is an observed test value at the selected spawn state, not a global terrain clearance specification.

Smoke success also requires sufficient loaded terrain, supported bounds, and the expected peer/beacon state. This makes the runner more useful than checking whether two executable windows opened. It inspects runtime outcomes and corresponding logs, then cleans up the specific child processes it started. A process that needs cleanup after producing a valid functional report should be reported as a shutdown behavior rather than silently converted into proof of graceful exit.

### 10.7 Compatibility and change control

Geometry changes should receive deliberate recipe treatment. Altering bounds, regional seed derivation, height sampling, roads, or coordinate interpretation can change an existing world's terrain. Network changes can alter packet compatibility or backup semantics. Save changes can alter the meaning of owner records. The three version fields are related but distinct, and a change should not blindly increment all of them without understanding the affected contract.

Recipe-01 saves remain separate rather than being silently imported into the resized battlefield. This avoids reinterpreting old coordinates and identities under a different map extent. Future migration should specify coordinate conversion, retained edit semantics, old-file preservation, and rollback behavior. Merely loading old JSON because field names still parse would not constitute a correct migration.

## Chapter 11 Validation results and interpretation

### 11.1 Evaluation design

Evaluation combines the source-defined behavior, recorded Unity assertions, recorded actual two-client trials, automated Node tests, and browser inspection. Each layer has a different purpose. A class-level assertion can exercise a deterministic rule. An actual process trial checks component integration and timeout behavior. A browser inspection confirms whether the operator can see the expected evidence and use a control. None by itself supplies a broad performance or reliability benchmark.

The selected game trials ran as two processes on a single Windows laptop using loopback connections. A full hardware inventory, timing trace, controlled loss model, independent disk-failure scenario, and randomized repetition design were not recorded. The report therefore avoids statistical failure probabilities, sustainable player-count claims, latency percentiles, speedup percentages, or measured FPS. The evidence is sufficient for a bounded functional conclusion.

### 11.2 Recorded Unity validation

Builds/validation.txt reports 631 assertions for the pinned battlefield baseline. Several assertions arise from loops over samples or ring cases. The number is not a count of 631 independent experiments. The validator covers map/seed/tile calculations, seam comparisons, hash-ring behavior, beacon merging, real loopback sockets, policy propagation, owner loss, stale-policy rejection, and missing-owner restore behavior.

| Check family | Representative behavior | Recorded evidence |
| --- | --- | --- |
| Geometry addresses | Exact endpoints and full tile coverage | Validator result and source |
| Regional seeds | Distinct seeds for tested world 2026 | Validator loop |
| Border compatibility | Neighboring region seam samples | Mesh comparison assertions |
| Mixed detail | Shared interpolated boundary | LOD edge assertions |
| Hash ring | Input-order independence and restricted remapping | Ring sample loops |
| Beacon state | Invalid input, repeat merge and digest behavior | Store assertions |
| Direct network | Compatible peers and world isolation | Actual loopback socket checks |
| Late join | More than one outgoing state page | Multi-page recovery check |
| Policy propagation | Remote applies ON/OFF revision | Peer-network assertions |
| Owner loss | ON retains and OFF removes/gates | Failure-path assertions |
| Restore | Missing owner survives catalog reload | Restoration assertion |
| Policy ordering | Old revision cannot override new | Revision comparison assertion |

The documentation update does not rerun Unity or rebuild the game merely to claim a new validation date. The named report remains a recorded artifact from the earlier verified build. Source hashes in the evidence package help connect that record to the implementation inspected for the study. New game-code changes would require new validation rather than treating the old report as permanently valid.

### 11.3 Actual two-client smoke result

| Measure | Client A | Client B |
| --- | --- | --- |
| Passed | true | true |
| Above-ground check | true | true |
| Loaded terrain tiles | 121 | 121 |
| Known remote peers | 1 | 1 |
| Shared beacons | 1 | 1 |
| Received packets | 123 | 126 |
| Player-feet Y | 95.150856 m | 95.150856 m |
| Terrain-hit Y | 95.070831 m | 95.070831 m |

The reports are Builds/Smoke/b787309b-peer-a.json and the matching peer-b file. They show that the tested clients generated local terrain, remained above its collider surface, recognized a remote, and shared a beacon. Packet totals are observed counts within the smoke interval, not a throughput measurement. One development process required runner cleanup after the functional report, a separate lifecycle detail retained in the verification notes.

### 11.4 Actual inventory and owner-loss result

Before failure in the inspected ON case, one peer reported 16 primary and 26 backup manifests; the other reported 26 primary and 16 backups. Each therefore advertised all 42 regional manifests, and all regions had two fresh reported holders. The runner also checks OFF inventories, requiring no non-primary retained copies and consistent held counts. These assertions use actual holder arrays rather than preferred secondary assignments.

| Policy | Departed-owner regions | Unavailable after expiry | Survivor loaded tiles | Save status |
| --- | --- | --- | --- | --- |
| OFF | 12 | 12 | 110 | Access denied in sandbox |
| ON | 16 | 0 | 121 | Access denied in sandbox |

The selected record is Builds/BackupPolicy/78b4b1a9-results.json with its two pre-failure inventory files. Different peer identities produced different ownership counts across policy cases. The meaningful comparison is conditional: OFF unavailability equals that run's departed-owner count, while synchronized ON copies preserve availability for the regions that peer owned. A direct comparison of 110 and 121 loaded tiles would mix position/availability with rendering and is not evidence of performance improvement.

Both save errors are material. The result establishes live retention and availability behavior after an actual process termination. It does not establish a successful disk backup, independent-machine resilience, a cold restart after total process loss, or recovery from power failure. Reporting that limitation is part of interpreting the experiment correctly, not a reason to discard the valid live behavior it observed.

### 11.5 Node tests and browser checks

| Test area | What the check establishes | What it does not establish |
| --- | --- | --- |
| Snapshot collection | Multiple observers, stale ages and malformed handling | Complete remote-machine collection |
| HTTP routes | Real assets and expected route behavior | Internet deployment hardening |
| Live command persistence | Ordered local policy writes and origin checks | Authenticated global administration |
| ON model failure | Received synchronized copies retain availability | Real transport or disk reliability |
| OFF model failure | Replicas prune and missing state stays missing | Full gameplay deletion semantics |
| Individual model switches | Proposed retention behavior and last-holder loss | Live per-peer policy support |
| Reset/all offline | Independent fixture state and online-copy availability | Machine boot/recovery behavior |
| Ring ordering | Deterministic fixture placement | Perfect balance for every peer set |
| Holder evidence | No inferred copies from assignments/stale peers | Trustworthy remote disk proof |

The recorded Node suite contains nine passing tests. Browser checks exercised individual model retention, ON/OFF comparison, reconnection, exact live inventory selection, and the 42-region map. The operator view correctly displayed matching memory alongside nonmatching/failed-save evidence. No usability survey, error-rate study, or third-party operator evaluation was performed, so the UI result remains a functional inspection.

### 11.6 Research-question findings

RQ1 is supported within the tested recipe/build by address coverage and seam checks. The shared boundary construction and the mixed-LOD interpolation explain the result rather than attributing it to independent random regions. RQ2 is supported by the selected live owner-loss outcomes, conditional on received surviving copies. RQ3 is supported by source-level evidence construction and inspected views that distinguish assignments, manifests, current memory, and save status.

The findings support the stated prototype objectives. They do not support the stronger conclusion that a seamless infinite MMO has been completed. The world is finite, the network is trusted, mutable state is simple, copy acknowledgements are absent, and the measured environment is local. The development roadmap follows from these specific gaps rather than presenting unrelated features as proof of success.

### 11.7 Threats to validity

Internal validity is limited by a small developer-authored test selection and incomplete instrumentation. The validator can miss untested seeds or scheduling cases. Live trials do not inject controlled packet loss or partitions. Save-access errors mean the primary failure test cannot answer the durability question. Different identities and ownership distributions prevent a matched quantitative comparison across the two selected policies.

External validity is limited by the one-laptop topology, Windows/Unity baseline, and simple beacon workload. Multiple local processes share the same operating system, power, storage, and network stack. They are not independent failure domains. Combat events, real construction edits, larger peer sets, wide-area routes, and adversarial participants may require substantially different authority, transport, and persistence mechanisms.

Construct validity depends on naming measurements correctly. A seed manifest is not a terrain mesh; an assigned backup is not receipt; a digest match is not a disk acknowledgement; heartbeat age is not RTT; a frame-rate target is not measured FPS. The report and panel preserve these distinctions because a misleading label can turn a correct small observation into an invalid larger claim.

## Chapter 12 Operating procedures and maintenance

### 12.1 Installation and file layout

The supplied Windows executable does not require the Unity editor to play. It does require the accompanying UnityPlayer DLL, data directory, and other build files. Copying only the EXE can make the build fail before any game logic runs. Editing or rebuilding requires the pinned editor and appropriate Windows build support. The project root is the directory that contains Assets, Packages, ProjectSettings, AdminPanel, and Tools.

The normal local-admin discovery expects AdminPanel beside Builds, with the executable inside Builds/Windows. Moving only the executable to an unrelated folder changes the inferred project root and can break control-file discovery. A portable distribution should preserve the documented layout or introduce an explicit configuration mechanism in a future release. The current version should not claim that every arbitrary installation location has been validated.

Node is needed for the external admin and its tests. The scripts use ES modules, the built-in test runner, and built-in fetch support. Node 20 or newer is the project baseline documented in the README; the evidence manifest records the actual Node version used when tests were packaged. Python dependencies are needed only to rebuild documentation artifacts, not to play or use the normal Node dashboard.

| Path relative to project | Operational purpose | Change caution |
| --- | --- | --- |
| Builds/Windows | Runnable complete Windows build | Keep build files together |
| Assets/Scenes/Frontier.unity | Generated prototype scene | Reset/build can recreate it |
| AdminPanel/server.mjs | Local HTTP service | Keep bound to intended localhost |
| AdminPanel/controls.json | Generated per-world policy | Prefer browser commands over manual revisions |
| Telemetry | Editor observer files | Old files remain historical |
| Builds/Windows/Telemetry | Standalone observer files | Requires write access |
| Tools | Test and documentation runners | Run from reviewed project checkout |
| Research and Documentation | Papers, report and source/evidence notes | Update claims when evidence changes |

### 12.2 Solo start and movement

Open the complete Windows build or enter Play mode in the supplied Unity scene. Choose Enter the frontier. Movement uses WASD, mouse look, Shift for sprinting, and Space for jumping. E places a survey beacon. Tab or Escape toggles mouse capture and the connection panel. Losing application focus releases capture so an operator can use the browser without continuing unintended mouse-look input.

The normal spawn is near X=0, Z=-90. The renderer prepares nearby terrain and performs terrain-layer grounding. Wait for the local neighborhood to load before treating a low tile count as a fault. If the game reports a missing region and holds the player, check policy and owner status rather than assuming the movement keys are broken. The availability gate can deliberately prevent movement even though the application is still running normally.

### 12.3 Two instances on one laptop

Start two normal game instances with the same seed and compatible build. Separate local save slots normally choose ports beginning at 47770 and 47771. If necessary, enter distinct valid ports in the connection panel. Start LAN mode on both, or direct-connect each instance to 127.0.0.1 and the other's port. Both should report one remote after compatible packet exchange.

CODE:
$game = '.\Builds\Windows\LookUpOn.exe'
$a = '--seed 2026 --port 47770'
$b = '--seed 2026 --port 47771'
$a += ' --connect 127.0.0.1 --remote-port 47771'
$b += ' --connect 127.0.0.1 --remote-port 47770'
Start-Process -FilePath $game -ArgumentList $a
Start-Process -FilePath $game -ArgumentList $b
ENDCODE

These are illustrative PowerShell launch commands for the documented local workflow. They start visible game windows because those windows are the player's interactive work surface. Check that the chosen ports are free. A failure to bind a port is an endpoint configuration problem, not evidence that the world seed or region layout is invalid. Multiple local processes demonstrate networking integration but share a single failure domain.

### 12.4 Separate-machine LAN start

Copy the complete build/project layout to both intended machines. Use the same seed, recipe, and protocol. Direct connection uses the other machine's LAN IPv4 address and listening UDP port. Loopback 127.0.0.1 remains local and cannot address the other laptop. If a firewall prompt appears, use the intended private-network rule rather than disabling the firewall globally.

Record the actual direct topology in a new experiment. Confirm each machine's current observer, peer identity, endpoint, and acknowledged policy. Do not assume that discovering one machine provides a full mesh with every other participant. The current report includes no completed separate-machine results, so this procedure is a reproduction plan rather than a claim that independent-laptop durability has already been achieved.

### 12.5 Admin startup and observer selection

From the project root run node AdminPanel/server.mjs, then open http://127.0.0.1:8765. The foreground service stops with Ctrl+C. AdminPanel/Start-Admin.ps1 can instead launch a hidden service, record its PID/logs, and open the browser. An alternate port can be supplied to the server or launcher. A browser tab that remains open after the service stops may still display last known content, so the connection badge and freshness indicators matter.

Choose the intended fresh observer rather than the first historical row. In a two-client test, identify it by peer ID, port, seed, and source. The Overview view should show the same world seed as the game. Connections and backups should show its direct peer and actual manifest inventory. Replication and saves should be read separately: the existence of a current memory match does not override a save-access error.

FIGURE: admin_overview

Figure 5. Actual external admin overview from the prototype. Region geometry, coordinates, copy evidence and operator controls remain separate from the game window.

### 12.6 Controlled backup demonstration

Use an isolated test world for a destructive owner-loss demonstration. Record current policy revision and wait until both peers acknowledge the chosen policy. With ON, wait until the required inventories show received copies before terminating an owner. With OFF, verify that non-primary replicas are absent. Choose the peer that actually owns at least one region; otherwise terminating it cannot demonstrate loss of owned terrain.

After termination, wait for expiry rather than immediately declaring the result. The observer must advance its game loop, process the timeout, refresh the catalog, and publish telemetry. Compare the number of unavailable regions against the departed owner's pre-failure responsibilities. Inspect player holding and loaded tiles only in the context of that observer's position. Repeat the procedure with a new controlled setup rather than assume that reenabling ON reconstructs already missing edits.

The provided Tools/Test-BackupPolicy.ps1 performs isolated ON and OFF cases using seeds 741902 and 741901. It saves pre-failure inventory evidence and checks both observers before failure. Its optional InspectSeconds argument pauses the connected ON case for dashboard inspection. The runner kills only child processes it created, and it does not change the default world's policy.

### 12.7 Safe interpretation and recovery runbook

| Observation | First interpretation | Appropriate next step |
| --- | --- | --- |
| Browser refuses localhost | Admin service may be stopped | Start service and verify printed port |
| Observer stale | Game may be paused, closed or stalled | Select intended fresh session and check game |
| No remote peers | Endpoint, compatibility or firewall issue | Compare seed/protocol/ports and LAN address |
| Red owned regions after OFF expiry | Deliberate missing-owner policy | Restore a valid owner/holder if retained |
| ON after loss still red | No accessible source of missing state | Do not claim regeneration recovered edits |
| Matching memory, save failed | Volatile copies may exist | Repair save access and verify a completed save |
| Zero backup copies on OFF | Expected retention trim | Check primary copies rather than assuming corruption |
| More than two ON holders | Current full connected-peer replication | Do not label it a strict two-backup policy |
| Different inventory on observers | Aging reports or differing local view | Compare freshness, identities and membership |
| Port cannot listen | Port occupied or invalid | Use a reviewed free port or close intended old instance |
| New window has new identity | Slot/save may differ or fail | Check selected save slot and save status |
| Player does not move over missing region | Safety hold is active | Check availability message and ownership |
| Shader or compile errors | Build/import issue | Inspect Unity Console with pinned editor |
| Admin command stays pending | Game did not acknowledge new revision | Check folder, control access and active frames |
| All copies on one laptop | No machine-failure independence | Add separate-machine trials |

Recovery should start with evidence collection rather than deleting files. Export the selected snapshot, identify the world and policy, retain the named save/error status, and determine whether any valid holder remains. A missing copy, a failed save, and an unreadable snapshot are different faults. Deleting a save or resetting a seed can destroy the very evidence needed to explain what happened and should not be presented as a general recovery command.

### 12.8 Rebuild validation and change maintenance

In Unity, Validate prototype runs the recorded class of checks. Build Windows prototype recreates the generated scene, validates, and creates the development build. Preserve custom scene changes first. Dashboard/model tests run through node --test AdminPanel/server.test.mjs AdminPanel/simulation.test.mjs. The two-peer smoke runner exercises terrain grounding and beacon exchange. The backup runner needs a running admin service and normal child clients.

Keep reports from different runs separate. A random run prefix identifies smoke and lifecycle outputs. An old passing file should not be mistaken for the result of a new source revision. When modifying game semantics, rerun the affected checks, retain unsuccessful outcomes, and update the research/report tables only after verifying the actual new artifacts. For a documentation-only change, preserve the distinction between rereading recorded evidence and rerunning a game experiment.

Deployment maintenance should preserve runtime data deliberately. Controls and telemetry are generated files, not hardcoded truths to commit as source. A project archive may omit generated build/log directories, so a researcher needs the selected evidence copies and hashes when distributing a report. The existing evidence manifest addresses that limited provenance need; it does not sign a public release or provide independent storage attestation.

## Chapter 13 Learning outcomes discussion and future development

### 13.1 Engineering learning demonstrated by the artifact

The first learning outcome is the separation of generated content from mutable state. A seed and coordinate function can reconstruct terrain, but the project must store a beacon event separately. This distinction appears in code, the save format, the backup policy, and the operator display. It is more useful than a general assertion that procedural generation makes a world lightweight or permanent.

The second outcome is understanding the difference between placement and possession. A consistent-hash ring selects a preferred owner from a local membership set. The catalog records received data and unresolved ownership. A holder array gives reported receipt evidence, while a saved digest gives a separate claim about a save. The project demonstrates that these objects cannot be collapsed into a single green backup indicator without losing important meaning.

The third outcome concerns runtime integration. Mesh LOD, seam constraints, colliders, the player controller, expiry, and telemetry all interact. A player can be above analytical height yet fail a collider check if the ray hits the wrong layer. A region can be missing while the renderer remains technically capable of generating it. Correct integration requires callbacks and measured conditions shared across components rather than isolated success in each class.

The fourth outcome is evidence interpretation. The team can explain why a functional local result is valid while a durability claim remains unsupported. Save-access errors should remain visible beside successful live retention. A target frame rate is not a benchmark. A deterministic model is not a packet-loss experiment. The report's matrices and named artifacts make these distinctions available for evaluation and further work.

### 13.2 Limits of the present architecture

The trusted-LAN assumption permits false peer claims. There is no authenticated identity, encrypted session, signed administration, anti-cheat system, or proof that a remote disk stored a record. GUID syntax and digest equality do not supply those properties. The operator service binds locally, and host/origin checks should not be mistaken for an internet authorization design.

Responsibility remains local to membership views. There are no owner epochs, quorum commits, or stale-writer fences. A fixed timeout can mistake a delayed or partitioned peer for a departed one. The OFF policy deliberately prunes data, and a full bounded beacon store can diverge under different admissions. These are concrete implementation constraints, not generic warnings unrelated to the prototype.

Rendering work remains on the main thread, with allocations and collider creation that have not been benchmarked on target hardware. The map has no combat collision on its decorations or complete gameplay loop. The current terrain is a height field with fixed bounds. None of these conditions prevents an exploration demonstration, but all affect the scope of the next development phase.

### 13.3 Prioritized development roadmap

| Priority | Proposed change | Acceptance evidence needed |
| --- | --- | --- |
| 1 | Per-region snapshots/edit logs and durable copy acknowledgement | Acknowledged data survives independent-machine crash/restart |
| 2 | Explicit replica factor and transfer progress | Under-replicated, copying, healthy and recovering states reflect receipt |
| 3 | Ownership epochs and partition policy | Controlled split/rejoin cannot silently accept stale authority |
| 4 | Transport sizing and loss/congestion behavior | Measured delivery/catch-up under declared network conditions |
| 5 | Profiling and generation jobs/pooling | Repeated frame-time, memory and allocation comparisons |
| 6 | Terrain/decor collision and focused combat loop | Traversal, damage, objective and spectator tests |
| 7 | Authenticated multi-network connectivity | Verified identity/policy rejection and NAT/relay scenarios |

The sequence prioritizes data and authority semantics before adding many gameplay mutations. A weapon, building system, or destructible object increases the consequences of ambiguous ownership. Likewise, moving beyond a trusted LAN increases the consequences of unverified identity and transport behavior. The roadmap should therefore be read as dependencies rather than a claim that adding visual content alone completes the distributed world.

### 13.4 Proposed research experiment design

A stronger follow-up study should use at least three separate machines, record hardware and build identities, fix or deliberately randomize peer IDs/topology, and declare a repetition plan before collecting results. It should vary packet loss, delay, jitter, abrupt crash, graceful leave, partition, simultaneous failures, and cold restart. The test should retain raw logs and unsuccessful trials rather than only publish the best-looking run.

Metrics should include received/durable copy versions, transfer delay, time without region availability, ownership changes, false failure declarations, recovered mutation count, per-peer traffic, and frame-time/memory distributions. Several metrics require new instrumentation and acknowledgement semantics before they can be measured meaningfully. Their inclusion in a future protocol table does not mean that values have already been collected.

Comparisons should hold workload and failure domains constant. Full connected-peer replication and a specified replica factor could then be compared on copy cost, catch-up, and observed recovery. The map partition count could be compared under the same exploration traces. A future study should report both spatial coverage and player/network workload so a larger map is not used as a substitute for a scalability result.

### 13.5 Conclusion

LookUpOn implements a bounded procedural battlefield with separate region responsibility, rendering tiles, peer-held manifests, mutable survey beacons, and an external operator plane. The selected actual trials exhibit the intended ON/OFF owner-loss behavior. The admin exposes direct links and reported copies, and the isolated model makes last-holder effects reproducible without changing real player saves.

The engineering contribution is the integration and evidence workflow. The prototype establishes a clear foundation for durable region data, stronger authority management, performance evaluation, and gameplay. Its current claims remain limited to the tested local behavior. The most valuable next step is to strengthen and independently measure the data/ownership contract before describing the world as durable, secure, or internet-scale.

## References

[1] J. Togelius, G. N. Yannakakis, K. O. Stanley, and C. Browne. Search-based procedural content generation: A taxonomy and survey. IEEE Transactions on Computational Intelligence and AI in Games, 3(3), 172–186, 2011. https://www.um.edu.mt/library/oar/handle/123456789/29609

[2] D. Karger, E. Lehman, T. Leighton, M. Levine, D. Lewin, and R. Panigrahy. Consistent hashing and random trees: Distributed caching protocols for relieving hot spots on the World Wide Web. ACM STOC, 654–663, 1997. https://people.csail.mit.edu/karger/Papers/web.pdf

[3] A. Bharambe, J. Pang, and S. Seshan. Colyseus: A distributed architecture for online multiplayer games. USENIX NSDI, 2006. https://www.usenix.org/legacy/event/nsdi06/tech/full_papers/bharambe/bharambe_html/

[4] G. DeCandia et al. Dynamo: Amazon's highly available key-value store. ACM SOSP, 2007. https://www.amazon.science/publications/dynamo-amazons-highly-available-key-value-store

[5] M. Shapiro, N. Preguica, C. Baquero, and M. Zawirski. Conflict-free replicated data types. SSS, 2011. https://perso.lip6.fr/Marc.Shapiro/papers/2011/CRDTs_SSS-2011.pdf

[6] A. Das, I. Gupta, and A. Motivala. SWIM: Scalable weakly-consistent infection-style process group membership protocol. DSN, 303–312, 2002. https://www.cs.cornell.edu/projects/Quicksilver/public_pdfs/SWIM.pdf

[7] L. Eggert, G. Fairhurst, and G. Shepherd. UDP Usage Guidelines. RFC 8085, 2017. https://www.rfc-editor.org/rfc/rfc8085

[8] Unity Technologies. Mesh scripting reference, Unity 6000.0. https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/mesh

[9] I. Stoica, R. Morris, D. Karger, M. F. Kaashoek, and H. Balakrishnan. Chord: A scalable peer-to-peer lookup service for Internet applications. ACM SIGCOMM, 2001; author-hosted manuscript. https://people.csail.mit.edu/karger/Papers/chord.pdf

[10] S. Gilbert and N. Lynch. Brewer's conjecture and the feasibility of consistent, available, partition-tolerant web services. ACM SIGACT News, 33(2), 51–59, 2002. https://groups.csail.mit.edu/tds/papers/Gilbert/Brewer6.pdf

[11] D. Ongaro and J. Ousterhout. In search of an understandable consensus algorithm. USENIX ATC, 305–319, 2014. https://www.usenix.org/node/184041.

[12] G. Fairhurst, T. Jones, M. Tuxen, I. Rungeler, and T. Volker. Packetization Layer Path MTU Discovery for Datagram Transports. RFC 8899, 2020. https://www.rfc-editor.org/rfc/rfc8899.html

[13] SR University. School of Computer Science and Artificial Intelligence. Official school page, accessed 8 November 2026. https://sru.edu.in/academics/schools/school-of-cse-ai/

The external references supply background and institutional context. All project measurements in Chapter 11 come from the named local reports. Original source files, the short paper, the current README, and the management documents provide the remaining implementation and submission context. Online-source access details are retained in the research source ledger.


## Appendix A Data dictionaries and interpretation

The dictionaries below describe the actual serializable classes and local service contract in this baseline. A field name is not an independent storage guarantee. Units and interpretation boundaries accompany the raw types so an integrator can distinguish region indices, tile counts, times, fingerprints and administrative revisions. The schema number identifies telemetry compatibility rather than a semantic commitment to every future field.

### A.1 Observer snapshot identity and geometry

| Field | Type or unit | Meaning and boundary |
| --- | --- | --- |
| schema | Integer 2 | Telemetry compatibility; old schema 1 is historical |
| protocol | Integer 3 | Game network compatibility |
| worldVersion | Integer 2 | Terrain recipe compatibility |
| updatedUtc | UTC timestamp | Publisher timestamp, not measured latency |
| peerId | GUID string | Selected local observer identity claim |
| seed | Signed integer | World selection |
| status | String | Local network status text |
| udpPort | Integer port | Local listening port when active |
| networking | Boolean | Socket-running status |
| travelLimitMetres | Metres, 5000 | Half extent, not full map side |
| mapSizeMetres | Metres, 10000 | Full supported horizontal side |
| regionColumns | Integer 7 | Region grid columns |
| regionRows | Integer 6 | Region grid rows |
| tilesPerRegion | Integer 64 | Eight by eight tile subdivision |
| totalTiles | Integer 2688 | Full addressable tile count |
| regionSizeX / regionSizeZ | Metres | Rectangular region dimensions |
| tileSizeX / tileSizeZ | Metres | Rectangular rendering-tile dimensions |
| loadedChunks | Tile count | Loaded rendering tiles despite legacy field name |
| pendingChunks | Tile count | Queued generation/LOD work |

The legacy names loadedChunks and chunkX/chunkZ need careful interpretation after the hierarchy change. loadedChunks counts rendering tiles. In PeerView, chunkX/chunkZ carry ownership-region coordinates. Integrators should use the explicit schema and geometry fields rather than assume that the same word chunk always denotes the same spatial level. A future schema could rename these fields, but current consumers must preserve their actual meaning.

### A.2 Observer state save control and traffic

| Field | Type or unit | Meaning and boundary |
| --- | --- | --- |
| beaconCount | Integer 0–4096 | Current local store count |
| beaconLimit | Integer 4096 | Configured admission capacity |
| stateDigest | SHA-256 hex | Canonical local in-memory beacon fingerprint |
| savedDigest | Hex or empty | Reported completed local save fingerprint |
| savePath | Local path | Observer machine path, not downloadable disk proof |
| saveStatus | String | Success/error description |
| lastSavedUtc | UTC or empty | Last reported completed save time |
| backupFileExists | Boolean | Active-policy view of previous local archive presence |
| backupsEnabled | Boolean | Applied world policy |
| backupRevision | Ordered integer | Applied control revision |
| backupAuthority | GUID string | Equal-revision authority tie-breaker |
| adminControlStatus | String | Local control-file reader status |
| bytesIn | Byte counter | Accepted network traffic accounting |
| bytesOut | Byte counter | Attempted successful send accounting |
| peers | PeerView array | Local observer plus known remotes |
| regions | RegionView array | All 42 region records |
| events | String array | Most recent local membership/policy events |

The server adds snapshotAgeSeconds and source when collecting a file. It also returns serverUtc, sessions, unreadableSnapshots and controls at the API-response level. The browser uses these collection values together with peer ages to label current or stale evidence. They are service-enrichment fields and should not be confused with fields that the Unity class itself serializes.

### A.3 Peer record

| Field | Type or unit | Meaning and boundary |
| --- | --- | --- |
| id | GUID string | Identity claim of local/remote peer |
| endpoint | String | Local marker or observed IPv4 and port |
| local | Boolean | Whether this record is the selected observer |
| x / y / z | Metres | Reported player pose |
| chunkX / chunkZ | Region indices | Ownership region, not tile indices |
| ageSeconds | Seconds | Remote heartbeat age at publisher |
| beacons | Integer count | Advertised total beacon store |
| packets | Integer count | Observer-received packet count for this peer |
| stateDigest | Hex string | Advertised memory fingerprint |
| savedDigest | Hex or empty | Advertised saved fingerprint |
| seedCopies | Integer count | Advertised manifest count |
| backupsEnabled | Boolean | Advertised applied policy |

### A.4 Region record

| Field | Type or unit | Meaning and boundary |
| --- | --- | --- |
| x / z | Region indices | Grid coordinate 0–6 and 0–5 |
| seed | Signed integer | Expected deterministic regional seed |
| minX / maxX | Metres | Region X boundaries |
| minZ / maxZ | Metres | Region Z boundaries |
| localBeacons | Integer count | Observer store records located here |
| loadedTiles | Integer 0–64 | Observer-rendered tiles in this region |
| available | Boolean | Observer catalog availability |
| missingOwner | GUID or empty | Pinned unresolved prior owner |
| owners | GUID array | Primary followed by permitted ring preferences |
| holders | GUID array | Local retained record and recent valid remote claims |

A region holder list is a manifestation of the observer’s recent evidence. A preferred secondary may be absent because it has not reported a copy. A disconnected holder may still have local bytes while being unable to serve them. A stale observer makes all its evidence historical. Therefore a consumer should not combine old arrays from several unrelated sessions and call the union a verified global inventory.

### A.5 Save and control documents

| Field | Type or unit | Meaning and boundary |
| --- | --- | --- |
| SaveData.version | Integer 2 | Save-format discriminator |
| SaveData.worldVersion | Integer 2 | World recipe for saved content |
| SaveData.seed | Signed integer | World to which snapshot belongs |
| SaveData.peerId | GUID string | Stable identity if saved/loaded successfully |
| SaveData.backupsEnabled | Boolean | Saved policy flag; not consensus metadata |
| SaveData.beacons | Beacon array | Permitted mutable records |
| SaveData.regionSeeds | Manifest array | Permitted retained seed records |
| SaveData.regionOwners | 42 strings | Owner/missing-owner restoration context |
| ControlDocument.worlds | Control array | Per-world entries in local file |
| BackupControl.seed | Signed integer | Command target world |
| BackupControl.backupsEnabled | Boolean | Requested policy |
| BackupControl.revision | Positive bounded integer | Policy order |
| BackupControl.authority | GUID string | Tie-breaker, not authentication |

## Appendix B Reference catalogs and illustrative messages

### B.1 Regional seed catalog for world 2026

This catalog is computed from the documented recipe function. It supplies a reproducible address/seed reference rather than a snapshot of current live ownership. The region ID remains unique within the 42-cell grid, while the signed seed is generated metadata. The validator separately checks distinct seeds for this tested world.

| ID | Region X | Region Z | Seed for world 2026 |
| --- | --- | --- | --- |
| 0 | 0 | 0 | -1181038184 |
| 1 | 1 | 0 | 809283719 |
| 2 | 2 | 0 | -748647594 |
| 3 | 3 | 0 | -638698947 |
| 4 | 4 | 0 | 2119125388 |
| 5 | 5 | 0 | 1959352203 |
| 6 | 6 | 0 | 938304698 |
| 7 | 0 | 1 | -1164260565 |
| 8 | 1 | 1 | 792506100 |
| 9 | 2 | 1 | -731869975 |
| 10 | 3 | 1 | -655476566 |
| 11 | 4 | 1 | 2135903007 |
| 12 | 5 | 1 | 1942574584 |
| 13 | 6 | 1 | 955082317 |
| 14 | 0 | 2 | -1147482946 |
| 15 | 1 | 2 | 842838957 |
| 16 | 2 | 2 | -782202832 |
| 17 | 3 | 2 | -672254185 |
| 18 | 4 | 2 | -2142286670 |
| 19 | 5 | 2 | 1992907441 |
| 20 | 6 | 2 | 904749460 |
| 21 | 0 | 3 | -1130705327 |
| 22 | 1 | 3 | 826061338 |
| 23 | 2 | 3 | -765425213 |
| 24 | 3 | 3 | -689031804 |
| 25 | 4 | 3 | -2125509051 |
| 26 | 5 | 3 | 1976129822 |
| 27 | 6 | 3 | 921527079 |
| 28 | 0 | 4 | -1113927708 |
| 29 | 1 | 4 | 742173243 |
| 30 | 2 | 4 | -681537118 |
| 31 | 3 | 4 | -705809423 |
| 32 | 4 | 4 | 2052014912 |
| 33 | 5 | 4 | 2026462679 |
| 34 | 6 | 4 | 871194222 |
| 35 | 0 | 5 | -1097150089 |
| 36 | 1 | 5 | 725395624 |
| 37 | 2 | 5 | -664759499 |
| 38 | 3 | 5 | -722587042 |
| 39 | 4 | 5 | 2068792531 |
| 40 | 5 | 5 | 2009685060 |
| 41 | 6 | 5 | 887971841 |

### B.2 Minimal record examples

The examples below are illustrative schema values, not a captured player event or an instruction to overwrite a live control file. GUID-shaped strings are deliberately fixed examples. A valid seed value must match the configured recipe and region. A control request should let the server generate the revision rather than reuse a copied example revision.

CODE:
Beacon record:
{
  "id": "11111111111111111111111111111111",
  "author": "22222222222222222222222222222222",
  "x": 50.0,
  "z": -90.0
}

Region manifest:
{ "region": 17, "seed": -672254185 }

World control command body:
{ "seed": 2026, "backupsEnabled": false }
ENDCODE

A network receiver must not accept a record simply because its JSON parses. It checks identity shape, finite position, supported bounds, expected manifest seed, compatibility and collection bounds. The saved and current digest refer to canonical beacon collections; adding whitespace to JSON is not the basis of their equality. A report consumer should preserve these distinctions when building new tools.

### B.3 Status interpretation examples

| Situation | UI evidence | Correct conclusion |
| --- | --- | --- |
| One peer reports 42 manifests | Inventory count and holder arrays | It claims those manifests; disk remains separate |
| Memory matches but save does not | Different memory/save columns | Current reported state agrees; saved state unconfirmed |
| Another peer is a ring preference only | Owners contains ID, holders does not | No manifest receipt evidence for that peer |
| Policy request is newer than snapshot | Pending command text | Selected game has not yet acknowledged it |
| Snapshot is old | Stale badge and age | Values are historical rather than live |
| OFF peer expires | Missing-owner regions and pruned changes | Requested availability policy has taken effect |
| ON is enabled after last online copy loss | Regions remain missing | Retention cannot invent absent data |

These cases are chosen because they can produce superficially similar visual symptoms. Correct diagnosis requires reading the relevant evidence column rather than interpreting every nonmatching state as packet loss or every red region as a mesh-generation defect.

## Appendix C Selected implementation source listings

These listings reproduce short, central project classes for audit and explanation. The runnable source files remain authoritative. Long lines may wrap in the report for readability; use the actual project files for compilation. The listings document implementation choices rather than introduce new experimental evidence.

### C.1 Responsibility ring

Source: Assets/Scripts/Core/HashRing.cs. The class sorts 64 virtual positions per peer, selects clockwise distinct owners, and treats replica count as a preference count. It does not contain a distributed routing table or a global membership protocol.

CODE:
using System;
using System.Collections.Generic;
using System.Linq;

namespace LookUpOn.Core
{
    public sealed class HashRing
    {
        readonly List<KeyValuePair<uint, string>> nodes = new List<KeyValuePair<uint, string>>();
        public void SetPeers(IEnumerable<string> peers)
        {
            nodes.Clear();
            foreach (string peer in peers.Distinct().OrderBy(p => p, StringComparer.Ordinal))
                for (int i = 0; i < 64; i++) nodes.Add(new KeyValuePair<uint, string>(WorldMath.Hash(peer + ":" + i), peer));
            nodes.Sort((a, b) => { int c = a.Key.CompareTo(b.Key); return c != 0 ? c : StringComparer.Ordinal.Compare(a.Value, b.Value); });
        }
        public string[] Owners(int x, int z, int replicas = 3)
        {
            if (nodes.Count == 0 || replicas <= 0) return Array.Empty<string>();
            uint key = WorldMath.Hash(x + ":" + z);
            int start = nodes.FindIndex(n => n.Key >= key);
            if (start < 0) start = 0;
            var result = new List<string>();
            for (int i = 0; i < nodes.Count && result.Count < replicas; i++)
            {
                string id = nodes[(start + i) % nodes.Count].Value;
                if (!result.Contains(id)) result.Add(id);
            }
            return result.ToArray();
        }
    }
}
ENDCODE

### C.2 Catalog and beacon implementation trace

RegionCatalog.cs and BeaconStore.cs remain in Assets/Scripts/Core alongside the reproduced ring. Chapter 8 explains the catalog arrays, the missing-owner gate and the restore rules. Chapter 7 explains beacon identity, deterministic conflict selection and digest construction. The following trace connects those descriptions to the authoritative source without repeating the complete implementation.

| Source | Entry point | State and effect |
| --- | --- | --- |
| RegionCatalog.cs | Refresh | Rebuilds membership and primary assignments while preserving missing-owner context |
| RegionCatalog.cs | Refresh and Merge | Generates local primary manifests or validates received manifests against the deterministic recipe |
| RegionCatalog.cs | Records and Restore | Exports held records and restores compatible saved ownership context |
| BeaconStore.cs | Merge | Resolves a duplicate identity by ordinal author then numeric X/Z ordering |
| BeaconStore.cs | Items | Exposes the bounded current record set for synchronization and saving |
| BeaconStore.cs | Digest | Sorts and fingerprints normalized record content for reconciliation |

The ring listing is sufficient to audit the selected placement rule. Reproducing every class would repeat the operational and data-contract explanations without adding experimental evidence. For an exact build or code review, use the versioned project files and the evidence manifest rather than copy text from a report layout.

## Appendix D Reproduction protocol and acceptance matrix

### D.1 Environment record

A new experiment should record the operating system, CPU, GPU, RAM, Unity version, build/source hashes, Node version, world seed, recipe, protocol, peer IDs, ports, direct topology, policy revision/authority, save path/status and failure domains. The selected earlier trials do not contain every one of those measurements. An empty field should remain unmeasured rather than be filled from a guess about the user’s laptop.

Do not change several experimental factors at once unless the design accounts for them. For example, comparing ON with one identity set against OFF with another can verify conditional functional behavior but cannot provide a clean quantitative ownership-balance comparison. A repeatable model fixture is useful for teaching, while real process results need actual pre-failure owner inventories for every trial.

### D.2 Proposed expanded experiment matrix

The following cases are proposed follow-up work. They have no fabricated pass marks. Several require new durable acknowledgement, version, or epoch instrumentation before a meaningful test is possible.

| Proposed case | Controlled condition | Required observation | Status |
| --- | --- | --- | --- |
| Independent-machine crash | Three separate machines | Acknowledged version recovered on survivor | Future |
| Last backup crash | Primary already gone | Availability follows last serving copy | Future live trial |
| Cold restart all nodes | Persisted snapshots only | Recovered IDs, owners and records agree | Future |
| Packet loss sweep | Declared loss percentages | Copy catch-up and false expiry distributions | Future |
| Delay and jitter | Declared network profile | Observed staleness versus local expiry | Future |
| Partition and rejoin | Known disconnected groups | Epoch/fencing and conflict outcome | Requires protocol work |
| Simultaneous failures | Declared independent domains | Retained acknowledged mutations | Future |
| Join during failure | Controlled membership sequence | Transfer progress and owner outcome | Future |
| Full-store divergence | 4096 distinct records per node | Admission and reconciliation semantics | Requires design |
| Corrupt save | Explicit disposable test data | Safe failure and evidence retention | Future |
| Unauthorized policy | Authenticated implementation | Rejected command without state change | Requires security work |
| Forged peer identity | Authenticated implementation | Impersonation rejected | Requires security work |
| Large datagram path | Instrumented MTU tests | No silent packet-size assumption | Future |
| Exploration trace repeat | Same route and hardware | Frame-time/memory distribution | Future |
| Replica-factor comparison | Same workload and topology | Traffic, storage and recovery trade-off | Requires factor protocol |
| Gameplay death | Future health/spectator mode | Storage role continues as designed | Requires gameplay |

### D.3 Acceptance recording procedure

For each proposed case, define the expected invariant and failure condition before running it. Keep raw logs, snapshots, policy revisions and process lifecycle events. Identify the exact point at which a copy becomes durable if the test concerns acknowledged data. A matching digest before a crash is useful context, but recovery must still read the post-restart state and compare the required version/records.

Retain unsuccessful and ambiguous outcomes. A run with an unwritable save directory can still answer a live availability question but cannot answer durable recovery. A run that loses a telemetry file can require inspecting game logs before assigning a failure category. A controlled experimental record should say whether the problem was the application contract, environment configuration, instrumentation or an unresolved combination.

When reporting results, separate configured parameters, derived arithmetic and measured values. State the trial count and independent failure domains. Report percentile distributions only when enough valid observations and a declared measurement method exist. Avoid assigning reliability percentages from one successful trial or describing a maximum admitted-peer constant as a tested sustainable player count.

### D.4 Artifact map

| Artifact | Purpose | Interpretation |
| --- | --- | --- |
| README.md | Entry point and commands | Current operating guide |
| docs/ARCHITECTURE.md | Component and policy description | Implementation-oriented design |
| docs/TESTING.md | Reproduction and evidence limits | Procedures and recorded results |
| AdminPanel/README.md | Admin/model use | Live policy differs from model switches |
| Research/SRU | Updated short paper and primary references | Concise research study |
| Documentation | This report and editable source | Detailed capstone documentation |
| Builds/validation.txt | Recorded validator outcome | 631 assertions in one suite |
| Builds/Smoke | Named client reports/logs | Grounding and synchronization |
| Builds/BackupPolicy | Inventories and owner-loss results | Actual selected lifecycle evidence |
| Presentation | SRU-format deck | Sixteen-slide presentation summary |

The report does not include invented public links for a blog, social post, video, or poster. Those submission artifacts require their own completed content and, for publication, explicit authorization. A company offer/completion certificate is not fabricated for the confirmed university project; the team should confirm applicable submission handling with management.
