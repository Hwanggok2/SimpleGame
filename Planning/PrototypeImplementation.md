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
- Player attacks have no automatic click cooldown. Every valid Enemy click creates one attack request; repeated clicks during approach are queued for the same target.
- Prototype Enemy touch correction uses a `1.5` world-unit radius instead of the previous `1.1` radius.
- Player visual: `Resources/Bandits - Pixel Art/Sprites/LightBandit`.
- Melee, Ranged, and Boss Enemy visual: `Resources/Monsters Creatures Fantasy/Sprites/Goblin`.
- ShieldEnemy visual: `Resources/Monsters Creatures Fantasy/Sprites/Skeleton`.
- Shared `CharacterSpriteAnimator` drives Idle, Move, Guard, Attack, and Hit frames. Attack starts at the actual hit resolution and Hit starts only when damage is applied; Player front recoil also uses Hit.
- ShieldEnemy plays Skeleton Walk while pursuing the Player outside its cyan approach range. It stops and immediately loops Shield when the Player enters that range; after a Hit one-shot it returns to Shield while the Player remains inside.
- The supplied sheets are side-view assets. Horizontal movement/targets flip the sprite; vertical movement plays the character's movement animation while preserving the last horizontal facing.

## Temporary prototype data

The planning documents do not yet fix the wave table, spawn positions, enemy numerical
stats, Player EXP curve, Player/Castle maximum HP, or production ad flow. The prototype
therefore isolates temporary values in:

- `PrototypeEnemyDefinitions`
- `PrototypeGameSession.SpawnPrototypeSet`
- `PlayerProgression.RequiredExperience`
- `PlayerRoot.Configure`
- `CastleRoot.Configure`

These values should be replaced by ScriptableObject or external table data after the
missing design data is approved. No production save system, account backend, analytics,
or rewarded-ad SDK is included.

## Verification

- Unity script compilation: passed with no errors. One pre-existing TMP word-wrapping obsolete warning remains in the Editor-only scene builder.
- EditMode combat-rule tests: `10 passed / 0 failed`.
- Scene validation: `0` missing scripts, `0` broken prefabs.
- PlayMode smoke test:
  - six enemies spawned;
  - level `1 -> 2`;
  - Critical card `0% -> 10%`;
  - Game Over entered when Castle HP reached zero;
  - Continue restored Castle to `15/30` with invulnerability.
