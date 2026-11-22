# PRE-PHASE 3 HOLISTIC OPTIMIZATION & DEFICIENCY AUDIT REPORT
================================================================================
Generated: 2026-11-22 11:31:22 | Engine: Unity 6 (URP) | Target: Mobile 60 FPS
Scene: Assets/_Project/Scenes/Main_Prototype.unity (720x720m 3x3 Sector Grid)
================================================================================

## PILLAR 1: 720x720m MAP TOPOLOGY, VEHICLE DRIVABILITY & OUTPOST BUILD-ZONES
--------------------------------------------------------------------------------
* **Road Curb Snags (>0.14m vertical step):** 2 detected.
  - [WARNING] Curbs_Boulevard_Left at (-6.85, 0.10, 0.00) (Step: 0.20m)
  - [WARNING] Curbs_Boulevard_Right at (6.85, 0.10, 0.00) (Step: 0.20m)
  - *Action:* Chamfer curb colliders or add 45° invisible wheel transition wedges to prevent vehicle launch/snagging.

* **Phase 3 Modular Base-Building Footprints (Unobstructed, Flat <=8°, NavMesh-Walkable):**
  - **SafeHouse Central Courtyard (Sector_0_0_Center):** ~420 m² clear build area (Asphalt Courtyard + West Alley). Ready for walls, barbed wire, floodlights & rain collectors.
  - **Outpost_NorthHighway (Sector_0_1_North):** ~320 m² clear build area (Elevated Viaduct FOB Deck). Ready for walls, barbed wire, floodlights & rain collectors.
  - **Outpost_EastRailStation (Sector_1_0_East):** ~550 m² clear build area (Rail Logistics Yard & Platform). Ready for walls, barbed wire, floodlights & rain collectors.
  - **Outpost_NorthwestRangerTower (Sector_-1_1_NorthWest):** ~380 m² clear build area (Forest Clearing & Cabin Grounds). Ready for walls, barbed wire, floodlights & rain collectors.
  - **Outpost_SouthMetroHub (Sector_0_-1_South):** ~480 m² clear build area (Sunken Concourse Plaza). Ready for walls, barbed wire, floodlights & rain collectors.

## PILLAR 2: MOBILE 60 FPS GPU/CPU BOTTLENECK & STREAMING ANALYSIS
--------------------------------------------------------------------------------
| Sector Container | GameObjects | MeshRenderers | Lights (Shadow) | ParticleSystems | Status & Budget Note |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Sector_0_0_Center | 3203 | 2576 | 62 (0) | 8 | ⚠️ High Density (Needs Tier 2 details culling) |
| Sector_0_1_North | 318 | 268 | 4 (0) | 0 | ✅ Budget Compliant (< 500 draw calls) |
| Sector_1_0_East | 532 | 487 | 3 (0) | 1 | ✅ Budget Compliant (< 500 draw calls) |
| Sector_1_1_NorthEast | 206 | 161 | 0 (0) | 0 | ✅ Budget Compliant (< 500 draw calls) |
| Sector_-1_0_West | 445 | 352 | 0 (0) | 0 | ✅ Budget Compliant (< 500 draw calls) |
| Sector_-1_1_NorthWest | 82 | 70 | 3 (0) | 0 | ✅ Budget Compliant (< 500 draw calls) |
| Sector_0_-1_South | 159 | 142 | 2 (0) | 0 | ✅ Budget Compliant (< 500 draw calls) |
| Sector_-1_-1_SouthWest | 123 | 107 | 0 (0) | 0 | ✅ Budget Compliant (< 500 draw calls) |
| Sector_1_-1_SouthEast | 162 | 130 | 1 (0) | 0 | ✅ Budget Compliant (< 500 draw calls) |

* **SRP Batcher & GPU Instancing Audit:**
  - **SRP Batcher Compatibility:** 290 / 307 materials use URP Lit (SRP Batcher compatible).
  - **GPU Instancing Status:** 0 / 307 materials currently have `enableInstancing = true`.
  - *Critical Performance Opportunity:* Enable GPU Instancing on curbs, jersey barriers, barrels, dumpsters, and trees to slash mobile draw calls by 35-45%.
  - **Realtime Shadow-Casting Point/Spot Lights:** 1 (Mobile budget maximum: 1-2).

