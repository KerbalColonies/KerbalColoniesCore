# Changeable Production Queue

## Understanding

Replace the separate vessel, facility-construction, and facility-upgrade work lists with one ordered, colony-wide production queue. Queue entries must be reorderable and cancelable, production facilities must independently work on the earliest queue entry whose required constraints they satisfy, and new facility construction/upgrade costs must be drained as work is completed rather than charged upfront.

## Decisions

- A production facility can process an item when its current level capabilities contain every constraint required by that item.
- Each production facility contributes to the earliest compatible and currently affordable queue item. It may skip incompatible or resource-/funds-blocked earlier entries so its available production is used as fully as possible.
- Facility resources and funds are drained proportionally with applied production. Resource costs come only from colony storage; active-vessel resources are not used.
- Canceling never refunds already consumed resources or funds.
- Canceling construction removes the unbuilt facility from the colony. Canceling an upgrade leaves the facility at its current level. Canceling a vessel build removes the vessel from its hangar.
- Legacy queues migrate in vessel, upgrade, construction order and preserve remaining work.
- Queue entries persist normalized build progress rather than configured build time. Facility items use their immutable target-level config as the source for time and cost calculation; vessel items persist the vessel-specific parameters plus recipe/config identity needed to calculate their time and cost.
- The queue-item base class exposes generic time/cost methods and an in-memory calculated-cost cache. Costs are calculated when an item is enqueued or restored during save load, are not serialized, and are recalculated only when the relevant Kerbal Colonies game parameters change.
- A persisted `paid` flag bypasses further construction-cost draining. Migrated facilities set it because their legacy upfront cost was already paid; migrated vessels remain unpaid and continue draining from their migrated progress.
- Missing per-level constraints and capabilities inherit a copied list from the previous level; level 0 defaults to an empty list. A production level that supports vessel recipes automatically receives the `vessel` capability.
- The config schema will distinguish target requirements from producer capabilities: a common per-level `buildConstraints` node for every facility type and a production-only per-level `productionCapabilities` node. Both contain repeated string values and are inherited when omitted.

## Approach

Introduce an abstract production queue item model in [source/colonyFacilities/ProductionFacility/KCProductionQueueItem.cs](../../source/colonyFacilities/ProductionFacility/KCProductionQueueItem.cs). The base model will hold normalized build progress, the `paid` flag, required constraints, persistence metadata, an in-memory cost cache, and lifecycle operations for starting, applying production, completing, canceling, and placing an item. It will define generic methods such as `GetBuildTime`, `CalculateCosts`, `RecalculateCosts`, and `GetCosts`. Facility items will calculate from their target-level facility config, while vessel items will persist dry mass, recipe config/level identity, and any other vessel-specific inputs required for calculation. Facility and vessel implementations will resolve their payloads by stable facility ID or vessel/hangar IDs and route completion/cancellation through the existing facility placement and hangar behavior.

Refactor [source/colonyFacilities/ProductionFacility/KCProductionFacility.cs](../../source/colonyFacilities/ProductionFacility/KCProductionFacility.cs) to own one ordered queue per colony and allocate each enabled producer's elapsed production to its earliest compatible, affordable entry, skipping blocked entries to keep usable production working. Production advances normalized progress using the item's calculated build time. Unless `paid` is set, the cost for a progress increment is extrapolated as the item's cached total cost multiplied by that increment and consumed atomically from `KCUnifiedColonyStorage` and `Funding` before progress advances. Queue load and enqueue paths will populate the cache, while a Kerbal Colonies parameter-change hook will recalculate all loaded queue-item caches when facility or vessel cost settings change. Queue persistence will move to explicit colony load/pre-save hooks, including deterministic migration from the current vessel, upgrading, and constructing save structures. Completed construction and additional-group upgrades will continue to use the existing completed-item placement lists; completed vessels will be finalized in their assigned hangar and removed from the queue immediately, with no finished-vessel list or placement stage.

Update configuration parsing and UI entry points so facility level requirements and producer capabilities control eligibility consistently. New facility builds and upgrades will enqueue without calling the upfront `removeResources` path, vessel storage with a dry mass will create a vessel queue item, and the production window will display the unified queue with move-up, move-down, and cancel controls for both item kinds. Existing tech-tree, CAB-level, hangar-capacity, completion notifications, and facility placement behavior will remain in force.

## Key Files

