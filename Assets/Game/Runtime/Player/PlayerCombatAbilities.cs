using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class PlayerCombatAbilities : MonoBehaviour
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

        [SerializeField] private int piercingLevel;
        [SerializeField] private int severLevel;
        [SerializeField] private int hitHealLevel;
        [SerializeField] private int staticChargeLevel;
        [SerializeField] private int movingSlashLevel;
        [SerializeField] private int shieldBypassLevel;
        [SerializeField] private int flyingSwordCountLevel;
        [SerializeField] private int flyingSwordHitCountLevel;
        [SerializeField] private float severDamageMultiplier = 2f;
        [SerializeField] private int hitHealAmount = 2;
        [SerializeField] private float staticDamageMultiplier = 0.75f;
        [SerializeField] private float movingSlashDamageMultiplier = 1.5f;
        [SerializeField] private float shieldBypassChancePerLevel = 0.1f;
        [SerializeField] private SpriteRenderer severTrailVisual;
        [SerializeField] private MovingSlashProjectile movingSlashPrefab;

        private readonly HashSet<EnemyBase> directTargets = new();
        private PlayerRoot owner;
        private EnemyWorldService enemyWorld;
        private float piercingWindowEndsAt;
        private int piercingTargetsConsumed;
        private bool canOpenPiercingWindowForCommand;
        private float nextSeverAvailableTime;
        private FlyingSwordController flyingSwords;

        public int PiercingLevel => piercingLevel;
        public int StaticChargeLevel => staticChargeLevel;
        public int MovingSlashLevel => movingSlashLevel;
        public int ShieldBypassLevel => shieldBypassLevel;
        public int FlyingSwordCountLevel => flyingSwordCountLevel;
        public int FlyingSwordHitCountLevel => flyingSwordHitCountLevel;
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

        public void Configure(
            PlayerRoot configuredOwner,
            EnemyWorldService configuredEnemyWorld,
            SpawnPointRegistry configuredSpawnPoints)
        {
            owner = configuredOwner;
            enemyWorld = configuredEnemyWorld;
            piercingLevel = 0;
            severLevel = 0;
            hitHealLevel = 0;
            staticChargeLevel = 0;
            movingSlashLevel = 0;
            shieldBypassLevel = 0;
            flyingSwordCountLevel = 0;
            flyingSwordHitCountLevel = 0;
            severDamageMultiplier = 2f;
            hitHealAmount = 2;
            staticDamageMultiplier = 0.75f;
            movingSlashDamageMultiplier = 1.5f;
            shieldBypassChancePerLevel = 0.1f;
            piercingWindowEndsAt = 0f;
            piercingTargetsConsumed = 0;
            canOpenPiercingWindowForCommand = false;
            nextSeverAvailableTime = 0f;
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

        public bool ApplyCard(LevelUpCardDefinition card)
        {
            if (card == null)
            {
                return false;
            }

            switch (card.TargetStat)
            {
                case PlayerStatId.Piercing:
                    piercingLevel = AddLevel(
                        piercingLevel,
                        card.MaxStack,
                        card.Value);
                    break;
                case PlayerStatId.Sever:
                    severLevel = AddLevel(
                        severLevel,
                        card.MaxStack,
                        1f);
                    severDamageMultiplier = Mathf.Max(0f, card.Value);
                    break;
                case PlayerStatId.HitHeal:
                    hitHealLevel = AddLevel(
                        hitHealLevel,
                        card.MaxStack,
                        1f);
                    hitHealAmount = Mathf.Max(
                        1,
                        Mathf.RoundToInt(card.Value));
                    break;
                case PlayerStatId.StaticCharge:
                    staticChargeLevel = AddLevel(
                        staticChargeLevel,
                        card.MaxStack,
                        1f);
                    staticDamageMultiplier = Mathf.Max(0f, card.Value);
                    break;
                case PlayerStatId.MovingSlash:
                    movingSlashLevel = AddLevel(
                        movingSlashLevel,
                        card.MaxStack,
                        1f);
                    movingSlashDamageMultiplier =
                        Mathf.Max(0f, card.Value);
                    break;
                case PlayerStatId.ShieldBypass:
                    shieldBypassLevel = AddLevel(
                        shieldBypassLevel,
                        card.MaxStack,
                        1f);
                    shieldBypassChancePerLevel =
                        Mathf.Clamp01(card.Value);
                    break;
                case PlayerStatId.FlyingSwordCount:
                    flyingSwordCountLevel = AddLevel(
                        flyingSwordCountLevel,
                        card.MaxStack,
                        card.Value);
                    break;
                case PlayerStatId.FlyingSwordHitCount:
                    flyingSwordHitCountLevel = AddLevel(
                        flyingSwordHitCountLevel,
                        card.MaxStack,
                        card.Value);
                    break;
                default:
                    return false;
            }

            flyingSwords?.SetLevels(
                flyingSwordCountLevel,
                flyingSwordHitCountLevel);
            return true;
        }

        public PlayerAttackExecution ExecuteNormalAttack(
            EnemyBase primary,
            bool critical,
            bool allowPiercing)
        {
            if (primary == null ||
                !primary.IsAlive ||
                owner == null ||
                enemyWorld == null)
            {
                return default;
            }

            AttackSide primarySide = ResolveSide(primary);
            CombatResult primaryPreview = BuildNormalAttackResult(
                primary,
                primarySide,
                critical,
                true);
            bool piercingAllowed = allowPiercing &&
                CombatResolver.CanPiercePastTarget(
                    primary.Archetype,
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
            }

            if (CanTriggerSever(
                    HasSever,
                    piercingAllowed,
                    primaryDamaged) &&
                TryReserveSever())
            {
                StartCoroutine(SpawnSeverAfterDelay(
                    owner.transform.position));
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

        public void TrySpawnMovingSlash(Vector2 movementDirection)
        {
            if (movingSlashLevel <= 0 ||
                movementDirection.sqrMagnitude <= 0.0001f ||
                Random.value > CalculateMovingSlashChance(
                    movingSlashLevel))
            {
                return;
            }

            MovingSlashProjectile.Spawn(
                movingSlashPrefab,
                owner,
                enemyWorld,
                movementDirection,
                CalculateMovingSlashMaximumHits(movingSlashLevel),
                CalculateMovingSlashSize(movingSlashLevel),
                movingSlashDamageMultiplier);
        }

        public static int CalculateStaticAdjacentTargetCount(int level)
        {
            return Mathf.Max(0, level) * 2 + 1;
        }

        public static float CalculateMovingSlashChance(int level)
        {
            return level <= 0
                ? 0f
                : Mathf.Clamp01(0.1f + 0.03f * (level - 1));
        }

        public static int CalculateMovingSlashMaximumHits(int level)
        {
            return Mathf.Max(0, level);
        }

        public static float CalculateMovingSlashSize(int level)
        {
            return level <= 0
                ? 0f
                : 1f + 0.1f * (level - 1);
        }

        public static float CalculateShieldBypassChance(
            int level,
            float chancePerLevel = 0.1f)
        {
            return Mathf.Clamp01(
                Mathf.Max(0, level) *
                Mathf.Clamp01(chancePerLevel));
        }

        public static int CalculateHitHealAmount(
            int level,
            int amountPerLevel = 2)
        {
            return Mathf.Max(0, level) *
                Mathf.Max(0, amountPerLevel);
        }

        public static bool CanTriggerHitHeal(
            bool targetDefeated,
            int level,
            float randomValue,
            float chance = HitHealChance)
        {
            return targetDefeated &&
                level > 0 &&
                randomValue < Mathf.Clamp01(chance);
        }

        public static int CalculateRemainingPiercingTargets(
            int level,
            int consumed)
        {
            return Mathf.Max(
                0,
                Mathf.Max(0, level) -
                Mathf.Max(0, consumed));
        }

        public static bool ShouldRefreshPiercingWindow(
            float currentTime,
            float windowEndsAt)
        {
            return currentTime >= windowEndsAt;
        }

        public static bool CanConsumePiercingTarget(
            int level,
            int consumed,
            float currentTime,
            float windowEndsAt,
            bool canOpenWindow)
        {
            if (level <= 0)
            {
                return false;
            }

            if (ShouldRefreshPiercingWindow(
                    currentTime,
                    windowEndsAt))
            {
                return canOpenWindow;
            }

            return CalculateRemainingPiercingTargets(
                level,
                consumed) > 0;
        }

        public static bool IsSeverCooldownReady(
            float currentTime,
            float nextAvailableTime)
        {
            return currentTime >= nextAvailableTime;
        }

        public static bool CanTriggerSever(
            bool hasSever,
            bool piercingAllowed,
            bool primaryDamaged)
        {
            return hasSever &&
                piercingAllowed &&
                primaryDamaged;
        }

        public bool RollShieldBypass()
        {
            return shieldBypassLevel > 0 &&
                Random.value < ShieldBypassChance;
        }

        public void BeginPiercingCommand()
        {
            canOpenPiercingWindowForCommand =
                piercingLevel > 0 &&
                ShouldRefreshPiercingWindow(
                    Time.time,
                    piercingWindowEndsAt);
        }

        public bool TryConsumePiercingTarget()
        {
            if (!CanConsumePiercingTarget(
                    piercingLevel,
                    piercingTargetsConsumed,
                    Time.time,
                    piercingWindowEndsAt,
                    canOpenPiercingWindowForCommand))
            {
                return false;
            }

            if (ShouldRefreshPiercingWindow(
                    Time.time,
                    piercingWindowEndsAt))
            {
                piercingWindowEndsAt =
                    Time.time + PiercingWindowDuration;
                piercingTargetsConsumed = 0;
                canOpenPiercingWindowForCommand = false;
            }

            ConsumePiercingTargets(1);
            return true;
        }

        public void RefundPiercingTarget()
        {
            piercingTargetsConsumed =
                Mathf.Max(0, piercingTargetsConsumed - 1);
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
                target.Archetype == EnemyArchetype.Shield &&
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
            Vector2 piercingStartPosition)
        {
            yield return new WaitForSeconds(SeverDelay);
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