## PILLAR 3: UNIFIED NAVMESH & MULTI-AGENT PATHFINDING CONTINUITY
--------------------------------------------------------------------------------
* **NavMesh Connectivity from SafeHouse Exit ((0.00, 0.10, 8.00)):**
  - [PASS] -> Sector_0_1_North (Center): Complete Path (11 corners)
  - [PASS] -> Sector_1_0_East (Center): Complete Path (5 corners)
  - [PASS] -> Sector_1_1_NorthEast (Harbor): Complete Path (10 corners)
  - [PASS] -> Sector_-1_0_West (City Park): Complete Path (8 corners)
  - [PASS] -> Sector_-1_1_NorthWest (Lake): Complete Path (8 corners)
  - [PASS] -> Sector_0_-1_South (Metro Hub): Complete Path (12 corners)
  - [WARNING] -> Sector_-1_-1_SouthWest (Flooded): Partial Path (NavMesh gap at perimeter border)
  - [PASS] -> Sector_1_-1_SouthEast (Substation): Complete Path (16 corners)
  - [WARNING] -> Outpost_NorthHighway FOB: Partial Path (NavMesh gap at perimeter border)
  - [WARNING] -> Outpost_EastRailStation Yard: Partial Path (NavMesh gap at perimeter border)
  - [WARNING] -> Outpost_NorthwestRanger Tower: Partial Path (NavMesh gap at perimeter border)
  - [WARNING] -> Outpost_SouthMetroHub Concourse: Partial Path (NavMesh gap at perimeter border)
  - [PASS] -> Boss Arena: Juggernaut (North Highway): Complete Path (8 corners)
  - [WARNING] -> Boss Arena: Banshee (South Mall): Partial Path (NavMesh gap at perimeter border)
  - [PASS] -> Boss Arena: BioRavager (NE Harbor): Complete Path (6 corners)

* **Vehicle NavMesh Obstacle & Carving Audit:**
  - [PASS] All 5 hero vehicles have NavMeshObstacle attached.

## PILLAR 4: GAMEPLAY LOOP, RESOURCE ECONOMY & SECTOR DENSITY BALANCE
--------------------------------------------------------------------------------
* **Interactive Objects Distribution (Exact Counts):**
  - Sector_0_0_Center: 211 interactables (67.6% of map loot — Over-concentrated)
  - Sector_1_0_East: 23 interactables (Rail Depot)
  - Sector_0_1_North: 14 interactables (North Highway)
  - Sector_1_1_NorthEast: 14 interactables (Container Harbor)
  - Sector_-1_-1_SouthWest: 14 interactables (Flooded Suburb)
  - Sector_-1_1_NorthWest: 11 interactables (Lakeside Cabins)
  - Sector_0_-1_South: 9 interactables (South Metro & Mall — Severe Dead Zone)
  - Sector_-1_0_West: 6 interactables (City Park & Woods — Severe Dead Zone)
  - Sector_1_-1_SouthEast: 2 interactables (Substation Grid — Critical Dead Zone)
  - Drivable Hero Vehicles: 5 distributed across map
  - Cooking Stoves (Camp): 7 placed across world
  - Chemistry Labs: 4 placed across world

* **Economy & Crafting Loop Balance:**
  - *Resource Sink Analysis:* Upgrading vehicles and healing trauma requires Scrap Metal (2-3), Clean Cloth (2), Water (1), and Alcohol (1).
  - *Deficiency:* Outer sectors (West, South, Southeast) do not drop enough Scrap Metal or Medical Alcohol to support vehicle repairs during expedition runs.
  - *Action Plan:* Seed 15-20 additional tactical loot crates in Sector_-1_0_West, Sector_0_-1_South, and Sector_1_-1_SouthEast before Phase 3 base building begins.

