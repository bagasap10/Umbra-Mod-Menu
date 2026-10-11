# Architecture and stability work

## Injection flow

`LauncherForm` discovers a verified Risk of Rain 2 process. `Bundle` validates the two DLL hashes, assembly identity and RoR2 build fingerprint from a bounded XML manifest. `InjectionSession` holds an exclusive write lease and persists the attempt marker using PID plus process start time.

`InjectionService` runs the same executable in hidden `--worker` mode. The helper repeats validation before opening the process, invokes `UmbraMenu.Loader.Load`, disposes native resources and exits. The UI does not load the game assembly or run native injection code in its own process. It polls a small status document written by the menu for startup confirmation.

`Loader.Load` only subscribes a callback to `RoR2Application.onUpdate`. The main-thread callback waits for Item, Equipment, Body, Survivor and Buff catalog availability and scene loading, with a three-minute catalog timeout. It initializes one persistent GameObject. `UmbraMenu.Start` reports ready only after the menu is initialized.

A same-build clean unload can be reloaded. An active menu, different build, startup failure or uncertain attempt blocks another injection. A timeout never kills the game and never frees remote pages underneath a potentially running native thread. An uncertain attempt requires either subsequent positive startup confirmation or a game restart. The small amount of deliberately preserved remote memory is reclaimed when that process exits.

## Concrete defects addressed

| Area | Previous risk | Change |
| --- | --- | --- |
| Loader | Loop calling deferred Unity `Destroy` could run forever | Main-thread queued lifecycle and one destruction request |
| Initialization | Remote entry could create objects before game catalogs were available | Wait for loading/catalog availability on game update |
| Dependency resolution | Repeated anonymous resolver registration and null resource stream | Removed embedded Octokit resolver/update polling; no external Harmony dependency |
| Native allocation cleanup | `MEM_DECOMMIT` left reservations behind | `MEM_RELEASE` with size zero; idempotent owners |
| Native threads | Infinite wait and leaked thread handle | Finite wait, instruction-cache flush and handle cleanup |
| Exception pointer | 32-bit read in a 64-bit game, guessed exception layout and object lifetime | Pointer-width check without dereferencing an unrooted exception object |
| Mono calls | Image loading ran before attaching the native thread | Attach after obtaining the root domain, before image loading and later runtime calls |
| Export discovery | One memory read per RVA/ordinal, weak bounds | Batch table reads and bounds/ordinal checks |
| Texture getter | Getter called itself recursively | Cached backing-field read plus regression test |
| Icon assets | Cache could grow with every visited DLC item | 128-entry cache; oldest completed entries released; pending loads bounded |
| Portal assets | Completion could spawn after unload/stage change; repeated pending callbacks | Generation invalidation, one callback per pending prefab, click debounce and exception containment |
| Player lookup | Stale body references and repeated `GetComponent`/caught exceptions | Validated cached game fields; clear on scene change/missing body |
| Update work | Per-frame delegate allocations, targets rebuilt each physics tick, idle stat recalculation | Cached routines, reused targets at 10 Hz, conditional stat recalculation |
| Drawing | ESP ran for multiple IMGUI event types | Repaint-only ESP drawing |
| Clear inventory | Repeated ally scans inside each item removal | One inventory pass and one ally scan |
| Settings | Positional parsing, broken keys/dimensions could fail startup | Named-field parsing, defaults per field, enum/finite-number validation and atomic saves |
| Feature errors | One failure could interrupt the whole update every frame | Per-feature managed guard, two-second backoff, pause after three consecutive failures |
| Item counts | Unbounded roll work and integer wraparound | Bounded click quantities; overflow checks; pack validation before mutation |

This is a focused refactor. The original menu's feature classes remain recognizable to keep behavior reviewable; a full rewrite of every gameplay function would increase regression risk without evidence that it is needed. Public menu/button IDs and loader entry points remain stable.

## Validation and limits

`scripts/Test.ps1` compiles production UI/catalog/portal/pack code against small game/Unity stand-ins. It checks grouping, exact sprite atlas coordinates, item/removal search, pack recipes, navigation, resizing, portal completion/state changes, projected ESP bounds, cache limits, overflow rejection and managed action failures.

Separate checks compile the production loader/settings/routine/texture/reflection code to test deferred main-thread initialization, catalog readiness, duplicate lifecycle calls, deferred destruction, component failure, timeout subscription cleanup, failure backoff, settings recovery and resource release. These are lifecycle models, not a substitute for Unity's runtime.

Injector checks use the production bundle/session/protocol code for checksums, incompatible builds, malformed manifests, atomic status reads, concurrent launchers, PID reuse and uncertain retry gates. Native memory tests allocate only inside the test console process, verify release through `VirtualQueryEx`, simulate preservation on timeout, and check failed-constructor handle cleanup. They **do not create a remote thread or inject a game**.

The build compiles against the actual installed game DLLs with warnings treated as errors. Off-screen launcher and modeled menu previews are rendered for layout inspection. Neither a successful compile nor these models verifies real native injection, engine lifecycle timing, DLC runtime assets, multiplayer behavior or a long gameplay session. A live acceptance pass remains necessary; no crash-free guarantee is claimed.

Mono thread registration follows the [official embedding API guidance](https://www.mono-project.com/docs/advanced/embedding/). The backend's existing attach/OS-thread-exit pattern is retained; it never dereferences captured managed exception objects after their remote thread has exited.

## Repository boundaries

- `src/Menu`: game-facing menu code.
- `src/Injector`: new WinForms launcher and supervised injection worker.
- `src/Shared`: bounded, atomic launcher/menu status protocol.
- `vendor/SharpMonoInjector`: pinned upstream source and documented local fixes.
- `tests`: regression fixtures and console checks; no personal game files.
- `scripts`: build, checks and clean package generation.

No game assemblies, personal settings, access tokens, old compiled injector binaries or auto-update network dependencies are needed in the public source tree. A release manifest fingerprints your installed RoR2 assembly; rebuild after updates instead of injecting an unverified stale DLL.
