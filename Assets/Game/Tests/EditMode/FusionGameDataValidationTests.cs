using System.IO;
using System.Reflection;
using NUnit.Framework;
using SimpleGameEditor;

namespace SimpleGame.Tests
{
    public sealed class FusionGameDataValidationTests
    {
        [Test]
        public void ValidateReferences_RejectsFusionIngredientCards()
        {
            var model = new GameDataExcelModel();
            model.LevelUpCards.Add(CreateCard("BASE_A"));
            model.LevelUpCards.Add(CreateCard("BASE_B"));
            model.LevelUpCards.Add(CreateFusionCard(
                "FUSION_A",
                "BASE_A|BASE_B"));
            model.LevelUpCards.Add(CreateFusionCard(
                "FUSION_B",
                "FUSION_A|BASE_B"));

            MethodInfo validateReferences =
                typeof(GameDataExcelParser).GetMethod(
                    "ValidateReferences",
                    BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(validateReferences, Is.Not.Null);

            TargetInvocationException exception = Assert.Throws<
                TargetInvocationException>(() =>
                    validateReferences.Invoke(null, new object[] { model }));

            Assert.That(
                exception.InnerException,
                Is.TypeOf<InvalidDataException>());
            StringAssert.Contains(
                "cannot use fusion card 'FUSION_A'",
                exception.InnerException.Message);
        }

        private static LevelUpCardDefinition CreateCard(string cardId)
        {
            return new LevelUpCardDefinition(
                cardId,
                cardId,
                cardId,
                cardId,
                LevelUpCardEffectType.UpgradeRank,
                PlayerStatId.Piercing,
                StatOperation.Add,
                1f,
                1,
                1,
                1,
                string.Empty,
                "희귀",
                "ICON_TEST",
                true);
        }

        private static LevelUpCardDefinition CreateFusionCard(
            string cardId,
            string ingredientCardIds)
        {
            return new LevelUpCardDefinition(
                cardId,
                cardId,
                cardId,
                cardId,
                LevelUpCardEffectType.Fusion,
                PlayerStatId.FlyingSwordPiercingFusion,
                StatOperation.Add,
                1f,
                1,
                1,
                1,
                string.Empty,
                "레전더리",
                "ICON_TEST",
                true,
                ingredientCardIds);
        }
    }
}
