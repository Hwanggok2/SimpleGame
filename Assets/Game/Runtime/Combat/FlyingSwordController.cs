using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [DisallowMultipleComponent]
    public sealed partial class FlyingSwordController : MonoBehaviour
    {
        public const int MaximumSwordCount = 3;
        public const int BaseHitCount = 2;
        public const int MaximumHitUpgradeLevel = 3;
        public const float LaunchInterval = 0.1f;
        public const float RechargeDuration = 0.3f;
        public const float PostTargetTravelDuration = 0.1f;
        public const float DamageMultiplier = 1f;

        private const float HitHalfWidth = 0.13f;
        private const float MinimumPathDistance = 0.0001f;
        private const int VisualSortingOrder = 100;

        [SerializeField]
        private SpriteRenderer[] readySwordVisuals =
            new SpriteRenderer[MaximumSwordCount];
        [SerializeField]
        private SpriteRenderer attackVisualTemplate;

        private readonly List<SwordSlot> slots =
            new(MaximumSwordCount);
        private readonly List<HitCandidate> hitCandidates = new();

        private PlayerRoot owner;
        private EnemyWorldService enemyWorld;
        private SpawnPointRegistry spawnPoints;
        private Transform visualRoot;
        private int swordCountLevel;
        private int hitCountLevel;
        private float nextLaunchAt;
        private bool missingVisualsLogged;
        private bool showReadyIndicators = true;
        private bool piercesEntirePath;
        private int staticChargeLevel;
        private float staticDamageMultiplier;

        public int SwordCountLevel => swordCountLevel;
        public int HitCountLevel => hitCountLevel;
        public bool PiercesEntirePath => piercesEntirePath;
        public int StaticChargeLevel => staticChargeLevel;

        public void ConfigureVisuals(
            SpriteRenderer[] configuredReadySwordVisuals,
            SpriteRenderer configuredAttackVisualTemplate)
        {
            readySwordVisuals = configuredReadySwordVisuals;
            attackVisualTemplate = configuredAttackVisualTemplate;
        }

        public void Configure(
            PlayerRoot configuredOwner,
            EnemyWorldService configuredEnemyWorld,
            SpawnPointRegistry configuredSpawnPoints,
            bool configuredShowReadyIndicators = true)
        {
            owner = configuredOwner;
            enemyWorld = configuredEnemyWorld;
            spawnPoints = configuredSpawnPoints;
            showReadyIndicators = configuredShowReadyIndicators;
            piercesEntirePath = false;
            staticChargeLevel = 0;
            staticDamageMultiplier = 0f;
            swordCountLevel = 0;
            hitCountLevel = 0;
            nextLaunchAt = 0f;
            missingVisualsLogged = false;

            ResolvePrefabVisuals();
            foreach (SwordSlot slot in slots)
            {
                slot.State = SwordState.Locked;
                slot.PrimaryTarget = null;
                slot.HitEnemyGenerations.Clear();
                HideSlotVisuals(slot);
            }
        }

        public void ConfigureFusionEffects(
            bool configuredPiercesEntirePath,
            int configuredStaticChargeLevel,
            float configuredStaticDamageMultiplier)
        {
            piercesEntirePath = configuredPiercesEntirePath;
            staticChargeLevel = Mathf.Max(
                0,
                configuredStaticChargeLevel);
            staticDamageMultiplier = Mathf.Max(
                0f,
                configuredStaticDamageMultiplier);
        }

        public void SetLevels(
            int configuredSwordCountLevel,
            int configuredHitCountLevel)
        {
            swordCountLevel = Mathf.Clamp(
                configuredSwordCountLevel,
                0,
                MaximumSwordCount);
            hitCountLevel = Mathf.Clamp(
                configuredHitCountLevel,
                0,
                MaximumHitUpgradeLevel);

            EnsureSlotCount(swordCountLevel);
            for (int index = 0; index < slots.Count; index++)
            {
                SwordSlot slot = slots[index];
                if (index < swordCountLevel)
                {
                    if (slot.State == SwordState.Locked)
                    {
                        slot.State = SwordState.Ready;
                    }

                    continue;
                }

                slot.State = SwordState.Locked;
                slot.PrimaryTarget = null;
                slot.HitEnemyGenerations.Clear();
                HideSlotVisuals(slot);
            }

            RefreshReadyIndicators();
        }

        private enum SwordState
        {
            Locked,
            Ready,
            Approaching,
            Passing,
            Cooling
        }

        private sealed class SwordSlot
        {
            public SwordSlot(
                GameObject root,
                GameObject indicatorVisual,
                GameObject attackVisual,
                SpriteRenderer attackRenderer,
                float attackWidthScale,
                float attackDepthScale,
                float attackDepthOffset,
                float attackSpriteHeight)
            {
                Transform = root.transform;
                IndicatorVisual = indicatorVisual;
                AttackVisual = attackVisual;
                AttackRenderer = attackRenderer;
                AttackBaseColor = attackRenderer.color;
                AttackWidthScale = attackWidthScale;
                AttackDepthScale = attackDepthScale;
                AttackDepthOffset = attackDepthOffset;
                AttackSpriteHeight = attackSpriteHeight;
                State = SwordState.Locked;
            }

            public Transform Transform { get; }
            public GameObject IndicatorVisual { get; }
            public GameObject AttackVisual { get; }
            public SpriteRenderer AttackRenderer { get; }
            public Color AttackBaseColor { get; }
            public float AttackWidthScale { get; }
            public float AttackDepthScale { get; }
            public float AttackDepthOffset { get; }
            public float AttackSpriteHeight { get; }
            public Dictionary<EnemyBase, uint> HitEnemyGenerations { get; } =
                new();
            public SwordState State { get; set; }
            public EnemyBase PrimaryTarget { get; set; }
            public Vector2 TargetPosition { get; set; }
            public Vector2 AttackOrigin { get; set; }
            public Vector2 Direction { get; set; }
            public float Speed { get; set; }
            public float RemainingPassDuration { get; set; }
            public float ReadyAt { get; set; }
            public int RemainingHits { get; set; }
            public bool PiercesEntirePath { get; set; }
            public int StaticChargeLevel { get; set; }
            public float StaticDamageMultiplier { get; set; }
        }

        private readonly struct HitCandidate
        {
            public HitCandidate(
                EnemyBase enemy,
                float progress)
            {
                Enemy = enemy;
                Progress = progress;
            }

            public EnemyBase Enemy { get; }
            public float Progress { get; }
        }
    }
}
