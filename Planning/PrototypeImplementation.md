# Prototype Implementation

## Final release target

- Final platform: Apps in Toss game mini-app inside the Toss app.
- Delivery: Unity WebGL packaged as `.ait` through the official Apps in Toss Unity SDK.
- Unity Editor and ordinary browser runs are development environments only.
- The official SDK, app ID, icon URL, production ad group IDs, and sandbox-device validation are not configured yet.
- Gameplay code remains independent from the host SDK. Future Apps in Toss calls belong at the platform boundary and must use asynchronous APIs without blocking the Unity main thread.

## Scene

- Path: `Assets/Scenes/PrototypeScene.unity`
- `SampleScene.unity` is not modified.
- Portrait reference resolution: `1080 x 1920`
- Runtime hierarchy:
  - `PrototypeSystems`: map bounds, arena visual, enemy factory, game session
  - `SpawnTransform`: 28 saved spawn transforms + `SpawnPointRegistry`
  - `Entities/Player`: Facade + health, movement, critical, progression, controller
  - `Entities/Castle`: Facade + reusable health
  - `Entities/Enemies`: spawned enemy Facades and their state/attack modules
  - `PrototypeHUD`: enum-name binding View + Presenter

## Controls

- Tap/click an empty point: move the Player.
- Tap/click an enemy: approach and attack.
- Each touch-command movement segment reaches its destination or attack position within `0.1s`.
- `PAUSE`: pause/resume.
- `CASTLE -10`: apply debug damage to the Castle.
- `PLAYER XP +5`: trigger the prototype level-up flow.
- `CONTINUE (AD TEST)`: simulate a successful rewarded-ad result after Game Over.

## Implemented rules

- Melee/Ranged front/rear attack table and level-difference immunity.
- Shield front/rear durability rules and conditional 0.5-second input lock.
- Boss durability and critical damage rules.
- Critical card: +10%, repeatable, maximum 70%.
- Boss three-second cycle:
  - `0.0–1.5`: move with warning area.
  - `1.5–2.0`: stop and apply the attack.
  - `2.0–3.0`: move toward Castle.
- Castle Game Over and rewarded continue:
  - maximum two continues;
  - Castle restores to 50%;
  - Player restores to maximum HP;
  - Castle is invulnerable for three seconds;
  - normal enemies move to the map boundary;
  - Boss moves halfway to the boundary.