- `source/colonyFacilities/ProductionFacility/KCProductionQueueItem.cs` - New shared queue-item abstraction plus facility and vessel item implementations.
- `source/colonyFacilities/ProductionFacility/KCProductionFacility.cs` - Unified queue ownership, producer scheduling, proportional cost draining, persistence, and legacy migration.
- `source/colonyFacilities/ProductionFacility/KCProductionInfo.cs` - Per-level producer capability parsing and automatic `vessel` capability.
- `source/KCFacilityInfoClass.cs` - Common per-level build-constraint parsing and inherited defaults.
- `source/colonyFacilities/ProductionFacility/KCProductionWindow.cs` - Unified ordered queue display, reordering, cancellation, compatibility feedback, and enqueue behavior.
- `source/colonyFacilities/CabFacility/KC_CAB_Facility.cs` - Facility construction/upgrade queue item creation.
- `source/colonyFacilities/CabFacility/KC_CAB_Window.cs` - Remove upfront charging and reflect queued upgrade state.
- `source/colonyFacilities/HangarFacility/KCHangarFacility.cs` - Vessel enqueue, completion-state persistence compatibility, and cancellation removal.
- `source/colonyFacilities/HangarFacility/KCHangarFacilityWindow.cs` - Route in-progress vessel cancellation through the unified queue and avoid stale queue entries.
- `source/colonyFacilities/HangarFacility/StoredVessel.cs` - Stable linkage between vessel queue entries and stored vessels while retaining legacy fields for migration.
- `source/Settings/KCGameParameters.cs` - Raise the queue cost-recalculation hook when KC facility or vessel cost parameters actually change.
- `source/ConfigFacilityLoader.cs` - Register production queue load/pre-save lifecycle actions with unique priorities.
- `source/colonyClass.cs` - Provide the minimal safe facility-removal operation needed when construction is canceled.

## Risks & Open Questions

- The repository has no test project, and the main project depends on a local KSP installation and mod assemblies. Verification therefore requires a successful solution build plus focused in-game save/load scenarios.
- Old vessel and facility work previously ran in separate production pools, so migration cannot preserve their former parallel execution. The agreed deterministic order is vessels, upgrades, then construction.
- External facility config packs are not in this repository. Empty level-0 lists keep old packs loadable, but producers without declared capabilities will not process constrained facilities; vessel-capable production levels remain compatible through the automatic `vessel` capability.
- Facility and vessel recipe configs are treated as immutable while a game is running. Queue persistence retains vessel calculation inputs but not calculated costs; save load/enqueue initializes the cache, relevant KC game-parameter changes invalidate it, and the `paid` flag prevents migrated facility entries from being charged twice.
- Static per-colony queue state must be initialized and cleared idempotently across game loads to avoid carrying entries between saves.

## Steps

- [x] 1. Add the queue-item model in `KCProductionQueueItem.cs` with common identity, required constraints, normalized progress, the `paid` flag, generic time/cost calculation methods, a nonserialized calculated-cost cache, matching, proportional affordability/consumption, lifecycle methods, and ConfigNode serialization.
- [x] 2. Implement facility queue-item behavior for new construction and upgrades, including existing completion/placement paths and cancellation that either removes an unbuilt facility or leaves an upgrading facility unchanged.
- [x] 3. Implement vessel queue-item behavior linked by vessel and hangar identifiers, persisting the vessel parameters and recipe config/level identity needed by the generic time/cost methods, using the hardcoded `vessel` requirement, immediately finalizing/removing completed entries, and deleting canceled vessels without refunds.
- [x] 4. Extend `KCFacilityInfoClass` to parse and inherit per-level `buildConstraints` lists without sharing mutable lists between levels.
- [x] 5. Extend `KCProductionInfo` to parse and inherit per-level `productionCapabilities`, automatically add `vessel` wherever vessel production is configured, and replace recipe-based producer eligibility with all-constraints matching.
- [x] 6. Replace unfinished production dictionaries and inferred hangar ordering in `KCProductionFacility` with one ordered per-colony queue, then allocate each producer to its earliest compatible and affordable entry, skipping blocked entries and applying as much usable production as possible.
- [x] 7. Add ordered queue save/load support and migrate legacy vessel, upgrade, and construction state in the agreed order by converting saved remaining work to normalized progress, setting `paid` for legacy facilities, retaining vessel cost/time calculation parameters, and calculating each restored item's cost cache once.
- [x] 8. Add a KC game-parameter change hook in `KCGameParameters` that detects relevant facility/vessel cost multiplier changes and asks the production queue to recalculate all loaded item cost caches.
- [x] 9. Change facility build and upgrade entry points in the production and CAB flows to enqueue unpaid items, calculate their initial cost cache, and avoid upfront resource/fund checks or removal while retaining tech-tree and CAB-level validation and preventing duplicate upgrades.
- [x] 10. Change vessel storage/build entry points to enqueue vessel work explicitly, calculate its initial cost cache, and keep hangar serialization capable of reading legacy vessel build fields.
- [x] 11. Rework the production window to show one mixed queue with progress, blocked/incompatible status, move-up, move-down, and cancel controls, and update the hangar window to use the same cancellation path.
- [x] 12. Update CAB summaries, facility enabled-state checks, and placement-state checks to use the unified queue while retaining completed construction and upgrade placement behavior.
- [x] 13. Build `source/KerbalColonies.csproj`; the build passed. Manual in-game verification remains required because the KSP runtime is unavailable in this environment.
