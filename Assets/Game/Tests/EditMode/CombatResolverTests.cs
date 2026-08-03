using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace SimpleGame.Tests
{
    public sealed class CombatResolverTests
    {
        [TestCase(
            EnemyArchetype.Melee,
            1,
            1,
            AttackSide.Front,
            1,
            3)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            1,
            AttackSide.Rear,
            3,
            3)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            2,
            AttackSide.Front,
            1,
            4)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            2,
            AttackSide.Rear,
            3,
            4)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            1,
            AttackSide.Front,
            1,
            3)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            2,
            AttackSide.Front,
            1,
            3)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            3,
            AttackSide.Rear,
            3,
            4)]
        public void Resolve_UsesAttackPowerAndScaledHealth(
            EnemyArchetype archetype,
            int playerLevel,
            int enemyLevel,
            AttackSide side,
            int expectedDamage,
            int expectedMaxHealth)
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(archetype);
            CombatResult result = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(enemyLevel),
                PlayerAttack(playerLevel),
                3f,
                side,
                false);

            Assert.That(result.Damage, Is.EqualTo(expectedDamage));
            Assert.That(
                result.TargetMaxHealth,
                Is.EqualTo(expectedMaxHealth));
        }

        [Test]
        public void Resolve_CriticalMultipliesDamageWithoutArmor()
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(EnemyArchetype.Melee);

            CombatResult front = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(2),
                1,
                3f,
                AttackSide.Front,
                true);
            CombatResult rear = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(2),
                1,
                3f,
                AttackSide.Rear,
                true);

            Assert.That(front.Damage, Is.EqualTo(3));
            Assert.That(rear.Damage, Is.EqualTo(9));
        }

        [TestCase(10, 1, false)]
        [TestCase(1, 1, true)]
        [TestCase(1, 2, true)]
        public void Resolve_ShieldFrontRecoilUsesActualDamage(
            int playerLevel,
            int enemyLevel,
            bool expectedRecoil)
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(EnemyArchetype.Shield);
            CombatResult result = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(enemyLevel),
                PlayerAttack(playerLevel),
                3f,
                AttackSide.Front,
                false);

            Assert.That(
                result.PlayerReaction == PlayerAttackReaction.Recoil,
                Is.EqualTo(expectedRecoil));
        }

        [Test]
        public void Resolve_CriticalShieldFrontHitBypassesRecoil()
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(EnemyArchetype.Shield);
            CombatResult result = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(2),
                1,
                3f,
                AttackSide.Front,
                true);

            Assert.That(result.Damage, Is.EqualTo(3));
            Assert.That(
                result.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.None));
        }

        [TestCase(EnemyArchetype.Melee)]
        [TestCase(EnemyArchetype.Ranged)]
        [TestCase(EnemyArchetype.Boss)]
        public void Resolve_HigherLevelNonShieldNeverLocksPlayerInput(
            EnemyArchetype archetype)
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(archetype);
            CombatResult result = CombatResolver.Resolve(
                definition,
                definition.CalculateMaxHealth(20),
                1,
                3f,
                AttackSide.Front,
                false);

            Assert.That(
                result.PlayerReaction,
                Is.EqualTo(PlayerAttackReaction.None));
        }

        [TestCase(true, false, true, PlayerAttackReaction.Recoil, CombatFeedbackLevel.CriticalHit)]
        [TestCase(true, true, false, PlayerAttackReaction.None, CombatFeedbackLevel.DefeatingHit)]
        [TestCase(false, false, false, PlayerAttackReaction.Recoil, CombatFeedbackLevel.FrontRecoil)]
        [TestCase(true, false, false, PlayerAttackReaction.None, CombatFeedbackLevel.NormalHit)]
        [TestCase(false, false, false, PlayerAttackReaction.None, CombatFeedbackLevel.None)]
        public void FeedbackResolver_SelectsOnlyTheLargestFeedback(
            bool damageApplied,
            bool targetDefeated,
            bool critical,
            PlayerAttackReaction reaction,
            CombatFeedbackLevel expected)
        {
            Assert.That(
                CombatFeedbackResolver.Resolve(
                    damageApplied,
                    targetDefeated,
                    critical,
                    reaction),
                Is.EqualTo(expected));
        }

        [TestCase(0, "0")]
        [TestCase(-3, "0")]
        [TestCase(2, "2")]
        [TestCase(11, "11")]
        public void DamagePopup_FormatsActualDamageCompactly(
            int amount,
            string expected)
        {
            Assert.That(
                DamagePopupView.FormatDamage(amount),
                Is.EqualTo(expected));
        }

        [Test]
        public void DamagePopupController_ReusesInactivePopup()
        {
            var controllerObject = new GameObject("CombatFeedback");
            var templateObject = new GameObject("DamagePopupTemplate");
            try
            {
                DamagePopupView template =
                    templateObject.AddComponent<DamagePopupView>();
                var labelObject = new GameObject("DamageText");
                labelObject.transform.SetParent(
                    templateObject.transform,
                    false);
                template.Configure(
                    labelObject.AddComponent<TextMeshPro>());
                templateObject.SetActive(false);

                CombatFeedbackController controller =
                    controllerObject.AddComponent<
                        CombatFeedbackController>();
                controller.Configure(null, null, template);
                Assert.That(
                    controller.HasConfiguredDamagePopup,
                    Is.True);
                controller.ShowDamagePopup(
                    Vector3.zero,
                    3,
                    DamagePopupStyle.Dealt);

                DamagePopupView first = controllerObject
                    .GetComponentInChildren<DamagePopupView>(true);
                Assert.That(first, Is.Not.Null);
                Assert.That(first.IsPlaying, Is.True);
                TMP_Text firstLabel = first.GetComponentInChildren<
                    TMP_Text>(true);
                Assert.That(firstLabel.text, Is.EqualTo("3"));
                Assert.That(
                    firstLabel.fontStyle,
                    Is.EqualTo(FontStyles.Bold));
                Assert.That(firstLabel.raycastTarget, Is.False);
                Assert.That(
                    firstLabel.GetComponent<Renderer>().sortingOrder,
                    Is.GreaterThanOrEqualTo(220));

                first.Stop();
                controller.ShowDamagePopup(
                    Vector3.one,
                    4,
                    DamagePopupStyle.Received);
                DamagePopupView reused = controllerObject
                    .GetComponentInChildren<DamagePopupView>(true);

                Assert.That(reused, Is.SameAs(first));
                Assert.That(
                    controllerObject
                        .GetComponentsInChildren<DamagePopupView>(true),
                    Has.Length.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(templateObject);
            }
        }

        [Test]
        public void DamagePopupController_ReportsMissingPrefab()
        {
            var controllerObject = new GameObject("CombatFeedback");
            try
            {
                CombatFeedbackController controller =
                    controllerObject.AddComponent<
                        CombatFeedbackController>();
                LogAssert.Expect(
                    LogType.Warning,
                    "Damage popup prefab is not configured.");

                controller.ShowDamagePopup(
                    Vector3.zero,
                    1,
                    DamagePopupStyle.Dealt);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
            }
        }

        [TestCase(EnemyArchetype.Shield, AttackSide.Front, false, false)]
        [TestCase(EnemyArchetype.Shield, AttackSide.Front, true, true)]
        [TestCase(EnemyArchetype.Shield, AttackSide.Rear, false, true)]
        [TestCase(EnemyArchetype.Melee, AttackSide.Front, false, true)]
        public void PiercingPastTarget_StopsAtLivingFrontShield(
            EnemyArchetype archetype,
            AttackSide side,
            bool targetDefeated,
            bool expected)
        {
            Assert.That(
                CombatResolver.CanPiercePastTarget(
                    archetype,
                    side,
                    targetDefeated),
                Is.EqualTo(expected));
        }

        [Test]
        public void ShieldDefinition_UsesCyanApproachRange()
        {
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Shield)
                    .ApproachRange,
                Is.EqualTo(2.25f));
        }

        [TestCase(1, 2)]
        [TestCase(10, 4)]
        [TestCase(50, 6)]
        [TestCase(200, 8)]
        public void EnemyAttackDamage_UsesCurveAndCapsAtEight(
            int level,
            int expectedDamage)
        {
            Assert.That(
                PrototypeEnemyDefinitions
                    .Create(EnemyArchetype.Melee)
                    .CalculateAttackDamage(level),
                Is.EqualTo(expectedDamage));
        }

        [Test]
        public void GetAttackSide_UsesEnemyFacing()
        {
            Assert.That(
                CombatResolver.GetAttackSide(
                    Vector2.up,
                    Vector2.zero,
                    Vector2.up),
                Is.EqualTo(AttackSide.Front));
            Assert.That(
                CombatResolver.GetAttackSide(
                    Vector2.up,
                    Vector2.zero,
                    Vector2.down),
                Is.EqualTo(AttackSide.Rear));
        }

        [TestCase(
            EnemyArchetype.Melee,
            10,
            1,
            EnemyThreatLevel.OneHit)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            1,
            EnemyThreatLevel.ThreeFrontOneRear)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            2,
            EnemyThreatLevel.ThreeFrontOneRear)]
        [TestCase(
            EnemyArchetype.Melee,
            1,
            2,
            EnemyThreatLevel.Dangerous)]
        [TestCase(
            EnemyArchetype.Ranged,
            1,
            3,
            EnemyThreatLevel.Dangerous)]
        public void ThreatLevel_UsesCurrentHitCounts(
            EnemyArchetype archetype,
            int playerLevel,
            int enemyLevel,
            EnemyThreatLevel expected)
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(archetype);
            Assert.That(
                CombatResolver.GetThreatLevel(
                    definition,
                    definition.CalculateMaxHealth(enemyLevel),
                    PlayerAttack(playerLevel),
                3f),
                Is.EqualTo(expected));
        }

        [TestCase(1, 1f)]
        [TestCase(4, 1f)]
        [TestCase(5, 1.2f)]
        [TestCase(7, 1.2f)]
        [TestCase(8, 1.5f)]
        [TestCase(12, 1.9f)]
        [TestCase(16, 2.4f)]
        [TestCase(20, 3f)]
        [TestCase(24, 3.8f)]
        [TestCase(32, 4.8f)]
        [TestCase(40, 6f)]
        [TestCase(48, 7.5f)]
        [TestCase(56, 9.5f)]
        [TestCase(60, 9.5f)]
        public void WaveHealthMultiplier_UsesDesignedGates(
            int waveNumber,
            float expectedMultiplier)
        {
            Assert.That(
                ProgressionCurve.CalculateWaveHealthMultiplier(
                    waveNumber),
                Is.EqualTo(expectedMultiplier));
        }

        [Test]
        public void EnemyHealth_JumpsAtFiveAndEightThenGrowsByLevel()
        {
            EnemyDefinition definition =
                PrototypeEnemyDefinitions.Create(
                    EnemyArchetype.Melee);

            int waveFour = definition.CalculateMaxHealth(4, 4);
            int waveFive = definition.CalculateMaxHealth(4, 5);
            int waveSeven = definition.CalculateMaxHealth(5, 7);
            int waveEight = definition.CalculateMaxHealth(6, 8);

            Assert.That(waveFour, Is.EqualTo(5));
            Assert.That(waveFive, Is.EqualTo(6));
            Assert.That(waveSeven, Is.EqualTo(7));
            Assert.That(waveEight, Is.EqualTo(9));
        }

        [TestCase(0, 1f)]
        [TestCase(1, 1f)]
        [TestCase(10, 1.1125f)]
        [TestCase(25, 1.3f)]
        [TestCase(49, 1.6f)]
        [TestCase(100, 1.6f)]
        public void EnemyMoveSpeed_GrowsPerLevelAndCapsAtSixtyPercent(
            int level,
            float expectedMultiplier)
        {
            const float baseMoveSpeed = 0.8f;

            Assert.That(
                ProgressionCurve.CalculateEnemyMoveSpeed(
                    baseMoveSpeed,
                    level),
                Is.EqualTo(
                    baseMoveSpeed * expectedMultiplier)
                    .Within(0.0001f));
        }

        [TestCase(1, 1f)]
        [TestCase(4, 2.4768f)]
        [TestCase(50, 11.283f)]
        public void AdditiveProgressionCurve_GrowsWithoutExponentials(
            int level,
            float expected)
        {
            Assert.That(
                ProgressionCurve.CalculateAdditiveStat(
                    1f,
                    0.65f,
                    level),
                Is.EqualTo(expected).Within(0.001f));
        }

        [TestCase(1, 8)]
        [TestCase(8, 23)]
        [TestCase(50, 0)]
        public void RequiredExperience_PreservesEarlyPaceAndCapsAtFifty(
            int level,
            int expected)
        {
            Assert.That(
                ProgressionCurve.CalculateRequiredExperience(level),
                Is.EqualTo(expected));
        }

        [TestCase(0f, 0)]
        [TestCase(-0.1f, 0)]
        [TestCase(0.35f, 1)]
        [TestCase(1.77f, 2)]
        [TestCase(2.5f, 3)]
        public void RoundDamage_UsesConventionalWholeNumbers(
            float rawDamage,
            int expected)
        {
            Assert.That(
                CombatResolver.RoundDamage(rawDamage),
                Is.EqualTo(expected));
        }

        private static int PlayerAttack(int level)
        {
            return ProgressionCurve.RoundPositiveStat(
                ProgressionCurve.CalculateAdditiveStat(
                    1f,
                    0.65f,
                    level));
        }
    }
}
