# Prototype Implementation

- Last updated: 2026-08-02

## Final release target

- Final platform: Apps in Toss game mini-app inside the Toss app.
- Delivery: Unity WebGL packaged as `.ait` through the official Apps in Toss Unity SDK.
- Unity Editor and ordinary browser runs are development environments only.
- The official SDK, app ID, icon URL, production ad group IDs, and sandbox-device validation are not configured yet.
- Gameplay code remains independent from the host SDK. Future Apps in Toss calls belong at the platform boundary and must use asynchronous APIs without blocking the Unity main thread.

## Scene

- Entry path: `Assets/Scenes/Lobby.unity`
- Battle path: `Assets/Scenes/Battle.unity`
- Build Settings order is `Lobby` then `Battle`; the old
  `PrototypeScene.unity` path is no longer generated or referenced.
- Portrait reference resolution: `1080 x 1920`
- Runtime hierarchy:
  - `PrototypeSystems`: enemy factory, game session, stage spawner, enemy recycler
  - `Entities/Player`: Facade + health, movement, critical, progression, controller, world area
  - `Entities/Player/SpawnTransform`: 28 Player-relative spawn transforms + `SpawnPointRegistry`
  - `Entities/Enemies`: spawned enemy Facades and their state/attack modules
  - `WorldGrid`: `3 x 3` active Tilemap chunks managed by `WorldChunkGrid`
  - `Main Camera`: Player follow + additive camera shake
  - `PrototypeHUD`: enum-name binding View + Presenter

## Controls

- Tap/click an empty point: move the Player.
- Tap/click an enemy: approach and attack.
- Empty-point movement uses a base cruise speed of `10 units/s`.
- A path Enemy is approached at `1.1x` the current speed.
- After a path Enemy is killed, the remaining route uses `1.2x` speed without stacking per Enemy.
- `PAUSE`: pause/resume.
- `PLAYER -10`: apply debug damage to the Player and verify Game Over.
- `PLAYER XP +5`: trigger the prototype level-up flow.
- `CONTINUE (AD TEST)`: simulate a successful rewarded-ad result after Game Over.

## Implemented rules

- Combat uses actual float HP and damage rather than armor or front-damage immunity.
- Player attack power and Enemy maximum HP use the same `1.7` per-level growth multiplier.
- Rear attacks deal `3x`; critical attacks multiply the resolved directional damage by `3x`.
- Lower-level one-hit exceptions remain data-driven per Enemy type.
- Every Enemy Prefab has a world-space HP Slider and numeric current/maximum HP label.
- Level-up and starting selections draw three distinct eligible cards by weight.
- Implemented cards: critical chance, maximum HP, movement speed, and attack range.
- Boss three-second cycle:
  - `0.0–1.5`: move with warning area.
  - `1.5–2.0`: stop and apply the attack.
  - `2.0–3.0`: move toward the Player.
- Player-death Game Over and rewarded continue:
  - maximum two continues;
  - Player restores to maximum HP;
  - normal enemies move to the opposite Player-relative Spawn boundary;
  - Boss repositioning and post-continue invulnerability remain undecided.
