using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [DisallowMultipleComponent]
    public sealed class FlyingSwordController : MonoBehaviour
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

        public int SwordCountLevel => swordCountLevel;
        public int HitCountLevel => hitCountLevel;

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
            SpawnPointRegistry configuredSpawnPoints)
        {
            owner = configuredOwner;
            enemyWorld = configuredEnemyWorld;
            spawnPoints = configuredSpawnPoints;
            swordCountLevel = 0;
            hitCountLevel = 0;
            nextLaunchAt = 0f;
            missingVisualsLogged = false;

            ResolvePrefabVisuals();
            foreach (SwordSlot slot in slots)
            {
                slot.State = SwordState.Locked;
                slot.PrimaryTarget = null;
                slot.HitEnemies.Clear();
                HideSlotVisuals(slot);
            }
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
                slot.HitEnemies.Clear();
                HideSlotVisuals(slot);
            }

            RefreshReadyIndicators();
        }

        public void HandlePrimaryHit(EnemyBase primary)
        {
            if (primary == null ||
                owner == null ||
                enemyWorld == null ||
                swordCountLevel <= 0 ||
                !owner.IsAlive)
            {
                return;
            }

            foreach (SwordSlot slot in slots)
            {
                if (slot.State == SwordState.Approaching &&
                    (slot.PrimaryTarget == null ||
                     !slot.PrimaryTarget.IsAlive) &&
                    BeginFlight(slot, primary))
                {
                    nextLaunchAt = Time.time + LaunchInterval;
                    return;
                }
            }

            if (!IsLaunchReady(Time.time, nextLaunchAt))
            {
                return;
            }

            foreach (SwordSlot slot in slots)
            {
                RefreshSlotAvailability(slot, Time.time);
                if (slot.State == SwordState.Ready &&
                    BeginFlight(slot, primary))
                {
                    nextLaunchAt = Time.time + LaunchInterval;
                    return;
                }
            }
        }

        public static int CalculateMaximumHits(int level)
        {
            return BaseHitCount +
                Mathf.Clamp(level, 0, MaximumHitUpgradeLevel);
        }

        public static bool IsLaunchReady(
            float currentTime,
            float nextAvailableTime)
        {
            return currentTime >= nextAvailableTime;
        }

        public static bool IsSlotReady(
            float currentTime,
            float readyAt)
        {
            return currentTime >= readyAt;
        }

        public static float CalculateFadeAlpha(
            float remainingDuration)
        {
            return Mathf.Clamp01(
                remainingDuration /
                PostTargetTravelDuration);
        }

        private void Update()
        {
            if (owner == null || enemyWorld == null)
            {
                HideAllVisuals();
                return;
            }

            if (!owner.IsAlive)
            {
                CancelFlights();
                HideAllVisuals();
                return;
            }

            float currentTime = Time.time;
            foreach (SwordSlot slot in slots)
            {
                RefreshSlotAvailability(slot, currentTime);
                switch (slot.State)
                {
                    case SwordState.Approaching:
                        TickApproaching(slot);
                        break;
                    case SwordState.Passing:
                        TickPassing(slot);
                        break;
                }
            }
        }

        private void LateUpdate()
        {
            if (owner != null &&
                enemyWorld != null &&
                owner.IsAlive)
            {
                RefreshReadyIndicators();
            }
        }

        private bool BeginFlight(
            SwordSlot slot,
            EnemyBase primary)
        {
            Vector2 targetPosition = primary.transform.position;
            if (!TryGetRandomSpawnPosition(
                    targetPosition,
                    out Vector2 start))
            {
                return false;
            }

            Vector2 path = targetPosition - start;
            float distance = path.magnitude;
            Vector2 direction = distance > Mathf.Epsilon
                ? path / distance
                : Vector2.right;
            float speed =
                PlayerMovement.CalculateMaximumTravelSpeed(distance);

            slot.PrimaryTarget = primary;
            slot.TargetPosition = targetPosition;
            slot.AttackOrigin = start;
            slot.Direction = direction;
            slot.Speed = speed;
            slot.RemainingPassDuration =
                PostTargetTravelDuration;
            slot.RemainingHits =
                CalculateMaximumHits(hitCountLevel);
            slot.ReadyAt = Time.time + RechargeDuration;
            slot.HitEnemies.Clear();
            slot.State = SwordState.Approaching;

            slot.Transform.position = new Vector3(
                start.x,
                start.y,
                owner.transform.position.z);
            FaceDirection(slot.Transform, direction);
            slot.IndicatorVisual.SetActive(false);
            RestoreAttackColor(slot);
            slot.AttackVisual.SetActive(true);
            RefreshAttackVisual(slot);
            return true;
        }

        private void TickApproaching(SwordSlot slot)
        {
            Vector2 previous = slot.Transform.position;
            float distanceToTarget = Vector2.Distance(
                previous,
                slot.TargetPosition);
            float frameDistance = slot.Speed * Time.deltaTime;
            if (frameDistance + 0.0001f < distanceToTarget)
            {
                SetWorldPosition(
                    slot,
                    previous + slot.Direction * frameDistance);
                RefreshAttackVisual(slot);
                return;
            }

            SetWorldPosition(slot, slot.TargetPosition);
            RefreshAttackVisual(slot);
            HitPrimary(slot);
            slot.State = SwordState.Passing;

            float overflow = Mathf.Max(
                0f,
                frameDistance - distanceToTarget);
            float overflowDuration = slot.Speed > 0f
                ? overflow / slot.Speed
                : Time.deltaTime;
            AdvancePassing(slot, overflowDuration);
        }

        private void TickPassing(SwordSlot slot)
        {
            AdvancePassing(
                slot,
                Time.deltaTime);
        }

        private void AdvancePassing(
            SwordSlot slot,
            float requestedDuration)
        {
            float duration = Mathf.Min(
                Mathf.Max(0f, requestedDuration),
                slot.RemainingPassDuration);
            float distance = slot.Speed * duration;
            Vector2 previous = slot.Transform.position;
            Vector2 next =
                previous + slot.Direction * distance;
            SetWorldPosition(slot, next);
            HitSecondaryEnemies(slot, previous, next);
            slot.RemainingPassDuration =
                Mathf.Max(
                    0f,
                    slot.RemainingPassDuration - duration);
            RefreshAttackVisual(
                slot,
                CalculateFadeAlpha(
                    slot.RemainingPassDuration));

            if (slot.RemainingPassDuration <= 0.0001f)
            {
                FinishFlight(slot);
            }
        }

        private void HitPrimary(SwordSlot slot)
        {
            EnemyBase primary = slot.PrimaryTarget;
            if (primary == null ||
                !primary.IsAlive ||
                slot.RemainingHits <= 0)
            {
                return;
            }

            slot.HitEnemies.Add(primary);
            if (ApplySwordHit(primary))
            {
                slot.RemainingHits--;
            }
        }

        private void HitSecondaryEnemies(
            SwordSlot slot,
            Vector2 start,
            Vector2 end)
        {
            hitCandidates.Clear();
            if (slot.RemainingHits <= 0 ||
                Vector2.SqrMagnitude(end - start) <= 0.0000001f)
            {
                return;
            }

            IReadOnlyList<EnemyBase> enemies = enemyWorld.Enemies;
            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyBase enemy = enemies[index];
                if (enemy == null ||
                    !enemy.IsAlive ||
                    slot.HitEnemies.Contains(enemy))
                {
                    continue;
                }

                Vector2 enemyPosition = enemy.transform.position;
                float progress = Vector2.Dot(
                    enemyPosition - start,
                    slot.Direction);
                if (progress <= 0f)
                {
                    continue;
                }

                float allowedDistance =
                    HitHalfWidth +
                    EnemyWorldService.GetColliderRadius(enemy);
                if (CombatGeometry.DistancePointToSegment(
                        enemyPosition,
                        start,
                        end) > allowedDistance)
                {
                    continue;
                }

                hitCandidates.Add(new HitCandidate(
                    enemy,
                    progress));
            }

            hitCandidates.Sort((left, right) =>
                left.Progress.CompareTo(right.Progress));
            foreach (HitCandidate candidate in hitCandidates)
            {
                if (slot.RemainingHits <= 0)
                {
                    break;
                }

                EnemyBase enemy = candidate.Enemy;
                if (enemy == null ||
                    !enemy.IsAlive ||
                    slot.HitEnemies.Contains(enemy))
                {
                    continue;
                }

                slot.HitEnemies.Add(enemy);
                if (ApplySwordHit(enemy))
                {
                    slot.RemainingHits--;
                }
            }

            hitCandidates.Clear();
        }

        private bool ApplySwordHit(EnemyBase enemy)
        {
            bool damageApplied = owner.ApplySkillHit(
                enemy,
                DamageMultiplier);
            if (damageApplied)
            {
                enemy.Session?.PlayCombatFeedback(
                    true,
                    !enemy.IsAlive,
                    false,
                    PlayerAttackReaction.None);
            }

            return damageApplied;
        }

        private void FinishFlight(SwordSlot slot)
        {
            slot.State = SwordState.Cooling;
            slot.PrimaryTarget = null;
            slot.HitEnemies.Clear();
            slot.AttackVisual.SetActive(false);
            RestoreAttackColor(slot);
        }

        private void RefreshSlotAvailability(
            SwordSlot slot,
            float currentTime)
        {
            if (slot.State == SwordState.Cooling &&
                IsSlotReady(currentTime, slot.ReadyAt))
            {
                slot.State = SwordState.Ready;
            }
        }

        private bool TryGetRandomSpawnPosition(
            Vector2 targetPosition,
            out Vector2 position)
        {
            IReadOnlyList<Transform> candidates =
                spawnPoints != null
                    ? spawnPoints.SpawnPoints
                    : null;
            int count = candidates?.Count ?? 0;
            if (count <= 0)
            {
                position = default;
                return false;
            }

            int startIndex = Random.Range(0, count);
            Transform fallback = null;
            for (int offset = 0; offset < count; offset++)
            {
                Transform candidate =
                    candidates[(startIndex + offset) % count];
                if (candidate == null)
                {
                    continue;
                }

                fallback ??= candidate;
                Vector2 candidatePosition = candidate.position;
                if (Vector2.Distance(
                        candidatePosition,
                        targetPosition) <= MinimumPathDistance)
                {
                    continue;
                }

                position = candidatePosition;
                return true;
            }

            if (fallback != null)
            {
                position = fallback.position;
                return true;
            }

            position = default;
            return false;
        }

        private void EnsureSlotCount(int count)
        {
            if (slots.Count >= count)
            {
                return;
            }

            EnsureVisualRoot();
            while (slots.Count < count)
            {
                SwordSlot slot = CreateSlot(slots.Count);
                if (slot == null)
                {
                    return;
                }

                slots.Add(slot);
            }
        }

        private SwordSlot CreateSlot(int index)
        {
            if (readySwordVisuals == null ||
                index < 0 ||
                index >= readySwordVisuals.Length ||
                readySwordVisuals[index] == null ||
                attackVisualTemplate == null)
            {
                if (!missingVisualsLogged)
                {
                    Debug.LogError(
                        "Player prefab requires Flying_Sword1..3 " +
                        "and Flying_Sword_Attack visuals.",
                        this);
                    missingVisualsLogged = true;
                }

                return null;
            }

            var root = new GameObject(
                $"FlyingSwordAttackSlot_{index + 1}");
            root.transform.SetParent(visualRoot, false);

            GameObject attackVisual = Instantiate(
                attackVisualTemplate.gameObject,
                root.transform);
            attackVisual.name =
                $"Flying_Sword_Attack_{index + 1}";
            Transform attackTransform = attackVisual.transform;
            Vector3 templateScale =
                attackVisualTemplate.transform.localScale;
            float depthOffset =
                attackVisualTemplate.transform.localPosition.z;
            attackTransform.localPosition =
                new Vector3(0f, 0f, depthOffset);
            attackTransform.localRotation = Quaternion.identity;
            attackTransform.localScale =
                new Vector3(
                    templateScale.x,
                    0f,
                    templateScale.z);

            SpriteRenderer attackRenderer =
                attackVisual.GetComponent<SpriteRenderer>();
            attackRenderer.sortingOrder = Mathf.Max(
                VisualSortingOrder,
                attackRenderer.sortingOrder);
            float spriteHeight =
                attackRenderer.sprite != null
                    ? attackRenderer.sprite.bounds.size.y
                    : 1f;

            SpriteRenderer readyRenderer =
                readySwordVisuals[index];
            readyRenderer.sortingOrder = Mathf.Max(
                VisualSortingOrder,
                readyRenderer.sortingOrder);
            readyRenderer.gameObject.SetActive(false);
            attackVisual.SetActive(false);

            return new SwordSlot(
                root,
                readyRenderer.gameObject,
                attackVisual,
                attackRenderer,
                templateScale.x,
                templateScale.z,
                depthOffset,
                Mathf.Max(
                    Mathf.Epsilon,
                    spriteHeight));
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot == null)
            {
                var rootObject =
                    new GameObject("FlyingSwordAttacks");
                visualRoot = rootObject.transform;
            }

            Transform parent =
                owner != null ? owner.transform.parent : null;
            if (visualRoot.parent != parent)
            {
                visualRoot.SetParent(parent, true);
            }
        }

        private void ResolvePrefabVisuals()
        {
            if (readySwordVisuals == null ||
                readySwordVisuals.Length != MaximumSwordCount)
            {
                SpriteRenderer[] previous = readySwordVisuals;
                readySwordVisuals =
                    new SpriteRenderer[MaximumSwordCount];
                int copyCount = previous != null
                    ? Mathf.Min(
                        previous.Length,
                        readySwordVisuals.Length)
                    : 0;
                for (int index = 0;
                     index < copyCount;
                     index++)
                {
                    readySwordVisuals[index] =
                        previous[index];
                }
            }

            Transform readyRoot = owner != null
                ? owner.transform.Find("Visual")
                : null;
            for (int index = 0;
                 index < readySwordVisuals.Length;
                 index++)
            {
                if (readySwordVisuals[index] == null &&
                    readyRoot != null)
                {
                    Transform candidate = readyRoot.Find(
                        $"Flying_Sword{index + 1}");
                    readySwordVisuals[index] =
                        candidate != null
                            ? candidate.GetComponent<SpriteRenderer>()
                            : null;
                }

                SpriteRenderer readyRenderer =
                    readySwordVisuals[index];
                if (readyRenderer != null)
                {
                    readyRenderer.sortingOrder = Mathf.Max(
                        VisualSortingOrder,
                        readyRenderer.sortingOrder);
                    readyRenderer.gameObject.SetActive(false);
                }
            }

            if (attackVisualTemplate == null &&
                owner != null)
            {
                Transform candidate = owner.transform.Find(
                    "Flying_Sword_Attack");
                attackVisualTemplate =
                    candidate != null
                        ? candidate.GetComponent<SpriteRenderer>()
                        : null;
            }

            if (attackVisualTemplate != null)
            {
                attackVisualTemplate.sortingOrder = Mathf.Max(
                    VisualSortingOrder,
                    attackVisualTemplate.sortingOrder);
                attackVisualTemplate.gameObject.SetActive(false);
            }
        }

        private void RefreshReadyIndicators()
        {
            if (readySwordVisuals == null)
            {
                return;
            }

            bool canShow =
                owner != null &&
                owner.IsAlive;
            for (int index = 0;
                 index < readySwordVisuals.Length;
                 index++)
            {
                SpriteRenderer readyRenderer =
                    readySwordVisuals[index];
                if (readyRenderer == null)
                {
                    continue;
                }

                bool ready = canShow &&
                    index < swordCountLevel &&
                    index < slots.Count &&
                    slots[index].State == SwordState.Ready;
                readyRenderer.gameObject.SetActive(ready);
            }
        }

        private static void RefreshAttackVisual(
            SwordSlot slot,
            float alpha = 1f)
        {
            float length = Mathf.Max(
                0f,
                Vector2.Dot(
                    (Vector2)slot.Transform.position -
                    slot.AttackOrigin,
                    slot.Direction));
            Transform attackTransform =
                slot.AttackVisual.transform;
            attackTransform.localPosition =
                new Vector3(
                    0f,
                    -length * 0.5f,
                    slot.AttackDepthOffset);
            attackTransform.localScale =
                new Vector3(
                    slot.AttackWidthScale,
                    length / slot.AttackSpriteHeight,
                    slot.AttackDepthScale);

            Color color = slot.AttackBaseColor;
            color.a *= Mathf.Clamp01(alpha);
            slot.AttackRenderer.color = color;
        }

        private static void RestoreAttackColor(
            SwordSlot slot)
        {
            slot.AttackRenderer.color =
                slot.AttackBaseColor;
        }

        private static void HideSlotVisuals(
            SwordSlot slot)
        {
            slot.IndicatorVisual.SetActive(false);
            slot.AttackVisual.SetActive(false);
            RestoreAttackColor(slot);
        }

        private void CancelFlights()
        {
            foreach (SwordSlot slot in slots)
            {
                if (slot.State != SwordState.Approaching &&
                    slot.State != SwordState.Passing)
                {
                    continue;
                }

                slot.State = SwordState.Cooling;
                slot.PrimaryTarget = null;
                slot.HitEnemies.Clear();
                slot.AttackVisual.SetActive(false);
                RestoreAttackColor(slot);
                slot.ReadyAt = Mathf.Max(
                    slot.ReadyAt,
                    Time.time);
            }

            hitCandidates.Clear();
        }

        private void HideAllVisuals()
        {
            foreach (SwordSlot slot in slots)
            {
                HideSlotVisuals(slot);
            }

            if (readySwordVisuals != null)
            {
                foreach (SpriteRenderer readyRenderer in
                         readySwordVisuals)
                {
                    if (readyRenderer != null)
                    {
                        readyRenderer.gameObject.SetActive(false);
                    }
                }
            }

            if (attackVisualTemplate != null)
            {
                attackVisualTemplate.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            CancelFlights();
            HideAllVisuals();
        }

        private static void SetWorldPosition(
            SwordSlot slot,
            Vector2 position)
        {
            Vector3 current = slot.Transform.position;
            slot.Transform.position = new Vector3(
                position.x,
                position.y,
                current.z);
        }

        private static void FaceDirection(
            Transform target,
            Vector2 direction)
        {
            float angle =
                Mathf.Atan2(direction.y, direction.x) *
                Mathf.Rad2Deg -
                90f;
            target.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDestroy()
        {
            if (visualRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(visualRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(visualRoot.gameObject);
                }
            }

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
            public HashSet<EnemyBase> HitEnemies { get; } = new();
            public SwordState State { get; set; }
            public EnemyBase PrimaryTarget { get; set; }
            public Vector2 TargetPosition { get; set; }
            public Vector2 AttackOrigin { get; set; }
            public Vector2 Direction { get; set; }
            public float Speed { get; set; }
            public float RemainingPassDuration { get; set; }
            public float ReadyAt { get; set; }
            public int RemainingHits { get; set; }
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
