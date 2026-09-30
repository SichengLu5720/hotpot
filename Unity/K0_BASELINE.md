# K0 bootstrap boundary

Prepared under PM bootstrap authorization without a run_id. During preparation another authorized task established repository baseline `fa5e3d2`; these Unity files remain a separate uncommitted addition for PM inspection and fixation before TASK-003 dispatch. No Task machine record is changed by this work.

The prototype v0.1 source is `.harness/references/prototype-v0.1/HotpotSort_Prototype_v0.1/Unity/`. Its ZIP SHA256 is `34AFB1587C45506C59F8B9E13785AE0E4487AE13DCF7EC7F632D08B34987F2EC`. The Daily SPEC identity is SHA256 `F97D61D9B61CE767F73D36252404BBD3156BEED3A788200051D454AA15D97C78`, as recorded in the task drafts. These are source identities, not runtime validation results.

Selective adoption retains the prototype package manifest, declared editor version, Boot scene identity and portrait sizing. Boot now references only the inert Bootstrap component. Old HotpotApp/Core/Resources, WebGL download bridge, Editor menus and recovered signatures are not imported. The inherited editor declaration is provisional: TASK-003 must investigate and pin a compatible Unity/WeChat combination before platform builds. No SDK or AppID is embedded.

## Ownership after K0

- Shared read-only: Contracts and public directory metadata. PM coordinates changes through a single writer.
- A: Runtime/Core, Runtime/Determinism, Runtime/Replay, Content/Daily, Editor/ContentImport.
- B: Runtime/Presentation, Runtime/UnityPhysics, UI, Prefabs, Scenes/Gameplay.unity; Art/Source and Art/Generated are the art producer's paths; B owns final import metadata/binding.
- C: Runtime/Session, Runtime/Platform/WeChat, Runtime/Bootstrap, Scenes/Boot.unity, Plugins/WeChat, Editor/WeChatBuild, Packages and ProjectSettings.

Each implementation assembly explicitly references Contracts; it must not reference another active task's implementation. Final composition belongs to C. A and B must not add RuntimeInitializeOnLoadMethod session launchers. Boot is the sole production entry.

Development doubles/demos belong under the owner's module `Development/` subdirectory, in an assembly with `defineConstraints: ["HOTPOT_DEVELOPMENT"]`. Development scenes stay there and are excluded from production build settings. C may use core/view doubles only for wiring; production composition must reject doubles. Do not activate development defines in a production configuration. Shared storage is not isolated by this skeleton; C must actually bind runtime paths before concurrent writing instances are launched.

## Contract scope

The minimal contracts support immutable daily identity, new session factories, pause/resume, view mounting/reset/disposal, lifecycle and viewport facts, and diagnostic delivery. Event collections defensively copy inputs. Session/replay/state identity and canonical payloads are strings to avoid JavaScript numeric precision loss. A remains responsible for canonical JSON/schema and algorithm selection. C transports it without deriving inventory or altering replay content.

`GameSnapshot.CanonicalStateJson` is an opaque, versioned handoff envelope, not a frozen final A/B inventory schema. No Seed derivation, supply replay, tap acceptance, RNG, director or inventory implementation is included. Their unresolved product/algorithm details in TASK-001/002 must be resolved before their implementation; this minimal C baseline does not claim those tasks are ready. Abstract kinds remain the Daily 16-kind domain, and resource IDs are distinct from kind IDs; concrete catalog/content versions must be supplied by A/B, never guessed by C.

No Unity import, full compilation, build, gameplay test or device validation is claimed. No production module is yet composed. K0 is not a playable prototype or release candidate.
