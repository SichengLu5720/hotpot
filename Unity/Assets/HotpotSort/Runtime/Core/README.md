# Daily core integration contract

This module implements TASK-001 only. It has no scene, physics, WeChat SDK,
wall clock, static mutable session data, file persistence or environment-variable
storage adapter. Each factory call constructs new inventory and RNG instances.

## Production construction

1. Read `Content/Daily/daily_core_1.0.0.json` as UTF-8 and its `outputSha256`
   from `Content/Daily/import-manifest.json`. Pass both to `DailyContent.Load`.
2. Supply exactly 16 distinct `IngredientEntry` objects valid for that content
   version. IDs are sorted ordinally before mapping. Resource address, display
   key, collider specification and size class are caller-owned actual catalog
   metadata; this core does not invent art assets or load their resources.
3. Construct `DailySessionFactory(content, entries)`. Use its
   `ConfigurationDigest` in `ChallengeContext`; it binds content and catalog.
4. `CreateDailySession(context)` returns `DailySession`; the existing
   `IGameSessionFactory.CreateSession` returns the same implementation through
   the unchanged public Contracts interface. A retry creates a fresh object.
   Initialization yields Running with all 50 plates Pending and C/H orders.
   Invalid context identity yields an inspectable Aborted session.

The host must reference `HotpotSort.Core`, and `HotpotSort.Replay` for replay.
The three runtime assemblies have no Unity engine references. This change does
not bind Bootstrap or edit the public Contracts files.

## Inputs and observations

- `Tap(new TapCommand(itemId, inputSeq, logicalBoundary, hitAccepted))`: IDs are
  1..183 in plate/source order; kinds A..P stay abstract after art mapping.
  Sequences are strictly increasing among accepted inputs. The view supplies
  actual hit/occlusion acceptance. Only ActiveAvailable items can be tapped.
- `Supply(new SupplyObservation(observationSeq, logicalBoundary, canSpawn,
  cooldownReady))`: each accepted call commits exactly the Pending head. No
  plates spawn during initialization or merely because a tap cleared a plate.
  The caller must supply a new observation for each subsequent plate.
- `Pause(ulong)` / `Resume(ulong)` record effective boundaries. The legacy
  parameterless Contracts methods use the latest injected boundary, without
  reading a platform clock. Terminal states reject further mutations.
- Boundaries are monotonic unsigned integer logical ticks, not seconds or
  timestamps. `durationBoundaries` accumulates Running intervals only; the host
  defines tick units when presenting duration. JSON uses decimal strings for
  boundaries, sequence IDs, seeds and RNG UInt64 state. There is no input lock.
- `CommandResult` exposes accepted/reason, immutable snapshot/event batch and
  hashAfter. Overflow is an accepted player tap with Failed outcome; the source
  item remains ActiveAvailable and processedItemCount does not increase.
  Changed fires only for committed transactions, after stable close. Reentrant
  commands during publication are rejected as Resolving.

## Observability and deterministic algorithms

`Snapshot.CanonicalStateJson` (`daily_state_v1`) includes IDs and locations for
every item, Pending and Active plate arrays, five fixed buffer positions, four
order slots (2/3 Locked), rational progress, statistics, identities, Mapping and
Director RNG state and transaction/event sequence. Strings and collections in
public DTOs are immutable copies. `StatisticsJson` adds rejected taps, retry and
time-source metadata without adding those fields to core state.

`StateHash`, `InitialHash`, `CoreEventsJson`, `DiagnosticsJson`,
`InspectDirector(slotId)` and `PresentationRngJson` are read-only. Director
inspection does not consume RNG; it reports evaluation without choosing a
weighted category/candidate. Actual DirectorEvaluated events include all raw
and filtered weights, all 16 legality/cost records, reservation IDs, scanned
plate IDs, fallback reason and every accepted/rejected raw RNG draw.

`PCG32_v1` uses standard XSH-RR 64/32, initseq=54 and two-step seeding.
Public drawIndex starts at zero after seeding and counts rejection draws.
NextBounded(1) consumes a draw. Normal Director selection uses category then
candidate draws; zero-weight and out-of-band fallback use one tie-pool draw,
including a singleton. Opening orders consume none. Mapping uses descending
Fisher-Yates over ordinal catalog IDs. `NextPresentation(bound)` only consumes
the independent Presentation stream and never changes core state/events.

