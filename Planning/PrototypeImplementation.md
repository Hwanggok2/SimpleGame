# Prototype Implementation

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

- Unity script compilation: passed with no errors or warnings.
- EditMode combat-rule tests: `10 passed / 0 failed`.
- Scene validation: `0` missing scripts, `0` broken prefabs.
- PlayMode smoke test:
  - six enemies spawned;
  - level `1 -> 2`;
  - Critical card `0% -> 10%`;
  - Game Over entered when Castle HP reached zero;
  - Continue restored Castle to `15/30` with invulnerability.