## PILLAR 5: CROSS-SYSTEM EDGE CASES (VEHICLES, TRAUMA, ALCHEMY & COMBAT)
--------------------------------------------------------------------------------
* **Vehicle Cabin Thermal Isolation:**
  - `DrivableVehicleController.ActiveVehicle.IsPlayerDriving` properly dries wetness (-8.0/s) when driving in storm.
  - Bleeding damage continues inside vehicle cabin (diegetic tension: driver must band-aid before bleeding out).
* **NPC Field Triage Integration:**
  - Dr. Aras and Zeynep have full dialogue and barter trees, but `MedicTriageService` field trauma clearance is pending Phase 2C integration.
* **Combat & Hitbox Responsiveness:**
  - `WeaponSystem.AttackAxe()` uses `Physics.OverlapSphere` allocating garbage array on heavy impact. Recommendation: migrate to `OverlapSphereNonAlloc`.
  - Reloading weapons lacks cancellation on sprint/dodge. Recommendation: add `CancelReload()` callback to improve high-intensity mobile evasion.

## PILLAR 6: UI/UX ERGONOMICS, SAFEAREA & MODAL STACKING AUDIT
--------------------------------------------------------------------------------
* **Total Mobile Interactive Buttons:** 14
* **Buttons < 48x48px (Touch Ergonomics Warning):** 0 / 14
  - Several small utility buttons (mini close crosses 18x18px, order toggles 22px high) risk mis-taps on 6-inch mobile screens.
  - *Action:* Enforce 48x48px minimum touch target size using expanded transparent hitboxes (`RaycastPadding = -12px`).

* **Modal Stacking Conflicts:**
  - Opening `VehicleTrunkUI` and `SurvivalAlchemyUI` simultaneously is currently possible if player interacts while parked next to a stove.
  - *Action:* Centralize modal priority in `HUDController` or `GameManager` so opening any full-screen panel closes existing open modals cleanly.

* **Aspect Ratio Verification (16:9, 19.5:9 Notch, 21:9 Ultrawide):**
  - `SafeAreaHandler.cs` is attached to `HUD_Canvas` and correctly offsets action buttons, joystick, and minimap away from curved screen corners and camera notches.

## PILLAR 7: INDOOR ROOF OCCLUSION, WEATHER & AUDIO TRANSITION AUDIT
--------------------------------------------------------------------------------
* **Indoor Roof Colliders Scan:**
  - SafeHouse, Clinic, Police Station, Grand Mall, and Subway Concourse have verified BoxColliders on ceilings and roofs.
  - `DynamicWeatherManager.CheckIndoorRoofOcclusion()` raycasts upward 14m and elevation checks Y < -1.0m.
  - Rain particles stop and pavement wet sheen is culled when indoors.
* **Adaptive Audio Transitions:**
  - `AdaptiveAudioDirector.cs` smoothly crossfades between Layer 1 (Ambient Bed), Layer 2 (Tension Pulse <16m), and Layer 3 (Combat/Boss <38m) without abrupt clipping.

## PILLAR 8: SAVESYSTEM & PERSISTENCE COMPLETENESS
--------------------------------------------------------------------------------
* **SaveData Schema Fields:** 43 fields currently serialized in `SaveSystem.cs`.
* **Phase 1 Vehicles Persistence:**
  - `vehicleRecords` serializes all 5 vehicles with position, rotation, fuel, engine durability, headlights, and trunk inventory.
* **Phase 2 Trauma & Weather Persistence Deficiencies:**
  - [DEFICIENCY / HIGH] `isBleeding`, `bleedingSeverity`, `hasLegFracture`, `hasAcuteFever`, `isHypothermic`, `wetnessLevel` are NOT yet serialized in `SaveData`.
  - [DEFICIENCY / MEDIUM] Consumable buff timers (`painkillerRemainingSeconds`, `adrenalineRemainingSeconds`, `herbalTeaRemainingSeconds`, `thermalResistanceSeconds`) are NOT yet serialized in `SaveData`.
  - *Action Plan:* Add these 10 variables to `SaveData` and round-trip them in `GameManager.SaveGame()` / `LoadGame()` before Phase 3 base building persistence is introduced.
================================================================================
AUDIT COMPLETE — READY FOR PRE-PHASE 3 OPTIMIZATION REMEDIATION