- Every Enemy targets the living Player; no Castle target or fallback remains.
- Melee enemies preserve the attack-start facing through telegraph and hit resolution.
- Chase-facing changes from left to right or right to left require the new side to remain valid for `0.5s`.
- Ranged enemies track the Player freely while aiming, lock facing and movement for `1s` after firing, and cannot fire again until `2s` after the shot.
- ShieldEnemy preserves its Shield direction for at least `0.8s`; an opposite-side direction is queued and applied after that hold instead of flipping immediately.
- `CameraFollowController` follows the Player's world position. `CameraShakeController` composes a temporary offset without replacing the follow position.
- The world keeps nine `20.48 x 20.48` Tilemap chunks around the Player. `WorldChunkGrid` moves only obsolete chunks to newly required coordinates.
- Four saved ground Tile assets seed the nine active chunks; active chunk count and authored source variation count are separate.
- `PlayerWorldArea` calculates a camera-external Spawn boundary and a larger recycle boundary.
- `EnemyWorldRecycler` preserves normal Enemy type, level, and accumulated damage while cancelling transient attack/movement state and moving the Enemy to the opposite Spawn boundary. Bosses and dead enemies are excluded.
- Account EXP conversion: `floor(score / 5)`.
- UI binding uses enum names as GameObject names and enum indices as cached component slots.
- Player movement uses `Vector2.MoveTowards` with units-per-second data instead of a destination duration. The base speed is `10`; `MOVE_SPEED_UP` adds `8` up to five times, reaching `50 units/s`.
- `SmoothDamp` eases into cruise speed and a distance-based `SmootherStep` curve eases into the destination. Momentum is preserved only while sweeping through one-hit enemies.
- Empty movement uses `1.0x`, a path Enemy approach uses `1.1x`, and movement after a real path kill uses `1.2x`. These multipliers never stack.
- A movement path checks the Player and Enemy Collider radii. The first Enemy intersecting the path becomes the attack target even when the user touched empty space behind it.
- A one-hit kill resumes movement to the original touch point at `1.2x`; an Enemy that survives keeps the Player stopped at the attack-range edge.
- The same touch command repeatedly acquires the next Enemy intersecting its remaining path. Consecutive one-hit kills keep the `1.2x` escape speed; the first survivor is attacked once and stops the Player at the attack-range edge.
- Enemy name and level labels use the normal, non-critical combat table relative to the current Player level: green for one hit from either side, white for three front hits/one rear hit, and red for every other case. Labels refresh after Player level-up.
- Player attacks have no automatic click cooldown. Every valid Enemy click creates one attack request; repeated clicks during approach are queued for the same target.
- Prototype Enemy touch correction uses a `1.5` world-unit radius instead of the previous `1.1` radius.
- Player Prefab: `Assets/Prefab/Player.prefab`.
- Player AnimationClips: `Assets/Game/Characters/Animations/Player`.
- Player AnimatorController: `Assets/Game/Characters/Animators/Player.controller`.
- Melee, Ranged, and Boss Enemy visual: `Assets/SourceAssets/Monsters Creatures Fantasy/Sprites/Goblin`.
- ShieldEnemy visual: `Assets/SourceAssets/Monsters Creatures Fantasy/Sprites/Skeleton`.
- All gameplay, UI, map, and effect Prefabs are stored directly under `Assets/Prefab`. Character AnimationClips and AnimatorControllers remain under `Assets/Game/Characters`, while only source Sprite Sheets remain under `Assets/SourceAssets`.
- `PrototypeEnemyFactory` uses a lazy pool keyed by saved Enemy Prefab. It reuses fully reset inactive instances first, instantiates only when that prefab's pool is empty, and caps inactive storage at 32 instances per prefab and 96 instances in total.
- Player and Enemy Prefabs serialize one Rigidbody2D, one Collider2D, and all fixed range, warning, marker, and label objects. Camera follow/shake, world area, world recycler, nine Tilemap chunks, and CombatFeedback components are saved in the Scene; runtime code contains no `AddComponent` or fixed-child construction.
- Shared `CharacterSpriteAnimator` only forwards Motion, FaceLeft, Attack, Hurt, and Death parameters to each Prefab's Animator. It does not load Sprite arrays or advance frames in code.
- Each character Prefab has exactly one Animator and its Controller on the Prefab root. Every AnimationClip binds to the shared `Visual/Sprite` child path.
- The Animator base layer drives Idle, Move, Guard, Attack, Hurt, and Death. A separate Facing layer drives saved direction clips instead of changing `SpriteRenderer.flipX` in code.
- The source sheets do not share a native direction: LightBandit faces left at scale X `+1`, while Goblin and Skeleton face right at scale X `+1`. Player Facing therefore uses Left `+1`/Right `-1`, and Enemy Facing uses Left `-1`/Right `+1`.
- Enemy death immediately stops movement and attacks, disables its Collider and gameplay markers, plays the profile Death clip, and only then disables the GameObject. Player lethal damage also enters its Death state and `RestoreAfterContinue` explicitly returns the Animator to Idle.
- ShieldEnemy plays Skeleton Walk while pursuing the Player outside its cyan approach range. It stops and immediately loops Shield when the Player enters that range; after a Hit one-shot it returns to Shield while the Player remains inside.
- ShieldEnemy keeps its current Shield direction for `0.8s`; a sustained opposite-side position is queued and applied only after the hold.
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
  - `Generated/PlayerBalanceTable.asset`
  - `Generated/LevelUpCardTable.asset`
- Unity-authored area:
  - `Catalogs/EnemyAssetCatalog.asset`
  - `Profiles/CombatFeedbackProfile.asset`
- Scene-authored area:
  - Player-relative `SpawnTransform/SpawnPointRegistry`
  - `WorldGrid` with nine active Tilemaps
  - four Tile assets under `Assets/Game/World/Tiles`

The source workbook is `Planning/GameData.xlsx`. After saving Excel, run
`SimpleGame > Data > Import Excel` in Edit Mode. The importer validates every
required sheet before changing any generated asset; a failed import reports the
sheet, row, and column where possible and preserves the previously valid SO data.
`Import Excel From...` can be used for an alternate `.xlsx` file.

`StageSpawnController` reads elapsed game time, resolves each `SpawnPointId` through
the Scene registry, and asks `PrototypeEnemyFactory` to create the configured Enemy
Prefab. Fixed components remain serialized in the Scene or Prefab; none of these
runtime data paths construct fixed components with `AddComponent`.

The current `StageSpawnSchedule` contains 29 rows. Account EXP requirements use
`40, 60, 100, 200`, and score conversion uses `floor(score / 5)`.

The Player EXP requirements are provisional: levels 1–20 currently use
`3 + level * 2`. The final Player EXP sheet may replace these values.
The `.xlsx` importer validates seven required sheets and updates the generated
ScriptableObjects only after all validation succeeds.
No production save system, account backend, analytics, or rewarded-ad SDK is included.

## Verification

- Unity script compilation: passed with no errors.
- EditMode combat, data, facing, movement-speed, and world-area tests: `41 passed / 0 failed`.
- Prototype Scene Play Mode smoke test: entered and exited successfully with `0` Console errors.
- Game data validation:
  - four Enemy balance rows and four Prefab mappings;
  - 29 scheduled spawns;
  - one Player balance row and four level-up card rows;
  - 28 Scene spawn points;
  - no duplicate runtime spawn ID or unresolved Enemy/SpawnPoint ID.
- Scene and generated data assets contain no missing script references.
