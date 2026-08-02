using UnityEngine;

namespace SimpleGame
{
    public sealed partial class PlayerCombatAbilities
    {
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
                        CalculateMovingSlashDamageMultiplier(
                            movingSlashLevel,
                            card.Value);
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
                case PlayerStatId.FilthThrow:
                    bool firstFilthThrowLevel =
                        filthThrowLevel <= 0;
                    filthThrowLevel = AddLevel(
                        filthThrowLevel,
                        card.MaxStack,
                        1f);
                    filthThrowBaseDamageMultiplier =
                        Mathf.Max(0f, card.Value);
                    if (firstFilthThrowLevel)
                    {
                        nextFilthThrowAt =
                            Time.time + FilthThrowInitialDelay;
                    }

                    break;
                case PlayerStatId.FlyingSwordPiercingFusion:
                    return ApplyFlyingSwordPiercingFusion();
                case PlayerStatId.FlyingSwordStaticFusion:
                    return ApplyFlyingSwordStaticFusion();
                case PlayerStatId.StaticFilthFusion:
                    return ApplyStaticFilthFusion();
                default:
                    return false;
            }

            flyingSwords?.SetLevels(
                flyingSwordCountLevel,
                flyingSwordHitCountLevel);
            return true;
        }

        private bool ApplyFlyingSwordPiercingFusion()
        {
            if (hasFlyingSwordPiercingFusion ||
                flyingSwordCountLevel <
                    FlyingSwordController.MaximumSwordCount ||
                flyingSwordHitCountLevel <
                    FlyingSwordController.MaximumHitUpgradeLevel ||
                piercingLevel < PiercingMaximumLevel)
            {
                return false;
            }

            flyingSwordPiercingCountSnapshot =
                flyingSwordCountLevel;
            flyingSwordPiercingHitsSnapshot =
                flyingSwordHitCountLevel;
            flyingSwordPiercingFusion = CreateFusionSwordController(
                "FlyingSwordPiercingFusion",
                true,
                0,
                0f,
                flyingSwordPiercingCountSnapshot,
                flyingSwordPiercingHitsSnapshot);
            hasFlyingSwordPiercingFusion = true;
            ResetBaseFlyingSword();
            ResetBasePiercing();
            return true;
        }

        private bool ApplyFlyingSwordStaticFusion()
        {
            if (hasFlyingSwordStaticFusion ||
                flyingSwordCountLevel <
                    FlyingSwordController.MaximumSwordCount ||
                flyingSwordHitCountLevel <
                    FlyingSwordController.MaximumHitUpgradeLevel ||
                staticChargeLevel < StaticChargeMaximumLevel)
            {
                return false;
            }

            flyingSwordStaticCountSnapshot =
                flyingSwordCountLevel;
            flyingSwordStaticHitsSnapshot =
                flyingSwordHitCountLevel;
            flyingSwordStaticChargeSnapshot =
                staticChargeLevel;
            flyingSwordStaticDamageSnapshot =
                staticDamageMultiplier;
            flyingSwordStaticFusion = CreateFusionSwordController(
                "FlyingSwordStaticFusion",
                false,
                flyingSwordStaticChargeSnapshot,
                flyingSwordStaticDamageSnapshot,
                flyingSwordStaticCountSnapshot,
                flyingSwordStaticHitsSnapshot);
            hasFlyingSwordStaticFusion = true;
            ResetBaseFlyingSword();
            ResetBaseStaticCharge();
            return true;
        }

        private bool ApplyStaticFilthFusion()
        {
            if (hasStaticFilthFusion ||
                staticChargeLevel < StaticChargeMaximumLevel ||
                filthThrowLevel < FilthThrowMaximumLevel)
            {
                return false;
            }

            staticFilthLevelSnapshot = filthThrowLevel;
            staticFilthDamageSnapshot =
                filthThrowBaseDamageMultiplier;
            staticFilthChargeSnapshot = staticChargeLevel;
            staticFilthChargeDamageSnapshot =
                staticDamageMultiplier;
            nextStaticFilthThrowAt = nextFilthThrowAt;
            hasStaticFilthFusion = true;
            ResetBaseStaticCharge();
            ResetBaseFilthThrow();
            return true;
        }

        private FlyingSwordController CreateFusionSwordController(
            string objectName,
            bool piercesEntirePath,
            int fusionStaticChargeLevel,
            float fusionStaticDamageMultiplier,
            int swordCount,
            int swordHits)
        {
            var controllerObject = new GameObject(objectName);
            controllerObject.transform.SetParent(transform, false);
            FlyingSwordController controller =
                controllerObject.AddComponent<FlyingSwordController>();
            controller.Configure(
                owner,
                enemyWorld,
                spawnPoints,
                false);
            controller.ConfigureFusionEffects(
                piercesEntirePath,
                fusionStaticChargeLevel,
                fusionStaticDamageMultiplier);
            controller.SetLevels(swordCount, swordHits);
            return controller;
        }

        private void ResetBaseFlyingSword()
        {
            flyingSwordCountLevel = 0;
            flyingSwordHitCountLevel = 0;
            flyingSwords?.SetLevels(0, 0);
        }

        private void ResetBasePiercing()
        {
            piercingLevel = 0;
            piercingWindowEndsAt = 0f;
            piercingTargetsConsumed = 0;
        }

        private void ResetBaseStaticCharge()
        {
            staticChargeLevel = 0;
        }

        private void ResetBaseFilthThrow()
        {
            filthThrowLevel = 0;
            nextFilthThrowAt = float.PositiveInfinity;
        }

        private static void DestroyFusionController(
            ref FlyingSwordController controller)
        {
            if (controller == null)
            {
                return;
            }

            GameObject controllerObject = controller.gameObject;
            controller = null;
            if (Application.isPlaying)
            {
                Destroy(controllerObject);
            }
            else
            {
                DestroyImmediate(controllerObject);
            }
        }
    }
}
