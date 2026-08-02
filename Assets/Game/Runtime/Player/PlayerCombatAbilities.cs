using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed partial class PlayerCombatAbilities : MonoBehaviour
    {
        public const float HitHealChance = 0.05f;
        public const float StaticSearchRadius = 3.2f;
        public const float PiercingReach = 4.5f;
        public const float PiercingHalfWidth = 0.42f;
        public const float PiercingWindowDuration = 0.4f;
        public const float SeverDelay = 0.3f;
        public const float SeverReuseCooldown = 0.1f;
        public const float SeverTrailFadeDuration = 0.1f;
        public const float SeverHalfWidth = 0.17f;
        public const int MovingSlashMaximumLevel = 5;
        public const float MovingSlashBaseDamageMultiplier = 1.8f;
        public const float MovingSlashDamageGrowthPerLevel = 0.35f;
        public const float MovingSlashSizeGrowthPerLevel = 0.15f;
        public const float MovingSlashBaseTravelDistance = 6f;
        public const float MovingSlashTravelGrowthPerLevel = 1.5f;
        public const int FilthThrowMaximumLevel = 5;
        public const float FilthThrowBaseInterval = 6f;
        public const float FilthThrowIntervalReductionPerLevel = 0.5f;
        public const float FilthThrowInitialDelay = 0.25f;
        public const float FilthThrowBaseDamageMultiplier = 0.35f;
        public const float FilthThrowDamageGrowthPerLevel = 0.1f;
        public const float FilthThrowBaseRadius = 1.2f;
        public const float FilthThrowRadiusGrowthPerLevel = 0.12f;
        public const int PiercingMaximumLevel = 5;
        public const int StaticChargeMaximumLevel = 5;

        [SerializeField] private int piercingLevel;
        [SerializeField] private int severLevel;
        [SerializeField] private int hitHealLevel;
        [SerializeField] private int staticChargeLevel;
        [SerializeField] private int movingSlashLevel;
        [SerializeField] private int shieldBypassLevel;
        [SerializeField] private int flyingSwordCountLevel;
        [SerializeField] private int flyingSwordHitCountLevel;
        [SerializeField] private int filthThrowLevel;
        [SerializeField] private float severDamageMultiplier = 2f;
        [SerializeField] private int hitHealAmount = 2;
        [SerializeField] private float staticDamageMultiplier = 0.75f;
        [SerializeField] private float movingSlashDamageMultiplier =
            MovingSlashBaseDamageMultiplier;
        [SerializeField] private float filthThrowBaseDamageMultiplier =
            FilthThrowBaseDamageMultiplier;
        [SerializeField] private float shieldBypassChancePerLevel = 0.1f;
        [SerializeField] private bool hasFlyingSwordPiercingFusion;
        [SerializeField] private bool hasFlyingSwordStaticFusion;
        [SerializeField] private bool hasStaticFilthFusion;
        [SerializeField] private int flyingSwordPiercingCountSnapshot;
        [SerializeField] private int flyingSwordPiercingHitsSnapshot;
        [SerializeField] private int flyingSwordStaticCountSnapshot;
        [SerializeField] private int flyingSwordStaticHitsSnapshot;
        [SerializeField] private int flyingSwordStaticChargeSnapshot;
        [SerializeField] private float flyingSwordStaticDamageSnapshot;
        [SerializeField] private int staticFilthLevelSnapshot;
        [SerializeField] private float staticFilthDamageSnapshot;
        [SerializeField] private int staticFilthChargeSnapshot;
        [SerializeField] private float staticFilthChargeDamageSnapshot;
        [SerializeField] private SpriteRenderer severTrailVisual;
        [SerializeField] private MovingSlashProjectile movingSlashPrefab;
        [SerializeField] private FilthProjectile filthProjectilePrefab;

        private readonly HashSet<EnemyBase> directTargets = new();
        private readonly HashSet<EnemyBase> staticBurstExclusions = new();
        private PlayerRoot owner;
        private EnemyWorldService enemyWorld;
        private SpawnPointRegistry spawnPoints;
        private float piercingWindowEndsAt;
        private int piercingTargetsConsumed;
        private float nextSeverAvailableTime;
        private float nextFilthThrowAt = float.PositiveInfinity;
        private float nextStaticFilthThrowAt = float.PositiveInfinity;
        private FlyingSwordController flyingSwords;
        private FlyingSwordController flyingSwordPiercingFusion;
        private FlyingSwordController flyingSwordStaticFusion;
        private Camera worldCamera;

        public int PiercingLevel => piercingLevel;
        public int StaticChargeLevel => staticChargeLevel;
        public int MovingSlashLevel => movingSlashLevel;
        public int ShieldBypassLevel => shieldBypassLevel;
        public int FlyingSwordCountLevel => flyingSwordCountLevel;
        public int FlyingSwordHitCountLevel => flyingSwordHitCountLevel;
        public int FilthThrowLevel => filthThrowLevel;
        public bool HasFlyingSwordPiercingFusion =>
            hasFlyingSwordPiercingFusion;
        public bool HasFlyingSwordStaticFusion =>
            hasFlyingSwordStaticFusion;
        public bool HasStaticFilthFusion => hasStaticFilthFusion;
        public int FlyingSwordPiercingCountSnapshot =>
            flyingSwordPiercingCountSnapshot;
        public int FlyingSwordPiercingHitsSnapshot =>
            flyingSwordPiercingHitsSnapshot;
        public int FlyingSwordStaticCountSnapshot =>
            flyingSwordStaticCountSnapshot;
        public int FlyingSwordStaticHitsSnapshot =>
            flyingSwordStaticHitsSnapshot;
        public int FlyingSwordStaticChargeSnapshot =>
            flyingSwordStaticChargeSnapshot;
        public float FlyingSwordStaticDamageSnapshot =>
            flyingSwordStaticDamageSnapshot;
        public int StaticFilthLevelSnapshot => staticFilthLevelSnapshot;
        public int StaticFilthChargeSnapshot => staticFilthChargeSnapshot;
        public float StaticFilthDamageSnapshot =>
            staticFilthDamageSnapshot;
        public float StaticFilthChargeDamageSnapshot =>
            staticFilthChargeDamageSnapshot;
        public float ShieldBypassChance =>
            CalculateShieldBypassChance(
                shieldBypassLevel,
                shieldBypassChancePerLevel);
        public bool HasSever => severLevel > 0;

        public void ConfigureSeverVisual(
            SpriteRenderer configuredSeverTrailVisual)
        {
            severTrailVisual = configuredSeverTrailVisual;
            if (severTrailVisual != null)
            {
                severTrailVisual.gameObject.SetActive(false);
            }
        }

        public void ConfigureMovingSlashPrefab(
            MovingSlashProjectile configuredPrefab)
        {
            movingSlashPrefab = configuredPrefab;
        }

        public void ConfigureFilthProjectilePrefab(
            FilthProjectile configuredPrefab)
        {
            filthProjectilePrefab = configuredPrefab;
        }

        public void Configure(
            PlayerRoot configuredOwner,
            EnemyWorldService configuredEnemyWorld,
            SpawnPointRegistry configuredSpawnPoints,
            Camera configuredWorldCamera)
        {
            owner = configuredOwner;
            enemyWorld = configuredEnemyWorld;
            spawnPoints = configuredSpawnPoints;
            worldCamera = configuredWorldCamera;
            piercingLevel = 0;
            severLevel = 0;
            hitHealLevel = 0;
            staticChargeLevel = 0;
            movingSlashLevel = 0;
            shieldBypassLevel = 0;
            flyingSwordCountLevel = 0;
            flyingSwordHitCountLevel = 0;
            filthThrowLevel = 0;
            severDamageMultiplier = 2f;
            hitHealAmount = 2;
            staticDamageMultiplier = 0.75f;
            movingSlashDamageMultiplier =
                MovingSlashBaseDamageMultiplier;
            filthThrowBaseDamageMultiplier =
                FilthThrowBaseDamageMultiplier;
            shieldBypassChancePerLevel = 0.1f;
            hasFlyingSwordPiercingFusion = false;
            hasFlyingSwordStaticFusion = false;
            hasStaticFilthFusion = false;
            flyingSwordPiercingCountSnapshot = 0;
            flyingSwordPiercingHitsSnapshot = 0;
            flyingSwordStaticCountSnapshot = 0;
            flyingSwordStaticHitsSnapshot = 0;
            flyingSwordStaticChargeSnapshot = 0;
            flyingSwordStaticDamageSnapshot = 0f;
            staticFilthLevelSnapshot = 0;
            staticFilthDamageSnapshot = 0f;
            staticFilthChargeSnapshot = 0;
            staticFilthChargeDamageSnapshot = 0f;
            piercingWindowEndsAt = 0f;
            piercingTargetsConsumed = 0;
            nextSeverAvailableTime = 0f;
            nextFilthThrowAt = float.PositiveInfinity;
            nextStaticFilthThrowAt = float.PositiveInfinity;
            DestroyFusionController(ref flyingSwordPiercingFusion);
            DestroyFusionController(ref flyingSwordStaticFusion);
            if (severTrailVisual == null)
            {
                severTrailVisual =
                    transform.Find("cutting")
                        ?.GetComponent<SpriteRenderer>();
            }

            if (severTrailVisual != null)
            {
                severTrailVisual.gameObject.SetActive(false);
            }

            flyingSwords = GetComponent<FlyingSwordController>();
            if (flyingSwords == null)
            {
                flyingSwords =
                    gameObject.AddComponent<FlyingSwordController>();
            }

            flyingSwords.Configure(
                owner,
                enemyWorld,
                configuredSpawnPoints);
            flyingSwords.SetLevels(
                flyingSwordCountLevel,
                flyingSwordHitCountLevel);
        }

        public PlayerAttackExecution ExecuteNormalAttack(
            EnemyBase primary,
            bool critical,
            bool allowAttackPiercing,
            bool movementPiercingRequested)
        {
            if (primary == null ||
                !primary.IsAlive ||
                owner == null ||
                enemyWorld == null)
            {
                return default;
            }

            TrySpawnMovingSlash(
                primary.transform.position -
                owner.transform.position);
            AttackSide primarySide = ResolveSide(primary);
            CombatResult primaryPreview = BuildNormalAttackResult(
                primary,
                primarySide,
                critical,
                true);
            bool piercingAllowed = allowAttackPiercing &&
                CombatResolver.CanPiercePastTarget(
                    primary.Definition,
                    primarySide,
                    primaryPreview.Damage >=
                        primary.CurrentHealth);
            int availablePiercingTargets = piercingAllowed
                ? GetRemainingPiercingTargetCount()
                : 0;
            List<EnemyBase> targets =
                enemyWorld.CollectPiercingTargets(
                owner.transform.position,
                primary,
                availablePiercingTargets,
                PiercingReach,
                PiercingHalfWidth);
            directTargets.Clear();
            foreach (EnemyBase target in targets)
            {
                directTargets.Add(target);
            }

            CombatResult primaryResult = default;
            bool primaryDamaged = false;
            bool anyDamage = false;
            int piercedTargetsThisAttack = 0;
            foreach (EnemyBase target in targets)
            {
                bool isPrimary = target == primary;
                AttackSide side = isPrimary
                    ? primarySide
                    : ResolveSide(target);
                CombatResult combinedResult =
                    BuildNormalAttackResult(
                        target,
                        side,
                        critical,
                        isPrimary);
                bool damaged = target.ReceivePlayerAttack(
                    combinedResult,
                    owner,
                    side,
                    critical);
                if (damaged)
                {
                    HandleEnemyDefeated(target);
                    anyDamage = true;
                    if (!isPrimary)
                    {
                        piercedTargetsThisAttack++;
                    }
                }

                if (isPrimary)
                {
                    primaryResult = combinedResult;
                    primaryDamaged = damaged;
                }
            }

            ConsumePiercingTargets(piercedTargetsThisAttack);
            if (primaryDamaged)
            {
                flyingSwords?.HandlePrimaryHit(primary);
                flyingSwordPiercingFusion?.HandlePrimaryHit(primary);
                flyingSwordStaticFusion?.HandlePrimaryHit(primary);
            }

            if (CanTriggerSever(
                    HasSever,
                    movementPiercingRequested &&
                        piercingAllowed,
                    primaryDamaged) &&
                TryReserveSever())
            {
                StartCoroutine(SpawnSeverAfterDelay(
                    owner.transform.position,
                    SeverDelay));
            }

            if (staticChargeLevel > 0)
            {
                int adjacentCount =
                    CalculateStaticAdjacentTargetCount(
                        staticChargeLevel);
                List<EnemyBase> adjacent =
                    enemyWorld.CollectNearestEnemies(
                        primary.transform.position,
                        StaticSearchRadius,
                        adjacentCount,
                        directTargets);
                foreach (EnemyBase enemy in adjacent)
                {
                    Vector2 arcStart = primary.transform.position;
                    Vector2 arcEnd = enemy.transform.position;
                    bool staticDamageApplied = ApplySkillHit(
                        enemy,
                        staticDamageMultiplier);
                    if (staticDamageApplied)
                    {
                        SlashTrailEffect.ShowStaticArc(
                            arcStart,
                            arcEnd);
                        anyDamage = true;
                    }
                }
            }

            return new PlayerAttackExecution(
                primaryResult,
                primaryDamaged,
                anyDamage,
                critical,
                primarySide,
                piercingAllowed && primaryDamaged);
        }

        public bool ApplySkillHit(
            EnemyBase enemy,
            float damageMultiplier)
        {
            if (enemy == null || !enemy.IsAlive || owner == null)
            {
                return false;
            }

            AttackSide side = ResolveSide(enemy);
            float damage =
                CalculateSkillBaseDamage(side) *
                Mathf.Max(0f, damageMultiplier);
            var result = new CombatResult(
                damage,
                enemy.MaxHealth,
                PlayerAttackReaction.None);
            bool damaged = enemy.ReceivePlayerAttack(
                result,
                owner,
                side,
                false);
            if (damaged)
            {
                HandleEnemyDefeated(enemy);
            }

            return damaged;
        }

        public bool ApplySkillHitWithStaticBurst(
            EnemyBase enemy,
            float baseDamageMultiplier,
            int burstLevel,
            float burstDamageMultiplier)
        {
            if (burstLevel <= 0 || burstDamageMultiplier <= 0f)
            {
                return ApplySkillHit(enemy, baseDamageMultiplier);
            }

            if (enemy == null || !enemy.IsAlive)
            {
                return false;
            }

            Vector2 burstCenter = enemy.transform.position;
            bool damaged = ApplySkillHit(
                enemy,
                Mathf.Max(0f, baseDamageMultiplier) +
                Mathf.Max(0f, burstDamageMultiplier));
            if (!damaged || enemyWorld == null)
            {
                return damaged;
            }

            staticBurstExclusions.Clear();
            staticBurstExclusions.Add(enemy);
            List<EnemyBase> adjacent =
                enemyWorld.CollectNearestEnemies(
                    burstCenter,
                    StaticSearchRadius,
                    CalculateStaticAdjacentTargetCount(burstLevel),
                    staticBurstExclusions);
            foreach (EnemyBase adjacentEnemy in adjacent)
            {
                Vector2 arcEnd = adjacentEnemy.transform.position;
                if (ApplySkillHit(
                        adjacentEnemy,
                        burstDamageMultiplier))
                {
                    SlashTrailEffect.ShowStaticArc(
                        burstCenter,
                        arcEnd);
                }
            }

            staticBurstExclusions.Clear();
            return true;
        }

        private static int AddLevel(
            int current,
            int maximum,
            float amount)
        {
            int increase = Mathf.Max(1, Mathf.RoundToInt(amount));
            return Mathf.Min(Mathf.Max(1, maximum), current + increase);
        }

        private AttackSide ResolveSide(EnemyBase enemy)
        {
            return CombatResolver.GetAttackSide(
                enemy.Facing.Direction,
                enemy.transform.position,
                owner.transform.position);
        }

        private float CalculateSkillBaseDamage(AttackSide side)
        {
            float sideMultiplier = side == AttackSide.Rear
                ? owner.RearAttackMultiplier
                : 1f;
            return owner.AttackPower * sideMultiplier;
        }

        private CombatResult BuildNormalAttackResult(
            EnemyBase target,
            AttackSide side,
            bool critical,
            bool isPrimary)
        {
            CombatResult baseResult = CombatResolver.Resolve(
                target.Definition,
                target.MaxHealth,
                owner.AttackPower,
                owner.RearAttackMultiplier,
                side,
                critical);
            float bonusDamage = isPrimary && staticChargeLevel > 0
                ? CalculateSkillBaseDamage(side) *
                    staticDamageMultiplier
                : 0f;
            float damage = baseResult.Damage + bonusDamage;
            PlayerAttackReaction reaction =
                isPrimary &&
                target.Definition.BlocksFrontAttacks &&
                side == AttackSide.Front &&
                damage >= target.CurrentHealth
                    ? PlayerAttackReaction.None
                    : baseResult.PlayerReaction;
            return new CombatResult(
                damage,
                baseResult.TargetMaxHealth,
                isPrimary
                    ? reaction
                    : PlayerAttackReaction.None);
        }

        private void HandleEnemyDefeated(EnemyBase enemy)
        {
            if (enemy == null ||
                enemy.IsAlive ||
                hitHealLevel <= 0)
            {
                return;
            }

            if (CanTriggerHitHeal(
                    true,
                    hitHealLevel,
                    Random.value))
            {
                owner.Health.Heal(CalculateHitHealAmount(
                    hitHealLevel,
                    hitHealAmount));
            }
        }

        private int GetRemainingPiercingTargetCount()
        {
            if (piercingLevel <= 0)
            {
                return 0;
            }

            if (ShouldRefreshPiercingWindow(
                    Time.time,
                    piercingWindowEndsAt))
            {
                piercingWindowEndsAt =
                    Time.time + PiercingWindowDuration;
                piercingTargetsConsumed = 0;
            }

            return CalculateRemainingPiercingTargets(
                piercingLevel,
                piercingTargetsConsumed);
        }

        private void ConsumePiercingTargets(int count)
        {
            piercingTargetsConsumed = Mathf.Min(
                piercingLevel,
                piercingTargetsConsumed +
                Mathf.Max(0, count));
        }

        private bool TryReserveSever()
        {
            if (!IsSeverCooldownReady(
                    Time.time,
                    nextSeverAvailableTime))
            {
                return false;
            }

            nextSeverAvailableTime =
                Time.time + SeverReuseCooldown;
            return true;
        }

        private IEnumerator SpawnSeverAfterDelay(
            Vector2 piercingStartPosition,
            float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (owner == null ||
                enemyWorld == null ||
                !owner.IsAlive)
            {
                yield break;
            }

            Vector2 currentPlayerPosition =
                owner.transform.position;
            SlashTrailEffect.Show(
                severTrailVisual,
                piercingStartPosition,
                currentPlayerPosition,
                SeverTrailFadeDuration);

            List<EnemyBase> severTargets =
                enemyWorld.CollectEnemiesAlongSegment(
                    piercingStartPosition,
                    currentPlayerPosition,
                    SeverHalfWidth);
            foreach (EnemyBase enemy in severTargets)
            {
                ApplySkillHit(enemy, severDamageMultiplier);
            }
        }
    }
}
