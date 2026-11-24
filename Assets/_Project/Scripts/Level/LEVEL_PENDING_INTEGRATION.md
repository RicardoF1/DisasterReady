# Level integration and QA items pending

This record tracks work that cannot be completed safely until Core / Character contracts are available. No substitute interfaces, game manager, or bootstrap should be created in Level.

## Core contracts needed from Keen

- Exact `SceneBootstrap.Register` signature, installer order, and callback arguments, including how the installer receives the root `Transform` and `house` boolean.
- Exact earthquake phase/event contract and end/reset notification. `LevelEarthquakeEffects.BeginEarthquake()` and `EndEarthquake()` are implemented as explicit Play Mode methods; they are not wired to Core. Nothing auto-starts during `Preparation`.
- Exact `IInteractable` contract for the nine supply props and optional gas shutoff / breaker panel. Current props are physical and are explicitly named "Prop Only" where optional; they do not implement interaction.
- Exact `IWorldQuery` contract and safe-refuge query expectations. Safe/danger trigger markers are local design data only, not exposed to Core.
- Exact public evacuation callback (`TryEvacuate()` or agreed equivalent) and permitted trigger-to-Core path. The exterior assembly trigger currently stores local `LevelEvacuationMarker` data only and invokes no callback.

## Character integration needed from Ricardo

- Actual `CharacterController` height, radius, skin width, step offset, slope limit, and expected standing clearance. Use these to validate the 1.2m interior doors, 1.6m main entrance, 1.3m stairs, 18 risers, and corridors; the current values are only prototype assumptions.

## Scene / bootstrap checks

- Confirm whether `Assets/_Project/Scripts/Level/Level.unity` is the runtime first-level scene or a design scene. At last inspection, Build Settings listed `Assets/_Project/Scenes/MainMenu.unity` and `Assets/_Project/Scenes/House.unity`, not `Level.unity`; `House.unity` itself had only camera, light and global volume. Do not edit scene list or rename scenes without approval.
- When Core is available, register this Level module through the existing `SceneBootstrap` with Keen's agreed order. Do not create another bootstrap or GameManager. Keep Level dependent on Core only, never directly on Player or UI/Audio.
- Reconfirm install behavior on both House/non-House scenes and verify that repeated loads/restarts do not duplicate the generated root.

## Physical / gameplay QA still required

- Enter Play Mode with the real player and verify traversal of the stair, rooms, clear main route and reachable supplies.
- With Core wired, verify hazards start only on `Earthquake`, never during `Preparation`, and every dynamic prop and broken pane resets when the quake ends.
- Exercise the blocked east secondary exit and confirm the green front route, sidewalk, assembly plaza and road connection remain passable with the actual player/controller. Review the existing plaza-to-road boundary.
- Test the assembly trigger through the agreed evacuation method.
- Review educational refuge/hazard locations with the domain owner. Markers are design candidates only and are not structural-safety certification.
- Confirm debris behavior / route choice with gameplay design; debris is static blockout geometry and is not earthquake-activated.

No fake `IInteractable`, `IWorldQuery`, `SceneBootstrap`, `GameEvents`, earthquake events, Player references, or `TryEvacuate` callbacks should be added to work around these missing contracts.