Reservations allocate other slots' unmet demand in slot order, then Buffer
index, Active plate/source order and Pending plate/source order. Items already
inside orders are outside the allocatable pool. Cost scanning finishes the
entire final Pending plate and counts every non-target item, including those
reserved for another order. Empty strict pools alone enable duplicate orders;
strict out-of-band pools use MinCostOutsideBands directly. Auto-absorption
preserves buffer holes. Completion chains have no fixed iteration cap.

`canonical_json_v1` uses ordinal object keys, contractual array order, UTF-8
without BOM, no whitespace, integers/booleans/strings/null, escaped controls
and UTF-16 surrogate code units, and lowercase SHA256. Core event sequence
starts at one and excludes rejections. TransactionClosed reserves its event
sequence before computing hashAfter; the published snapshot hashes exactly
to that value. Core hashes exclude session ID, retry, time source, diagnostic
rejections, external input/observation sequences, Presentation RNG and visual
data. Injected logical boundary and elapsed logical ticks are core state;
platform clock timestamps never enter it. PlateSpawned records logical IDs;
presentation RNG state is available separately, outside core events.

## Replay and fixture construction

`ExportReplay()` returns the existing Contracts `ReplayPackage`, containing
`daily_replay_v1`: context, content/catalog/configuration and algorithm
identities, seed, initial hash, accepted taps, actual supply commits, effective
pause/resume records, per-commit state/event hashes, final hashes and separate
diagnostics. Export is entirely in memory; persistence belongs to the caller.

`HotpotSort.Replay.DailyReplay.Run(factory, package)` creates a fresh session,
validates identities and each record, and returns the first mismatch index
(-1 for header/initialization). The internal replay supply path verifies exact
queue head/item IDs; it never asks a physics scene to solve supply again.
Unknown versions are rejected. Current version implementations remain named
and fixed so future versions can dispatch separately instead of silently
interpreting old data with new rules. Rejected diagnostics are retained in
the package but never re-applied as accepted commands.

`CreateFixtureSession(context, DailyFixture)` is an explicit isolated in-memory
construction port for future fixed QA plans. A fixture supplies the spawned
prefix length, exactly five buffer slots, two order definitions and completed
item IDs. Unassigned spawned items remain ActiveAvailable. Duplicate/missing
or impossible assignments abort initialization. Fixture completion chains
settle during initialization; initial events/hash and the fixture itself are
recorded for reproducible fixture replays. It shares no mutable state with
production sessions and does not rely on environment variables.

## Content import and verification status

`Editor/ContentImport/DailyContentImporter.Import` verifies frozen source byte
SHA256 values, copies the 50 source plate indices and imports only the 20
original Difficulty 3 rows. Decimal weights are multiplied by exactly 100 and
must convert integrally. Runtime data contains no Difficulty dimension.
`DailyContentBuildGuard` validates the frozen import before Unity builds and
is also available from Hotpot Sort / Validate Daily Content. Source JSON and
canonical output use `-text` Git attributes so Windows checkout conversion
cannot invalidate byte hashes.

The portable build/import utility can be run from the worktree root:

```powershell
dotnet build Unity/Assets/HotpotSort/Editor/ContentImport/CompileAndImport.csproj --nologo --verbosity minimal
dotnet Unity/Assets/HotpotSort/Editor/ContentImport/.build/bin/Debug/net10.0/CompileAndImport.dll Unity/Assets/HotpotSort/Content/Daily/skeleton_C.source.json Unity/Assets/HotpotSort/Content/Daily/LevelDifficultyConfig.source.json Unity/Assets/HotpotSort/Content/Daily
```

Performed: portable .NET 10 compilation (0 warnings/errors) and formal source
import (50/183/16 and 20 rows; canonical output SHA256
`e6612cb54b548b81aabef9bc376441682ff0157b556b5a6258cba4c7057e5d08`).
The portable build is a compile/import diagnostic, not a game test harness.
Unity 2022.3 editor compilation, the Unity build callback, gameplay/replay
execution, long runs, Android/WeChat and device experience are NOT RUN.
No release QA scripts or acceptance assertions were created or executed.
