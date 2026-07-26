using System;
using System.Linq;
using SimpleGame;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SimpleGameEditor
{
    public static class CharacterAssetBuilder
    {
        public const string RootPath = "Assets/Game/Characters";
        public const string PlayerPrefabPath =
            "Assets/Resources/Bandits - Pixel Art/Demo/LightBandit.prefab";
        public const string MeleePrefabPath =
            RootPath + "/Prefabs/Enemies/MeleeEnemy.prefab";
        public const string RangedPrefabPath =
            RootPath + "/Prefabs/Enemies/RangedEnemy.prefab";
        public const string ShieldPrefabPath =
            RootPath + "/Prefabs/Enemies/ShieldEnemy.prefab";
        public const string BossPrefabPath =
            RootPath + "/Prefabs/Enemies/BossEnemy.prefab";
        public const string PrototypeSquareAssetPath =
            RootPath + "/Shared/PrototypeSquare.asset";

        private const string AnimationPath = RootPath + "/Animations";
        private const string AnimatorPath = RootPath + "/Animators";
        private const string LightBanditAnimationPath =
            "Assets/Resources/Bandits - Pixel Art/Animations/Light Bandit";
        private const string LightBanditControllerPath =
            LightBanditAnimationPath + "/LightBandit_AnimController.controller";
        private const string LegacyPlayerAnimationPath =
            AnimationPath + "/Player";
        private const string LegacyPlayerControllerPath =
            AnimatorPath + "/Player.controller";
        private const string LegacyPlayerPrefabPath =
            RootPath + "/Prefabs/Player";
        private const string LegacyPlayerFaceRightPath =
            AnimationPath + "/Common/FaceRight.anim";
        private const string LegacyPlayerFaceLeftPath =
            AnimationPath + "/Common/FaceLeft.anim";
        private const string SpriteBindingPath = "Visual/Sprite";

        [MenuItem("SimpleGame/Build Character Assets %#g")]
        public static void Build()
        {
            EnsureFolders();
            RemoveLegacyPlayerAssets();
            LoadPrototypeSquareSprite();

            AnimationClip playerFaceRight = CreateFacingClip(
                $"{LightBanditAnimationPath}/LightBandit_FaceRight.anim",
                -1f);
            AnimationClip playerFaceLeft = CreateFacingClip(
                $"{LightBanditAnimationPath}/LightBandit_FaceLeft.anim",
                1f);
            AnimationClip enemyFaceRight = CreateFacingClip(
                $"{AnimationPath}/Common/EnemyFaceRight.anim",
                1f);
            AnimationClip enemyFaceLeft = CreateFacingClip(
                $"{AnimationPath}/Common/EnemyFaceLeft.anim",
                -1f);

            Sprite[] lightBandit = LoadSprites(
                "Assets/Resources/Bandits - Pixel Art/Sprites/LightBandit.png");
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
                playerFaceLeft,
                LightBanditAnimationPath,
                LightBanditControllerPath,
                "LightBandit");

            Sprite[] goblinIdle = LoadSprites(
                "Assets/Resources/Monsters Creatures Fantasy/Sprites/Goblin/Idle.png");
            AnimatorController goblinController = BuildProfile(
                "Goblin",
                goblinIdle,
                LoadSprites(
                    "Assets/Resources/Monsters Creatures Fantasy/Sprites/Goblin/Run.png"),
                goblinIdle,
                LoadSprites(
                    "Assets/Resources/Monsters Creatures Fantasy/Sprites/Goblin/Attack1.png"),
                LoadSprites(
                    "Assets/Resources/Monsters Creatures Fantasy/Sprites/Goblin/Take Hit.png"),
                LoadSprites(
                    "Assets/Resources/Monsters Creatures Fantasy/Sprites/Goblin/Death.png"),
                12f,
                12f,
                12f,
                16f,
                12f,
                12f,
                enemyFaceRight,
                enemyFaceLeft);

            Sprite[] skeletonIdle = LoadSprites(
                "Assets/Resources/Monsters Creatures Fantasy/Sprites/Skeleton/Idle.png");
            AnimatorController skeletonController = BuildProfile(
                "Skeleton",
                skeletonIdle,
                LoadSprites(
                    "Assets/Resources/Monsters Creatures Fantasy/Sprites/Skeleton/Walk.png"),
                LoadSprites(
                    "Assets/Resources/Monsters Creatures Fantasy/Sprites/Skeleton/Shield.png"),
                LoadSprites(
                    "Assets/Resources/Monsters Creatures Fantasy/Sprites/Skeleton/Attack1.png"),
                LoadSprites(
                    "Assets/Resources/Monsters Creatures Fantasy/Sprites/Skeleton/Take Hit.png"),
                LoadSprites(
                    "Assets/Resources/Monsters Creatures Fantasy/Sprites/Skeleton/Death.png"),
                12f,
                12f,
                12f,
                16f,
                12f,
                12f,
                enemyFaceRight,
                enemyFaceLeft);

            BuildPlayerPrefab(playerController, playerIdle[0]);
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
            string clipPrefixOverride = null)
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
            AddTriggerTransition(
                machine,
                hurt,
                CharacterSpriteAnimator.HurtParameter);
            AddTriggerTransition(
                machine,
                death,
                CharacterSpriteAnimator.DeathParameter);

            AddReturnTransitions(attack, idle, move, guard);
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
            AnimationClip deathClip,
            AnimationClip faceRightClip,
            AnimationClip faceLeftClip)
        {
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

        private static void BuildPlayerPrefab(
            RuntimeAnimatorController controller,
            Sprite idleSprite)
        {
            var root = new GameObject("Player");
            root.AddComponent<HealthComponent>();
            root.AddComponent<PlayerMovement>();
            root.AddComponent<CriticalSystem>();
            root.AddComponent<PlayerProgression>();
            root.AddComponent<PlayerController>();
            CharacterSpriteAnimator animation =
                root.AddComponent<CharacterSpriteAnimator>();
            PlayerRoot playerRoot = root.AddComponent<PlayerRoot>();
            ConfigurePhysics(root, 0.34f);

            ConfigureVisual(
                root.transform,
                controller,
                idleSprite,
                1.65f,
                30,
                animation);
            SpriteRenderer attackRange = CreateSpriteVisual(
                root.transform,
                "PlayerAttackRange",
                new Color(0.55f, 0.58f, 0.62f, 0.2f),
                Vector2.one * PlayerController.AttackRange * 2f,
                5);
            TMP_Text levelLabel = CreateWorldLabel(
                root.transform,
                "PLAYER",
                new Vector3(0f, 0.72f, 0f),
                2.5f,
                35);
            playerRoot.ConfigureVisuals(attackRange, levelLabel);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
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
                archetype == EnemyArchetype.Boss ? 1.8f : 1.25f,
                20,
                animation);

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
            TMP_Text levelLabel = CreateWorldLabel(
                root.transform,
                $"{archetype} Lv.1",
                new Vector3(0f, archetype == EnemyArchetype.Boss ? 1.1f : 0.67f, 0f),
                2.3f,
                26);
            enemy.ConfigureVisuals(approachRange, facingMarker, levelLabel);

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
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            child.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            return label;
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
            EnsureFolder(AnimationPath + "/Common");
            EnsureFolder(AnimationPath + "/Goblin");
            EnsureFolder(AnimationPath + "/Skeleton");
            EnsureFolder(AnimatorPath);
            EnsureFolder(RootPath + "/Shared");
            EnsureFolder(RootPath + "/Prefabs/Enemies");
        }

        private static void RemoveLegacyPlayerAssets()
        {
            AssetDatabase.DeleteAsset(LegacyPlayerAnimationPath);
            AssetDatabase.DeleteAsset(LegacyPlayerControllerPath);
            AssetDatabase.DeleteAsset(LegacyPlayerPrefabPath);
            AssetDatabase.DeleteAsset(LegacyPlayerFaceRightPath);
            AssetDatabase.DeleteAsset(LegacyPlayerFaceLeftPath);
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                }

                current = next;
            }
        }
    }
}