- Account EXP conversion: `floor(score / 5)`.
- UI binding uses enum names as GameObject names and enum indices as cached component slots.
- Player movement uses a duration-based command: empty-point and enemy-approach movement completes within `0.1s`; no movement occurs when an Enemy is already inside the gray attack range.
- A movement path checks the Player and Enemy Collider radii. The first Enemy intersecting the path becomes the attack target even when the user touched empty space behind it.
- A one-hit kill resumes movement to the original touch point; an Enemy that survives keeps the Player stopped at the attack-range edge.
- The same touch command repeatedly acquires the next Enemy intersecting its remaining path. Consecutive one-hit kills are swept in order; the first survivor is attacked once and stops the Player at the attack-range edge.
- Enemy name and level labels use the normal, non-critical combat table relative to the current Player level: green for one hit from either side, white for three front hits/one rear hit, and red for every other case. Labels refresh after Player level-up.
- Player attacks have no automatic click cooldown. Every valid Enemy click creates one attack request; repeated clicks during approach are queued for the same target.
- Prototype Enemy touch correction uses a `1.5` world-unit radius instead of the previous `1.1` radius.
- Player Prefab: `Assets/Resources/Bandits - Pixel Art/Demo/LightBandit.prefab`.
- Player AnimationClips and AnimatorController: `Assets/Resources/Bandits - Pixel Art/Animations/Light Bandit`.
- Melee, Ranged, and Boss Enemy visual: `Resources/Monsters Creatures Fantasy/Sprites/Goblin`.
- ShieldEnemy visual: `Resources/Monsters Creatures Fantasy/Sprites/Skeleton`.
- The Resources LightBandit Prefab is the single gameplay Player Prefab. It contains the project Player components, serialized Rigidbody2D/Collider2D, SpriteRenderer, and Animator; the former generated Player Prefab/clip/controller duplicates under `Assets/Game/Characters` are removed.
- Enemy profile AnimationClips, Goblin/Skeleton AnimatorControllers, and the four Enemy Prefabs remain under `Assets/Game/Characters`.
- `PrototypeEnemyFactory` instantiates saved Enemy Prefabs instead of creating GameObjects and components at runtime.
- Player and Enemy Prefabs serialize one Rigidbody2D, one Collider2D, and all fixed range, warning, marker, and label objects. Castle, Arena, CameraShake, and CombatFeedback components are saved in the Scene; runtime code contains no `AddComponent` or fixed-child construction.
- Shared `CharacterSpriteAnimator` only forwards Motion, FaceLeft, Attack, Hurt, and Death parameters to each Prefab's Animator. It does not load Sprite arrays or advance frames in code.
- Each character Prefab has exactly one Animator and its Controller on the Prefab root. Every AnimationClip binds to the shared `Visual/Sprite` child path.
- The Animator base layer drives Idle, Move, Guard, Attack, Hurt, and Death. A separate Facing layer drives saved direction clips instead of changing `SpriteRenderer.flipX` in code.
- The source sheets do not share a native direction: LightBandit faces left at scale X `+1`, while Goblin and Skeleton face right at scale X `+1`. Player Facing therefore uses Left `+1`/Right `-1`, and Enemy Facing uses Left `-1`/Right `+1`.
- Enemy death immediately stops movement and attacks, disables its Collider and gameplay markers, plays the profile Death clip, and only then disables the GameObject. Player lethal damage also enters its Death state and `RestoreAfterContinue` explicitly returns the Animator to Idle.
- ShieldEnemy plays Skeleton Walk while pursuing the Player outside its cyan approach range. It stops and immediately loops Shield when the Player enters that range; after a Hit one-shot it returns to Shield while the Player remains inside.
- ShieldEnemy keeps its previous left/right facing for `0.5s` after the Player crosses to the opposite side, then turns while continuing its current movement or Guard state.
- Starting card selections equal `AccountLevel - 1`; an account level of 3 therefore requires two sequential selections before gameplay begins. The current prototype account level is serialized on `PrototypeGameSession` until account save loading is implemented.
- Level-up and starting card selection use a dedicated `CardSelection` run state. `Time.timeScale` is zero and Player input, Enemy state updates, animations, elapsed time, and spawning remain stopped until all queued selections finish.
- The supplied sheets are side-view assets. Horizontal movement/targets select the saved FaceLeft or FaceRight animation state; vertical movement plays the character's movement animation while preserving the last horizontal facing.

## Game data assets

The current prototype reads balance and schedule data through
`Assets/Game/Data/GameDataManifest.asset`.

- Excel-generated area:
  - `Generated/EnemyBalanceTable.asset`
  - `Generated/StageSpawnSchedule.asset`
  - `Generated/PlayerLevelExperience.asset`
  - `Generated/AccountLevelExperience.asset`
  - `Generated/GlobalBalance.asset`
- Unity-authored area:
  - `Catalogs/EnemyAssetCatalog.asset`
  - `Profiles/CombatFeedbackProfile.asset`
- Scene-authored area:
  - `SpawnTransform/SpawnPointRegistry` in `PrototypeScene`

`StageSpawnController` reads elapsed game time, resolves each `SpawnPointId` through
the Scene registry, and asks `PrototypeEnemyFactory` to create the configured Enemy
Prefab. Fixed components remain serialized in the Scene or Prefab; none of these
runtime data paths construct fixed components with `AddComponent`.

The current `StageSpawnSchedule` mirrors the drafted sheet: 14 WAVE_01 enemies at
1 second and one level-5 GoblinBoss at 120 seconds. Account EXP requirements use
`40, 60, 100, 200`, and score conversion uses `floor(score / 5)`.

The Player EXP requirements are provisional: levels 1–20 currently retain the old
prototype curve `3 + level * 2`. The final Player EXP sheet must replace these values.
The actual `.xlsx` importer and row-validation report are still pending; until they
exist, `SimpleGame/Data/Create or Update Data Assets` seeds the generated assets.
No production save system, account backend, analytics, or rewarded-ad SDK is included.

## Verification

- Unity script compilation: passed with no errors.
- EditMode combat and data tests: `42 passed / 0 failed`.
- Game data validation:
  - four Enemy balance rows and four Prefab mappings;
  - 15 scheduled spawns;
  - 28 Scene spawn points;
  - no duplicate runtime spawn ID or unresolved Enemy/SpawnPoint ID.
- Scene and generated data assets contain no missing script references.
