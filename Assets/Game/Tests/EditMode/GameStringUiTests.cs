using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SimpleGame.Tests
{
    public sealed class GameStringUiTests
    {
        [Test]
        public void HudView_UsesImportedStringsWithoutPrefabRegeneration()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/PrototypeHUD.prefab");
            Assert.That(prefab, Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            GameStringTable strings =
                ScriptableObject.CreateInstance<GameStringTable>();
            strings.Configure(new[]
            {
                new GameStringEntry(
                    GameStringIds.UiSettingsButton,
                    "문자열 설정"),
                new GameStringEntry(
                    GameStringIds.UiDifficultyTitle,
                    "문자열 난이도"),
                new GameStringEntry(
                    GameStringIds.UiDifficultyStageFormat,
                    "{0} / {1}"),
                new GameStringEntry(
                    GameStringIds.UiDifficultyOptionFormat,
                    "{0}: {1}"),
                new GameStringEntry(
                    GameStringIds.DifficultyEasyName,
                    "문자열 쉬움"),
                new GameStringEntry(
                    GameStringIds.DifficultyEasyDescription,
                    "문자열 쉬움 설명"),
                new GameStringEntry(
                    GameStringIds.DifficultyNormalName,
                    "문자열 보통"),
                new GameStringEntry(
                    GameStringIds.DifficultyNormalDescription,
                    "문자열 보통 설명")
            });

            try
            {
                PrototypeHUDView view =
                    instance.GetComponent<PrototypeHUDView>();
                Assert.That(view, Is.Not.Null);

                view.Initialize(strings);
                view.SetDifficultyContext(
                    "문자열 스테이지",
                    "문자열 스테이지 설명");
                view.ShowDifficultySelection(true);

                Assert.That(
                    view.SettingsButton
                        .GetComponentInChildren<TMP_Text>(true)
                        .text,
                    Is.EqualTo("문자열 설정"));

                Transform panel = instance.transform.Find(
                    "ModalRoot/DifficultySelectionPanel");
                Assert.That(panel, Is.Not.Null);
                Assert.That(
                    panel.Find("DifficultyTitle")
                        .GetComponent<TMP_Text>().text,
                    Is.EqualTo("문자열 난이도"));
                Assert.That(
                    panel.Find("DifficultyDescription")
                        .GetComponent<TMP_Text>().text,
                    Is.EqualTo(
                        "문자열 스테이지 / 문자열 스테이지 설명"));
                Assert.That(
                    panel.Find(HudButtonId.DifficultyEasy.ToString())
                        .GetComponentInChildren<TMP_Text>(true).text,
                    Is.EqualTo("문자열 쉬움: 문자열 쉬움 설명"));
                Assert.That(
                    panel.Find(HudButtonId.DifficultyNormal.ToString())
                        .GetComponentInChildren<TMP_Text>(true).text,
                    Is.EqualTo("문자열 보통: 문자열 보통 설명"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(strings);
            }
        }
    }
}
