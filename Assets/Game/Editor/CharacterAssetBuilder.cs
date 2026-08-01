using System;
using System.Linq;
using SimpleGame;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGameEditor
{
    public static class CharacterAssetBuilder
    {
        public const string RootPath = "Assets/Game/Characters";
        public const string PrefabRootPath = "Assets/Prefab";
        public const string PlayerPrefabPath =
            PrefabRootPath + "/Player.prefab";
        public const string MeleePrefabPath =
            PrefabRootPath + "/MeleeEnemy.prefab";
        public const string RangedPrefabPath =
            PrefabRootPath + "/RangedEnemy.prefab";
        public const string ShieldPrefabPath =
            PrefabRootPath + "/ShieldEnemy.prefab";
        public const string BossPrefabPath =
            PrefabRootPath + "/BossEnemy.prefab";
        public const string MushroomBossPrefabPath =
            PrefabRootPath + "/MushroomBoss.prefab";
        public const string FlyingEyePrefabPath =
            PrefabRootPath + "/FlyingEyeEnemy.prefab";
        public const string FlyingEyeBossPrefabPath =
            PrefabRootPath + "/FlyingEyeBoss.prefab";
        public const string SkeletonBossPrefabPath =
            PrefabRootPath + "/SkeletonBoss.prefab";
        public const string MovingSlashPrefabPath =
            PrefabRootPath + "/MovingSlash.prefab";
        public const string FilthProjectilePrefabPath =
            PrefabRootPath + "/FilthProjectile.prefab";
        public const string HealthPickupPrefabPath =
            PrefabRootPath + "/HealthPickup.prefab";
        public const string PoisonCloudPrefabPath =
            PrefabRootPath + "/MushroomPoisonCloud.prefab";
        public const string PrototypeSquareAssetPath =
            RootPath + "/Shared/PrototypeSquare.asset";
        public const string AimDashAssetPath =
            RootPath + "/Shared/AimDash.asset";
        public const string AimEllipseAssetPath =
            RootPath + "/Shared/AimEllipse.asset";
        public const string AimArrowAssetPath =
            RootPath + "/Shared/AimArrow.asset";
        public const string DefaultFontPath =
            "Assets/Font/Pretendard-Regular SDF.asset";

        private const string AnimationPath = RootPath + "/Animations";
        private const string AnimatorPath = RootPath + "/Animators";
        private const string PlayerAnimationPath =
            AnimationPath + "/Player";
        private const string PlayerControllerPath =
            AnimatorPath + "/Player.controller";
        private const string SourceAssetRootPath =
            "Assets/SourceAssets";
        private const string SourcePlayerAnimationPath =
            SourceAssetRootPath +
            "/Bandits - Pixel Art/Animations/Light Bandit";
        private const string SourcePlayerControllerPath =
            SourcePlayerAnimationPath + "/LightBandit_AnimController.controller";
        private const string SourcePlayerPrefabPath =
            SourceAssetRootPath +
            "/Bandits - Pixel Art/Demo/LightBandit.prefab";
        private const string MovingSlashSpritePath =
            SourceAssetRootPath +
            "/Effects/MovingSlash_Crescent_6f.png";
        private const string SpriteBindingPath = "Visual/Sprite";

        [MenuItem("SimpleGame/Build Character Assets %#g")]
        public static void Build()
        {
            EnsureFolders();
            MigratePlayerAssets();
            LoadPrototypeSquareSprite();

            AnimationClip playerFaceRight = CreateFacingClip(
                $"{PlayerAnimationPath}/Player_FaceRight.anim",
                -1f);
            AnimationClip playerFaceLeft = CreateFacingClip(
                $"{PlayerAnimationPath}/Player_FaceLeft.anim",
                1f);
            AnimationClip enemyFaceRight = CreateFacingClip(
                $"{AnimationPath}/Common/EnemyFaceRight.anim",
                1f);
            AnimationClip enemyFaceLeft = CreateFacingClip(
                $"{AnimationPath}/Common/EnemyFaceLeft.anim",
                -1f);

            Sprite[] lightBandit = LoadSprites(
                SourceAssetRootPath +
                "/Bandits - Pixel Art/Sprites/LightBandit.png");
            Sprite[] playerIdle = Slice(lightBandit, 0, 4);
            Sprite[] playerMove = Slice(lightBandit, 8, 8);
            Sprite[] playerGuard = Slice(lightBandit, 4, 4);
            Sprite[] playerAttack = Slice(lightBandit, 16, 8);
            Sprite[] playerHurt = Slice(lightBandit, 32, 2);
            Sprite[] playerDeath = Slice(lightBandit, 35, 1);

            AnimatorController playerController = BuildProfile(
                "Player",
                playerIdle,
                playerMove,
                playerGuard,
                playerAttack,
                playerHurt,
                playerDeath,
                4f,
                10f,
                4f,
                10f,
                8f,
                1f,
                playerFaceRight,
                playerFaceLeft);

            Sprite[] goblinIdle = LoadSprites(
                SourceAssetRootPath +
                "/Monsters Creatures Fantasy/Sprites/Goblin/Idle.png");
            AnimatorController goblinController = BuildProfile(
                "Goblin",
                goblinIdle,
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Goblin/Run.png"),
                goblinIdle,
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Goblin/Attack1.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Goblin/Take Hit.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Goblin/Death.png"),
                12f,
                12f,
                12f,
                16f,
                12f,
                12f,
                enemyFaceRight,
                enemyFaceLeft,
                attack2Frames: LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Goblin/Attack2.png"),
                attack2Fps: 16f);

            Sprite[] skeletonIdle = LoadSprites(
                SourceAssetRootPath +
                "/Monsters Creatures Fantasy/Sprites/Skeleton/Idle.png");
            AnimatorController skeletonController = BuildProfile(
                "Skeleton",
                skeletonIdle,
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Skeleton/Walk.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Skeleton/Shield.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Skeleton/Attack1.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Skeleton/Take Hit.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Skeleton/Death.png"),
                12f,
                12f,
                12f,
                16f,
                12f,
                12f,
                enemyFaceRight,
                enemyFaceLeft,
                attack2Frames: LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Skeleton/Attack2.png"),
                attack2Fps: 16f);

            Sprite[] mushroomIdle = LoadSprites(
                SourceAssetRootPath +
                "/Monsters Creatures Fantasy/Sprites/Mushroom/Idle.png");
            AnimatorController mushroomController = BuildProfile(
                "Mushroom",
                mushroomIdle,
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Mushroom/Run.png"),
                mushroomIdle,
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Mushroom/Attack1.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Mushroom/Take Hit.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Mushroom/Death.png"),
                12f,
                12f,
                12f,
                14f,
                12f,
                12f,
                enemyFaceRight,
                enemyFaceLeft,
                attack2Frames: LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Mushroom/Attack2.png"),
                attack2Fps: 14f);

            Sprite[] flyingEyeIdle = LoadSprites(
                SourceAssetRootPath +
                "/Monsters Creatures Fantasy/Sprites/Flying eye/Flight.png");
            AnimatorController flyingEyeController = BuildProfile(
                "FlyingEye",
                flyingEyeIdle,
                flyingEyeIdle,
                flyingEyeIdle,
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Flying eye/Attack1.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Flying eye/Take Hit.png"),
                LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Flying eye/Death.png"),
                12f,
                12f,
                12f,
                16f,
                12f,
                12f,
                enemyFaceRight,
                enemyFaceLeft,
                attack2Frames: LoadSprites(
                    SourceAssetRootPath +
                    "/Monsters Creatures Fantasy/Sprites/Flying eye/Attack2.png"),
                attack2Fps: 16f);

            MovingSlashProjectile movingSlashPrefab =
                BuildMovingSlashPrefab();
            FilthProjectile filthProjectilePrefab =
                BuildFilthProjectilePrefab();
            BuildHealthPickupPrefab();
            BuildPoisonCloudPrefab();
            BuildPlayerPrefab(
                playerController,
                playerIdle[0],
                movingSlashPrefab,
                filthProjectilePrefab);
            BuildEnemyPrefab(
                EnemyArchetype.Melee,
                goblinController,
                goblinIdle[0],
                MeleePrefabPath);
            BuildEnemyPrefab(
                EnemyArchetype.Ranged,
                goblinController,
                goblinIdle[0],
                RangedPrefabPath);
            BuildEnemyPrefab(
                EnemyArchetype.Shield,
                skeletonController,
                skeletonIdle[0],
                ShieldPrefabPath);
            BuildEnemyPrefab(
                EnemyArchetype.Boss,
                goblinController,
                goblinIdle[0],
                BossPrefabPath);
            BuildEnemyPrefab(
                EnemyArchetype.Boss,
                mushroomController,
                mushroomIdle[0],
                MushroomBossPrefabPath);
            BuildEnemyPrefab(
                EnemyArchetype.Melee,
                flyingEyeController,
                flyingEyeIdle[0],
                FlyingEyePrefabPath);
            BuildEnemyPrefab(
                EnemyArchetype.Boss,
                flyingEyeController,
                flyingEyeIdle[0],
                FlyingEyeBossPrefabPath);
            BuildEnemyPrefab(
                EnemyArchetype.Boss,
                skeletonController,
                skeletonIdle[0],
                SkeletonBossPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Character assets created under {RootPath}");
        }

        private static AnimatorController BuildProfile(
            string profile,
            Sprite[] idleFrames,
            Sprite[] moveFrames,
            Sprite[] guardFrames,
            Sprite[] attackFrames,
            Sprite[] hurtFrames,
            Sprite[] deathFrames,
            float idleFps,
            float moveFps,
            float guardFps,
            float attackFps,
            float hurtFps,
            float deathFps,
            AnimationClip faceRight,
            AnimationClip faceLeft,
            string clipFolderOverride = null,
            string controllerPathOverride = null,
            string clipPrefixOverride = null,
            Sprite[] attack2Frames = null,
            float attack2Fps = 0f)
        {
            string clipFolder =
                clipFolderOverride ?? $"{AnimationPath}/{profile}";
            string clipPrefix = clipPrefixOverride ?? profile;
            string moveSuffix =
                clipFolderOverride == null ? "Move" : "Run";
            string guardSuffix =
                clipFolderOverride == null ? "Guard" : "CombatIdle";
            AnimationClip idle = CreateClip(
                $"{clipFolder}/{clipPrefix}_Idle.anim",
                idleFrames,
                idleFps,
                true);
            AnimationClip move = CreateClip(
                $"{clipFolder}/{clipPrefix}_{moveSuffix}.anim",
                moveFrames,
                moveFps,
                true);
            AnimationClip guard = CreateClip(
                $"{clipFolder}/{clipPrefix}_{guardSuffix}.anim",
                guardFrames,
                guardFps,
                true);
            AnimationClip attack = CreateClip(
                $"{clipFolder}/{clipPrefix}_Attack.anim",
                attackFrames,
                attackFps,
                false);
            AnimationClip attack2 =
                attack2Frames != null && attack2Frames.Length > 0
                    ? CreateClip(
                        $"{clipFolder}/{clipPrefix}_Attack2.anim",
                        attack2Frames,
                        attack2Fps > 0f
                            ? attack2Fps
                            : attackFps,
                        false)
                    : null;
            AnimationClip hurt = CreateClip(
                $"{clipFolder}/{clipPrefix}_Hurt.anim",
                hurtFrames,
                hurtFps,
                false);
            AnimationClip death = CreateClip(
                $"{clipFolder}/{clipPrefix}_Death.anim",
                deathFrames,
                deathFps,
                false);

            return CreateController(
                controllerPathOverride ??
                    $"{AnimatorPath}/{profile}.controller",
                idle,
                move,
                guard,
                attack,
                attack2,
                hurt,
                death,
                faceRight,
                faceLeft);
        }

        private static AnimationClip CreateClip(
            string path,
            Sprite[] frames,
            float frameRate,
            bool loop)
        {
            if (frames == null || frames.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Animation has no Sprite frames: {path}");
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = frameRate;
            ClearCurves(clip);
            var binding = new EditorCurveBinding
            {
                path = SpriteBindingPath,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = frames
                .Select(
                    (sprite, index) => new ObjectReferenceKeyframe
                    {
                        time = index / frameRate,
                        value = sprite
                    })
                .ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationClip CreateFacingClip(
            string path,
            float scaleX)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                AssetDatabase.CreateAsset(clip, path);
            }

            ClearCurves(clip);
            var binding = new EditorCurveBinding
            {
                path = SpriteBindingPath,
                type = typeof(Transform),
                propertyName = "m_LocalScale.x"
            };
            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                AnimationCurve.Constant(0f, 1f, scaleX));
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void ClearCurves(AnimationClip clip)
        {
            foreach (
                EditorCurveBinding binding in
                AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            }

            foreach (
                EditorCurveBinding binding in
                AnimationUtility.GetCurveBindings(clip))
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }
        }

        private static AnimatorController CreateController(
            string path,
            AnimationClip idleClip,
            AnimationClip moveClip,
            AnimationClip guardClip,
            AnimationClip attackClip,
            AnimationClip attack2Clip,
            AnimationClip hurtClip,
            AnimationClip deathClip,
            AnimationClip faceRightClip,
            AnimationClip faceLeftClip)
        {
            AnimatorController existing =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null && HasGameControllerStructure(existing))
            {
                EnsureControllerStates(
                    existing,
                    attack2Clip,
                    deathClip,
                    faceRightClip,
                    faceLeftClip);
                return existing;
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter(
                CharacterSpriteAnimator.MotionParameter,
                AnimatorControllerParameterType.Int);
            controller.AddParameter(
                CharacterSpriteAnimator.FaceLeftParameter,
                AnimatorControllerParameterType.Bool);
            controller.AddParameter(
                CharacterSpriteAnimator.AttackParameter,
                AnimatorControllerParameterType.Trigger);
            if (attack2Clip != null)
            {
                controller.AddParameter(
                    CharacterSpriteAnimator.Attack2Parameter,
                    AnimatorControllerParameterType.Trigger);
            }
            controller.AddParameter(
                CharacterSpriteAnimator.HurtParameter,
                AnimatorControllerParameterType.Trigger);
            controller.AddParameter(
                CharacterSpriteAnimator.DeathParameter,
                AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = AddState(machine, "Idle", idleClip, new Vector3(260f, 20f));
            AnimatorState move = AddState(machine, "Move", moveClip, new Vector3(520f, -80f));
            AnimatorState guard = AddState(machine, "Guard", guardClip, new Vector3(520f, 120f));
            AnimatorState attack = AddState(machine, "Attack", attackClip, new Vector3(780f, -80f));
            AnimatorState attack2 = attack2Clip != null
                ? AddState(
                    machine,
                    "Attack2",
                    attack2Clip,
                    new Vector3(780f, -180f))
                : null;
            AnimatorState hurt = AddState(machine, "Hurt", hurtClip, new Vector3(780f, 120f));
            AnimatorState death = AddState(machine, "Death", deathClip, new Vector3(1040f, 20f));
            machine.defaultState = idle;

            AddMotionTransition(idle, move, 1);
            AddMotionTransition(idle, guard, 2);
            AddMotionTransition(move, idle, 0);
            AddMotionTransition(move, guard, 2);
            AddMotionTransition(guard, idle, 0);
            AddMotionTransition(guard, move, 1);

            AddTriggerTransition(
                machine,
                attack,
                CharacterSpriteAnimator.AttackParameter);
            if (attack2 != null)
            {
                AddTriggerTransition(
                    machine,
                    attack2,
                    CharacterSpriteAnimator.Attack2Parameter);
            }
            AddTriggerTransition(
                machine,
                hurt,
                CharacterSpriteAnimator.HurtParameter);
            AddTriggerTransition(
                machine,
                death,
                CharacterSpriteAnimator.DeathParameter);

            AddReturnTransitions(attack, idle, move, guard);
            if (attack2 != null)
            {
                AddReturnTransitions(attack2, idle, move, guard);
            }
            AddReturnTransitions(hurt, idle, move, guard);

            controller.AddLayer("Facing");
            AnimatorControllerLayer[] layers = controller.layers;
            layers[1].defaultWeight = 1f;
            controller.layers = layers;
            AnimatorStateMachine facingMachine =
                controller.layers[1].stateMachine;
            AnimatorState faceRight = AddState(
                facingMachine,
                "FaceRight",
                faceRightClip,
                new Vector3(260f, 20f));
            AnimatorState faceLeft = AddState(
                facingMachine,
                "FaceLeft",
                faceLeftClip,
                new Vector3(520f, 20f));
            facingMachine.defaultState = faceRight;
            AddFacingTransition(faceRight, faceLeft, true);
            AddFacingTransition(faceLeft, faceRight, false);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static bool HasGameControllerStructure(
            AnimatorController controller)
        {
            bool hasMotion = controller.parameters.Any(
                parameter =>
                    parameter.name == CharacterSpriteAnimator.MotionParameter);
            bool hasFacing = controller.parameters.Any(
                parameter =>
                    parameter.name ==
                    CharacterSpriteAnimator.FaceLeftParameter);
            bool hasFacingLayer = controller.layers.Any(
                layer => layer.name == "Facing");
            return hasMotion && hasFacing && hasFacingLayer;
        }

        private static void EnsureControllerStates(
            AnimatorController controller,
            AnimationClip attack2Clip,
            AnimationClip deathClip,
            AnimationClip faceRightClip,
            AnimationClip faceLeftClip)
        {
            if (attack2Clip != null)
            {
                if (!controller.parameters.Any(
                        parameter =>
                            parameter.name ==
                            CharacterSpriteAnimator.Attack2Parameter))
                {
                    controller.AddParameter(
                        CharacterSpriteAnimator.Attack2Parameter,
                        AnimatorControllerParameterType.Trigger);
                }

                AnimatorStateMachine attackMachine =
                    controller.layers[0].stateMachine;
                AnimatorState attack2 = attackMachine.states
                    .Select(child => child.state)
                    .FirstOrDefault(state => state.name == "Attack2");
                if (attack2 == null)
                {
                    attack2 = AddState(
                        attackMachine,
                        "Attack2",
                        attack2Clip,
                        new Vector3(780f, -180f));
                    AnimatorState idle = attackMachine.states
                        .Select(child => child.state)
                        .First(state => state.name == "Idle");
                    AnimatorState move = attackMachine.states
                        .Select(child => child.state)
                        .First(state => state.name == "Move");
                    AnimatorState guard = attackMachine.states
                        .Select(child => child.state)
                        .First(state => state.name == "Guard");
                    AddReturnTransitions(
                        attack2,
                        idle,
                        move,
                        guard);
                }
                else
                {
                    attack2.motion = attack2Clip;
                }

                bool hasAttack2Transition =
                    attackMachine.anyStateTransitions.Any(
                        transition =>
                            transition.destinationState == attack2 &&
                            transition.conditions.Any(
                                condition =>
                                    condition.parameter ==
                                    CharacterSpriteAnimator
                                        .Attack2Parameter));
                if (!hasAttack2Transition)
                {
                    AddTriggerTransition(
                        attackMachine,
                        attack2,
                        CharacterSpriteAnimator.Attack2Parameter);
                }
            }

            if (!controller.parameters.Any(
                    parameter =>
                        parameter.name ==
                        CharacterSpriteAnimator.DeathParameter))
            {
                controller.AddParameter(
                    CharacterSpriteAnimator.DeathParameter,
                    AnimatorControllerParameterType.Trigger);
            }

            AnimatorStateMachine machine =
                controller.layers[0].stateMachine;
            AnimatorState death = machine.states
                .Select(child => child.state)
                .FirstOrDefault(state => state.name == "Death");
            if (death == null)
            {
                death = AddState(
                    machine,
                    "Death",
                    deathClip,
                    new Vector3(1040f, 20f));
            }
            else
            {
                death.motion = deathClip;
            }

            bool hasTransition = machine.anyStateTransitions.Any(
                transition =>
                    transition.destinationState == death &&
                    transition.conditions.Any(
                        condition =>
                            condition.parameter ==
                            CharacterSpriteAnimator.DeathParameter));
            if (!hasTransition)
            {
                AddTriggerTransition(
                    machine,
                    death,
                    CharacterSpriteAnimator.DeathParameter);
            }

            AnimatorControllerLayer facingLayer = controller.layers
                .First(layer => layer.name == "Facing");
            AnimatorStateMachine facingMachine =
                facingLayer.stateMachine;
            AnimatorState faceRight = facingMachine.states
                .Select(child => child.state)
                .First(state => state.name == "FaceRight");
            AnimatorState faceLeft = facingMachine.states
                .Select(child => child.state)
                .First(state => state.name == "FaceLeft");
            faceRight.motion = faceRightClip;
            faceLeft.motion = faceLeftClip;

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine,
            string name,
            AnimationClip clip,
            Vector3 position)
        {
            AnimatorState state = machine.AddState(name, position);
            state.motion = clip;
            return state;
        }

        private static void AddMotionTransition(
            AnimatorState source,
            AnimatorState destination,
            int motion)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                motion,
                CharacterSpriteAnimator.MotionParameter);
        }

        private static void AddTriggerTransition(
            AnimatorStateMachine machine,
            AnimatorState destination,
            string parameter)
        {
            AnimatorStateTransition transition =
                machine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = true;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddFacingTransition(
            AnimatorState source,
            AnimatorState destination,
            bool faceLeft)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(
                faceLeft
                    ? AnimatorConditionMode.If
                    : AnimatorConditionMode.IfNot,
                0f,
                CharacterSpriteAnimator.FaceLeftParameter);
        }

        private static void AddReturnTransitions(
            AnimatorState source,
            AnimatorState idle,
            AnimatorState move,
            AnimatorState guard)
        {
            AddReturnTransition(source, idle, 0);
            AddReturnTransition(source, move, 1);
            AddReturnTransition(source, guard, 2);
        }

        private static void AddReturnTransition(
            AnimatorState source,
            AnimatorState destination,
            int motion)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                motion,
                CharacterSpriteAnimator.MotionParameter);
        }

        private static MovingSlashProjectile BuildMovingSlashPrefab()
        {
            Sprite[] frames = LoadSprites(MovingSlashSpritePath);
            if (frames.Length !=
                MovingSlashProjectile.AnimationFrameCount)
            {
                throw new InvalidOperationException(
                    "Moving slash sprite sheet requires exactly " +
                    $"{MovingSlashProjectile.AnimationFrameCount} frames.");
            }

            var root = new GameObject("MovingSlash");
            SpriteRenderer renderer =
                root.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.flipX = true;
            renderer.sortingOrder = 24;
            MovingSlashProjectile projectile =
                root.AddComponent<MovingSlashProjectile>();
            projectile.ConfigureVisuals(renderer, frames);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                root,
                MovingSlashPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<MovingSlashProjectile>();
        }

        private static void BuildPlayerPrefab(
            RuntimeAnimatorController controller,
            Sprite idleSprite,
            MovingSlashProjectile movingSlashPrefab,
            FilthProjectile filthProjectilePrefab)
        {
            var root = new GameObject("Player");
            root.AddComponent<HealthComponent>();
            root.AddComponent<PlayerMovement>();
            root.AddComponent<CriticalSystem>();
            root.AddComponent<PlayerProgression>();
            root.AddComponent<PlayerStats>();
            PlayerController playerController =
                root.AddComponent<PlayerController>();
            CharacterSpriteAnimator animation =
                root.AddComponent<CharacterSpriteAnimator>();
            PlayerRoot playerRoot = root.AddComponent<PlayerRoot>();
            PlayerCombatAbilities combatAbilities =
                root.GetComponent<PlayerCombatAbilities>();
            FlyingSwordController flyingSwords =
                root.AddComponent<FlyingSwordController>();
            ConfigurePhysics(root, 0.34f);

            ConfigureVisual(
                root.transform,
                controller,
                idleSprite,
                1.65f,
                30,
                animation);
            Transform visual = root.transform.Find("Visual");
            Vector3[] readySwordPositions =
            {
                new(0.123f, 0.717f, 0f),
                new(-0.208f, 0.544f, 0f),
                new(0.171f, 0.376f, 1f)
            };
            var readySwordVisuals =
                new SpriteRenderer[FlyingSwordController.MaximumSwordCount];
            for (int index = 0;
                 index < readySwordVisuals.Length;
                 index++)
            {
                SpriteRenderer readySword = CreateSpriteVisual(
                    visual,
                    $"Flying_Sword{index + 1}",
                    Color.white,
                    new Vector2(0.052924413f, 0.4592f),
                    100);
                readySword.transform.localPosition =
                    readySwordPositions[index];
                readySword.gameObject.SetActive(false);
                readySwordVisuals[index] = readySword;
            }

            SpriteRenderer attackSword = CreateSpriteVisual(
                root.transform,
                "Flying_Sword_Attack",
                Color.white,
                new Vector2(0.038196404f, 6.480458f),
                100);
            attackSword.transform.localPosition =
                new Vector3(-5.25f, 7.03f, -0.15f);
            attackSword.transform.localRotation =
                Quaternion.Euler(-0.89f, -0.53f, 49.59f);
            attackSword.gameObject.SetActive(false);
            flyingSwords.ConfigureVisuals(
                readySwordVisuals,
                attackSword);

            SpriteRenderer cutting = CreateSpriteVisual(
                root.transform,
                "cutting",
                new Color(
                    0.06918234f,
                    0.06918234f,
                    0.06918234f,
                    1f),
                new Vector2(0.038196404f, 6.480458f),
                100);
            cutting.transform.localPosition =
                new Vector3(-5.25f, 7.03f, -0.15f);
            cutting.transform.localRotation =
                Quaternion.Euler(-0.89f, -0.53f, 49.59f);
            cutting.gameObject.SetActive(false);
            combatAbilities.ConfigureSeverVisual(cutting);
            combatAbilities.ConfigureMovingSlashPrefab(
                movingSlashPrefab);
            combatAbilities.ConfigureFilthProjectilePrefab(
                filthProjectilePrefab);

            SpriteRenderer aimRay = CreateSpriteVisual(
                root.transform,
                "AimRay",
                Color.white,
                Vector2.one,
                108);
            aimRay.sprite = LoadAimDashSprite();
            aimRay.drawMode = SpriteDrawMode.Tiled;
            aimRay.tileMode = SpriteTileMode.Continuous;
            aimRay.size = new Vector2(
                0.01f,
                PlayerController.AimRayWidth);
            SpriteRenderer aimEndpoint = CreateSpriteVisual(
                root.transform,
                "AimEndpoint",
                Color.white,
                Vector2.one * PlayerController.AimEndpointSize,
                109);
            aimEndpoint.sprite = LoadAimEllipseSprite();
            SpriteRenderer commandEndpoint = CreateSpriteVisual(
                root.transform,
                "CommandEndpoint",
                Color.white,
                Vector2.one * PlayerController.AimEndpointSize,
                110);
            commandEndpoint.sprite = LoadAimEllipseSprite();
            SpriteRenderer commandArrow = CreateSpriteVisual(
                root.transform,
                "CommandArrow",
                new Color(1f, 0f, 0f, 0.5f),
                Vector2.one * PlayerController.AimEndpointSize,
                111);
            commandArrow.sprite = LoadAimArrowSprite();
            playerController.ConfigureAimVisuals(
                aimRay,
                aimEndpoint,
                commandEndpoint,
                commandArrow);

            SpriteRenderer attackRange = CreateSpriteVisual(
                root.transform,
                "PlayerAttackRange",
                new Color(0.55f, 0.58f, 0.62f, 0.2f),
                Vector2.one * PlayerController.DefaultAttackRange * 2f,
                5);
            TMP_Text levelLabel = CreateWorldLabel(
                root.transform,
                "PLAYER",
                new Vector3(0f, 0.72f, 0f),
                2.5f,
                35);
            PlayerHealthBar healthBar = CreatePlayerWorldHealthBar(
                root.transform,
                -0.72f);
            playerRoot.ConfigureVisuals(
                attackRange,
                levelLabel,
                healthBar);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static FilthProjectile BuildFilthProjectilePrefab()
        {
            var root = new GameObject("FilthProjectile");
            SpriteRenderer orb = CreateSpriteVisual(
                root.transform,
                "Orb",
                new Color(0.34f, 0.2f, 0.08f, 1f),
                new Vector2(0.42f, 0.42f),
                27);
            orb.transform.localRotation =
                Quaternion.Euler(0f, 0f, 45f);

            var field = new GameObject("DamageField");
            field.transform.SetParent(root.transform, false);
            SpriteRenderer outer = CreateSpriteVisual(
                field.transform,
                "Outer",
                new Color(0.31f, 0.42f, 0.08f, 0.28f),
                Vector2.one,
                15);
            outer.transform.localRotation =
                Quaternion.Euler(0f, 0f, 45f);
            SpriteRenderer inner = CreateSpriteVisual(
                field.transform,
                "Inner",
                new Color(0.43f, 0.24f, 0.06f, 0.32f),
                Vector2.one * 0.68f,
                16);
            inner.transform.localRotation =
                Quaternion.Euler(0f, 0f, 15f);
            field.SetActive(false);

            FilthProjectile projectile =
                root.AddComponent<FilthProjectile>();
            projectile.ConfigureVisuals(orb, field);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                root,
                FilthProjectilePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<FilthProjectile>();
        }

        private static void BuildEnemyPrefab(
            EnemyArchetype archetype,
            RuntimeAnimatorController controller,
            Sprite idleSprite,
            string path)
        {
            var root = new GameObject($"{archetype}Enemy");
            root.AddComponent<EnemyHealth>();
            root.AddComponent<EnemyFacing>();
            root.AddComponent<EnemyMovement>();
            root.AddComponent<EnemyStateMachine>();
            CharacterSpriteAnimator animation =
                root.AddComponent<CharacterSpriteAnimator>();
            EnemyBase enemy;

            switch (archetype)
            {
                case EnemyArchetype.Melee:
                    root.AddComponent<EnemyAttackModule>();
                    enemy = root.AddComponent<MeleeEnemy>();
                    break;
                case EnemyArchetype.Ranged:
                    root.AddComponent<EnemyAttackModule>();
                    enemy = root.AddComponent<RangedEnemy>();
                    break;
                case EnemyArchetype.Shield:
                    enemy = root.AddComponent<ShieldEnemy>();
                    break;
                case EnemyArchetype.Boss:
                    root.AddComponent<BossAttackModule>();
                    enemy = root.AddComponent<BossEnemy>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(archetype),
                        archetype,
                        null);
            }

            ConfigurePhysics(
                root,
                archetype == EnemyArchetype.Boss ? 0.62f : 0.38f);
            ConfigureVisual(
                root.transform,
                controller,
                idleSprite,
                archetype == EnemyArchetype.Boss ? 2.3f : 1.25f,
                20,
                animation);
            if (archetype == EnemyArchetype.Shield)
            {
                animation.ConfigureTintPulse(
                    new Color(0.25f, 0.7f, 1f, 1f),
                    2.4f);
            }

            SpriteRenderer approachRange = archetype == EnemyArchetype.Shield
                ? CreateSpriteVisual(
                    root.transform,
                    "ShieldApproachRange",
                    new Color(0.15f, 0.8f, 0.95f, 0.18f),
                    Vector2.one * 4.5f,
                    4)
                : null;
            SpriteRenderer facingMarker = CreateSpriteVisual(
                root.transform,
                "FacingMarker",
                Color.yellow,
                new Vector2(0.18f, 0.35f),
                24);
            facingMarker.enabled = false;
            TMP_Text levelLabel = CreateWorldLabel(
                root.transform,
                $"{PrototypeEnemyDefinitions.GetDisplayName(archetype)} " +
                "레벨 1",
                new Vector3(0f, archetype == EnemyArchetype.Boss ? 1.1f : 0.67f, 0f),
                2.3f,
                26);
            EnemyHealthBar healthBar = CreateWorldHealthBar(
                root.transform,
                archetype == EnemyArchetype.Boss ? 1.48f : 0.93f);
            enemy.ConfigureVisuals(
                approachRange,
                facingMarker,
                levelLabel,
                healthBar);

            EnemyAttackModule attack = root.GetComponent<EnemyAttackModule>();
            if (attack != null)
            {
                SpriteRenderer warning = CreateSpriteVisual(
                    root.transform,
                    "AttackWarning",
                    new Color(1f, 0f, 0f, 0.34f),
                    Vector2.one,
                    5);
                warning.enabled = false;
                attack.ConfigureIndicator(warning);
            }

            BossAttackModule bossAttack = root.GetComponent<BossAttackModule>();
            if (bossAttack != null)
            {
                SpriteRenderer warning = CreateSpriteVisual(
                    root.transform,
                    "BossAttackWarning",
                    new Color(1f, 0.05f, 0.05f, 0.42f),
                    new Vector2(2.4f, 2.4f),
                    6);
                warning.transform.localPosition = new Vector3(0f, -1.5f, 0f);
                warning.enabled = false;
                bossAttack.ConfigureIndicator(warning);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildHealthPickupPrefab()
        {
            var root = new GameObject("HealthPickup");
            root.AddComponent<HealthPickup>();
            ConfigurePhysics(root, 0.4f);

            SpriteRenderer diamond = CreateSpriteVisual(
                root.transform,
                "Orb",
                new Color(0.95f, 0.2f, 0.28f, 0.95f),
                new Vector2(0.72f, 0.72f),
                18);
            diamond.transform.localRotation =
                Quaternion.Euler(0f, 0f, 45f);
            CreateSpriteVisual(
                root.transform,
                "CrossHorizontal",
                Color.white,
                new Vector2(0.42f, 0.12f),
                19);
            CreateSpriteVisual(
                root.transform,
                "CrossVertical",
                Color.white,
                new Vector2(0.12f, 0.42f),
                19);

            PrefabUtility.SaveAsPrefabAsset(
                root,
                HealthPickupPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildPoisonCloudPrefab()
        {
            var root = new GameObject("MushroomPoisonCloud");
            root.AddComponent<MushroomPoisonCloud>();

            SpriteRenderer core = CreateSpriteVisual(
                root.transform,
                "CloudCore",
                new Color(0.25f, 0.72f, 0.16f, 0.24f),
                Vector2.one * MushroomPoisonCloud.DamageRadius * 2f,
                16);
            core.transform.localRotation =
                Quaternion.Euler(0f, 0f, 45f);
            SpriteRenderer inner = CreateSpriteVisual(
                root.transform,
                "CloudInner",
                new Color(0.48f, 0.9f, 0.16f, 0.2f),
                Vector2.one * MushroomPoisonCloud.DamageRadius * 1.45f,
                17);
            inner.transform.localRotation =
                Quaternion.Euler(0f, 0f, 15f);

            PrefabUtility.SaveAsPrefabAsset(
                root,
                PoisonCloudPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ConfigureVisual(
            Transform root,
            RuntimeAnimatorController controller,
            Sprite idleSprite,
            float scale,
            int sortingOrder,
            CharacterSpriteAnimator animation)
        {
            Animator animator = root.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root, false);
            visual.transform.localScale = Vector3.one * scale;

            var spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(visual.transform, false);
            SpriteRenderer renderer =
                spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = idleSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;

            animation.Configure(animator, renderer);
        }

        public static Sprite LoadPrototypeSquareSprite()
        {
            Sprite existing = AssetDatabase
                .LoadAllAssetsAtPath(PrototypeSquareAssetPath)
                .OfType<Sprite>()
                .FirstOrDefault();
            if (existing != null)
            {
                return existing;
            }

            var texture = new Texture2D(1, 1)
            {
                name = "PrototypeSquareTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, PrototypeSquareAssetPath);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            sprite.name = "PrototypeSquare";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        private static Sprite LoadAimDashSprite()
        {
            return LoadOrCreateProceduralSprite(
                AimDashAssetPath,
                "AimDash",
                16,
                4,
                40f,
                (x, _) => x < 9,
                TextureWrapMode.Repeat);
        }

        private static Sprite LoadAimEllipseSprite()
        {
            return LoadOrCreateProceduralSprite(
                AimEllipseAssetPath,
                "AimEllipse",
                64,
                40,
                64f,
                (x, y) =>
                {
                    float offsetX = x - 31.5f;
                    float offsetY = y - 19.5f;
                    float outer =
                        offsetX * offsetX / (30f * 30f) +
                        offsetY * offsetY / (18f * 18f);
                    float inner =
                        offsetX * offsetX / (26f * 26f) +
                        offsetY * offsetY / (14f * 14f);
                    return outer <= 1f && inner >= 1f;
                },
                TextureWrapMode.Clamp);
        }

        private static Sprite LoadAimArrowSprite()
        {
            return LoadOrCreateProceduralSprite(
                AimArrowAssetPath,
                "AimArrow",
                64,
                40,
                64f,
                (x, y) =>
                {
                    float vertical = Mathf.Abs(y - 19.5f);
                    bool shaft = x >= 10 && x <= 40 && vertical <= 2.5f;
                    bool head = x >= 36 && x <= 54 &&
                        vertical <= (54f - x) * (10f / 18f);
                    return shaft || head;
                },
                TextureWrapMode.Clamp);
        }

        private static Sprite LoadOrCreateProceduralSprite(
            string path,
            string spriteName,
            int width,
            int height,
            float pixelsPerUnit,
            Func<int, int, bool> isOpaque,
            TextureWrapMode wrapMode)
        {
            Sprite existing = AssetDatabase
                .LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .FirstOrDefault();
            if (existing != null)
            {
                return existing;
            }

            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false)
            {
                name = spriteName + "Texture",
                filterMode = FilterMode.Point,
                wrapMode = wrapMode
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = isOpaque(x, y)
                        ? Color.white
                        : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = spriteName;
            AssetDatabase.AddObjectToAsset(sprite, texture);
            EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        public static SpriteRenderer CreateSpriteVisual(
            Transform parent,
            string name,
            Color color,
            Vector2 size,
            int sortingOrder)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadPrototypeSquareSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        public static TextMeshPro CreateWorldLabel(
            Transform parent,
            string text,
            Vector3 localPosition,
            float fontSize,
            int sortingOrder)
        {
            var child = new GameObject("LevelLabel");
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;

            TextMeshPro label = child.AddComponent<TextMeshPro>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                DefaultFontPath);
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            child.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            return label;
        }

        private static PlayerHealthBar CreatePlayerWorldHealthBar(
            Transform parent,
            float localY)
        {
            var canvasObject = new GameObject(
                "HealthBarCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition =
                new Vector3(0f, localY, 0f);
            canvasObject.transform.localScale =
                Vector3.one * 0.01f;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 40;

            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(105f, 12f);

            var sliderObject = new GameObject(
                "HealthSlider",
                typeof(RectTransform),
                typeof(Slider));
            sliderObject.transform.SetParent(
                canvasObject.transform,
                false);
            RectTransform sliderRect =
                sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            Image background = CreateHealthBarImage(
                sliderObject.transform,
                "Background",
                new Color(0.08f, 0.08f, 0.08f, 0.95f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Image fill = CreateHealthBarImage(
                sliderObject.transform,
                "Fill",
                new Color(0.15f, 0.85f, 0.25f, 1f),
                Vector2.zero,
                Vector2.one,
                new Vector2(1.5f, 1.5f),
                new Vector2(-1.5f, -1.5f));

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = background;

            PlayerHealthBar healthBar =
                parent.gameObject.AddComponent<PlayerHealthBar>();
            healthBar.Configure(canvasObject, slider);
            return healthBar;
        }

        private static EnemyHealthBar CreateWorldHealthBar(
            Transform parent,
            float localY)
        {
            var canvasObject = new GameObject(
                "HealthBarCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition =
                new Vector3(0f, localY, 0f);
            canvasObject.transform.localScale =
                Vector3.one * 0.01f;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 40;

            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(120f, 20f);

            var sliderObject = new GameObject(
                "HealthSlider",
                typeof(RectTransform),
                typeof(Slider));
            sliderObject.transform.SetParent(
                canvasObject.transform,
                false);
            RectTransform sliderRect =
                sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            Image background = CreateHealthBarImage(
                sliderObject.transform,
                "Background",
                new Color(0.08f, 0.08f, 0.08f, 0.95f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Image fill = CreateHealthBarImage(
                sliderObject.transform,
                "Fill",
                new Color(0.15f, 0.85f, 0.25f, 1f),
                Vector2.zero,
                Vector2.one,
                new Vector2(2f, 2f),
                new Vector2(-2f, -2f));

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = background;

            var labelObject = new GameObject(
                "HealthValue",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(
                canvasObject.transform,
                false);
            TextMeshProUGUI label =
                labelObject.GetComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                DefaultFontPath);
            label.text = "3/3";
            label.fontSize = 13f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            EnemyHealthBar healthBar =
                parent.gameObject.AddComponent<EnemyHealthBar>();
            healthBar.Configure(canvasObject, slider, label);
            return healthBar;
        }

        private static Image CreateHealthBarImage(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect =
                imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigurePhysics(GameObject root, float radius)
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = radius;
            collider.isTrigger = true;
        }

        private static Sprite[] LoadSprites(string path)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => GetFrameIndex(sprite.name))
                .ToArray();
            if (sprites.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No Sprite frames found: {path}");
            }

            return sprites;
        }

        private static Sprite[] Slice(Sprite[] source, int start, int count)
        {
            if (source.Length < start + count)
            {
                throw new InvalidOperationException(
                    $"Sprite sheet has {source.Length} frames; " +
                    $"requested {start}..{start + count - 1}.");
            }

            var frames = new Sprite[count];
            Array.Copy(source, start, frames, 0, count);
            return frames;
        }

        private static int GetFrameIndex(string spriteName)
        {
            int separator = spriteName.LastIndexOf('_');
            return separator >= 0 &&
                int.TryParse(spriteName[(separator + 1)..], out int index)
                    ? index
                    : 0;
        }

        private static void EnsureFolders()
        {
            EditorAssetUtility.EnsureFolder(AnimationPath + "/Common");
            EditorAssetUtility.EnsureFolder(PlayerAnimationPath);
            EditorAssetUtility.EnsureFolder(AnimationPath + "/Goblin");
            EditorAssetUtility.EnsureFolder(AnimationPath + "/Skeleton");
            EditorAssetUtility.EnsureFolder(AnimationPath + "/Mushroom");
            EditorAssetUtility.EnsureFolder(AnimationPath + "/FlyingEye");
            EditorAssetUtility.EnsureFolder(AnimatorPath);
            EditorAssetUtility.EnsureFolder(RootPath + "/Shared");
            EditorAssetUtility.EnsureFolder(PrefabRootPath);
        }

        private static void MigratePlayerAssets()
        {
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_Idle.anim",
                PlayerAnimationPath + "/Player_Idle.anim");
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_Run.anim",
                PlayerAnimationPath + "/Player_Move.anim");
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_CombatIdle.anim",
                PlayerAnimationPath + "/Player_Guard.anim");
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_Attack.anim",
                PlayerAnimationPath + "/Player_Attack.anim");
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_Hurt.anim",
                PlayerAnimationPath + "/Player_Hurt.anim");
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_Death.anim",
                PlayerAnimationPath + "/Player_Death.anim");
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_Jump.anim",
                PlayerAnimationPath + "/Player_Jump.anim");
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_Recover.anim",
                PlayerAnimationPath + "/Player_Recover.anim");
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_FaceRight.anim",
                PlayerAnimationPath + "/Player_FaceRight.anim");
            MoveAssetIfNeeded(
                SourcePlayerAnimationPath + "/LightBandit_FaceLeft.anim",
                PlayerAnimationPath + "/Player_FaceLeft.anim");
            MoveAssetIfNeeded(
                SourcePlayerControllerPath,
                PlayerControllerPath);
            MoveAssetIfNeeded(
                SourcePlayerPrefabPath,
                PlayerPrefabPath);
        }

        private static void MoveAssetIfNeeded(
            string sourcePath,
            string destinationPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(destinationPath) != null ||
                AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
            {
                return;
            }

            string error = AssetDatabase.MoveAsset(
                sourcePath,
                destinationPath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException(error);
            }
        }

    }
}
