using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class FilthProjectile : MonoBehaviour
    {
        public const float ThrowDuration = 0.45f;
        public const float FieldDuration = 3f;
        public const float DamageTickInterval = 0.5f;
        public const float ArcHeight = 1.4f;

        [SerializeField] private SpriteRenderer orbRenderer;
        [SerializeField] private GameObject fieldVisual;

        private readonly List<EnemyBase> damageTargets = new();
        private readonly Dictionary<EnemyBase, uint>
            staticTriggeredEnemyGenerations = new();
        private PlayerRoot owner;
        private EnemyWorldService enemyWorld;
        private Vector2 start;
        private Vector2 destination;
        private float damageMultiplier;
        private float damageRadius;
        private int staticChargeLevel;
        private float staticDamageMultiplier;
        private float throwElapsed;
        private float fieldElapsed;
        private int appliedTickCount;
        private bool landed;

        public static void Spawn(
            FilthProjectile prefab,
            PlayerRoot owner,
            EnemyWorldService enemyWorld,
            Vector2 destination,
            float damageMultiplier,
            float damageRadius,
            int staticChargeLevel = 0,
            float staticDamageMultiplier = 0f)
        {
            if (prefab == null)
            {
                Debug.LogError(
                    "Filth projectile prefab is not assigned.",
                    owner);
                return;
            }

            FilthProjectile projectile = Instantiate(prefab);
            projectile.name = "FilthProjectile";
            projectile.Configure(
                owner,
                enemyWorld,
                destination,
                damageMultiplier,
                damageRadius,
                staticChargeLevel,
                staticDamageMultiplier);
        }

        public void ConfigureVisuals(
            SpriteRenderer configuredOrbRenderer,
            GameObject configuredFieldVisual)
        {
            orbRenderer = configuredOrbRenderer;
            fieldVisual = configuredFieldVisual;
        }

        public static int CalculateTickCount(float exposureDuration)
        {
            return Mathf.Clamp(
                Mathf.FloorToInt(
                    Mathf.Max(0f, exposureDuration) /
                    DamageTickInterval),
                0,
                Mathf.RoundToInt(
                    FieldDuration / DamageTickInterval));
        }

        public static bool ShouldTriggerStaticBurst(
            int staticChargeLevel,
            bool hasRecordedGeneration,
            uint recordedGeneration,
            uint currentGeneration)
        {
            return staticChargeLevel > 0 &&
                (!hasRecordedGeneration ||
                 recordedGeneration != currentGeneration);
        }

        public static Vector2 CalculateArcPosition(
            Vector2 start,
            Vector2 destination,
            float normalizedTime,
            float height = ArcHeight)
        {
            float progress = Mathf.Clamp01(normalizedTime);
            Vector2 linear = Vector2.Lerp(
                start,
                destination,
                progress);
            return linear +
                Vector2.up *
                Mathf.Max(0f, height) *
                4f *
                progress *
                (1f - progress);
        }

        private void Configure(
            PlayerRoot configuredOwner,
            EnemyWorldService configuredEnemyWorld,
            Vector2 configuredDestination,
            float configuredDamageMultiplier,
            float configuredDamageRadius,
            int configuredStaticChargeLevel,
            float configuredStaticDamageMultiplier)
        {
            owner = configuredOwner;
            enemyWorld = configuredEnemyWorld;
            start = owner != null
                ? owner.transform.position
                : transform.position;
            destination = configuredDestination;
            damageMultiplier =
                Mathf.Max(0f, configuredDamageMultiplier);
            damageRadius = Mathf.Max(0.1f, configuredDamageRadius);
            staticChargeLevel = Mathf.Max(
                0,
                configuredStaticChargeLevel);
            staticDamageMultiplier = Mathf.Max(
                0f,
                configuredStaticDamageMultiplier);
            throwElapsed = 0f;
            fieldElapsed = 0f;
            appliedTickCount = 0;
            staticTriggeredEnemyGenerations.Clear();
            landed = false;
            transform.position = start;
            transform.localScale = Vector3.one;
            if (orbRenderer != null)
            {
                orbRenderer.enabled = true;
            }

            if (fieldVisual != null)
            {
                fieldVisual.SetActive(false);
                fieldVisual.transform.localScale =
                    Vector3.one * damageRadius * 2f;
            }
        }

        private void Update()
        {
            if (owner == null ||
                enemyWorld == null ||
                !owner.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            if (!landed)
            {
                throwElapsed += Time.deltaTime;
                float progress = ThrowDuration > 0f
                    ? throwElapsed / ThrowDuration
                    : 1f;
                transform.position = CalculateArcPosition(
                    start,
                    destination,
                    progress);
                if (progress >= 1f)
                {
                    Land();
                }

                return;
            }

            fieldElapsed += Time.deltaTime;
            int requiredTickCount =
                CalculateTickCount(fieldElapsed);
            while (appliedTickCount < requiredTickCount)
            {
                appliedTickCount++;
                DamageEnemies();
            }

            if (fieldElapsed >= FieldDuration)
            {
                Destroy(gameObject);
            }
        }

        private void Land()
        {
            landed = true;
            transform.position = destination;
            if (orbRenderer != null)
            {
                orbRenderer.enabled = false;
            }

            if (fieldVisual != null)
            {
                fieldVisual.SetActive(true);
            }
        }

        private void DamageEnemies()
        {
            enemyWorld.FillEnemiesInRadius(
                destination,
                damageRadius,
                damageTargets);
            foreach (EnemyBase enemy in damageTargets)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                bool hasRecordedGeneration =
                    staticTriggeredEnemyGenerations.TryGetValue(
                        enemy,
                        out uint recordedGeneration);
                if (ShouldTriggerStaticBurst(
                        staticChargeLevel,
                        hasRecordedGeneration,
                        recordedGeneration,
                        enemy.SpawnGeneration))
                {
                    staticTriggeredEnemyGenerations[enemy] =
                        enemy.SpawnGeneration;
                    owner.ApplySkillHitWithStaticBurst(
                        enemy,
                        damageMultiplier,
                        staticChargeLevel,
                        staticDamageMultiplier);
                    continue;
                }

                owner.ApplySkillHit(enemy, damageMultiplier);
            }
        }

    }
}
