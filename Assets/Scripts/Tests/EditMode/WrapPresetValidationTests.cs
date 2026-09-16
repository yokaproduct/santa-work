using System.Linq;
using NUnit.Framework;
using Santa.MicroGames.Wrap;
using UnityEditor;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// M5(ラッピング)の6プリセットを、`23_MicroGame_Wrap.md` §5.2 のバリデーション条件で再検証する。
    /// `Assets/Settings/Questions/WrapGeneratorSettings.asset`(実データ)を直接読み込んで検証する。
    /// </summary>
    public class WrapPresetValidationTests
    {
        private const string AssetPath = "Assets/Settings/Questions/WrapGeneratorSettings.asset";

        private static WrapGeneratorSettings LoadSettingsOrSkip()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WrapGeneratorSettings>(AssetPath);
            if (settings == null)
            {
                Assert.Ignore($"{AssetPath} がまだ存在しません。先に Tools/Scaffold/初期データを投入 を実行してください。");
            }
            return settings;
        }

        [Test]
        public void SixPresetsExist()
        {
            var settings = LoadSettingsOrSkip();
            Assert.AreEqual(6, settings.Presets.Count,
                "★2026-09-13: 形カテゴリ(R04〜R06)は出題対象から外したがデータは削除していないので、6件のまま");
        }

        [Test]
        public void ShapeCategoryPresets_AreDisabled_ButNotDeleted()
        {
            // ★2026-09-13 ディレクター決定: 形カテゴリ(R04〜R06)をいったん出題なしにする。
            //   「戻せる形」にするため enabled フラグで無効化するだけで、プリセット自体は残す。
            var settings = LoadSettingsOrSkip();
            foreach (var preset in settings.Presets)
            {
                if (preset.category == WrapCategory.Shape)
                {
                    Assert.IsFalse(preset.enabled, $"{preset.id}: 形カテゴリは一時停止中のはず(enabled=false)");
                }
                else
                {
                    Assert.IsTrue(preset.enabled, $"{preset.id}: 色カテゴリは出題対象のはず(enabled=true)");
                }
            }
        }

        [Test]
        public void ActivePresets_ExcludesDisabledPresets_AndOnlyContainsColorCategory()
        {
            var settings = LoadSettingsOrSkip();
            var active = settings.ActivePresets;

            Assert.AreEqual(3, active.Count,
                "形カテゴリ3件を除いた色カテゴリ3件(R01〜R03)だけが出題対象のはず");
            foreach (var preset in active)
            {
                Assert.AreEqual(WrapCategory.Color, preset.category, $"{preset.id}: ActivePresetsに形カテゴリが混ざっている");
            }
        }

        [Test]
        public void EachPresetHasNineBoxes_AndThreeChuteSamples()
        {
            var settings = LoadSettingsOrSkip();
            foreach (var preset in settings.Presets)
            {
                Assert.AreEqual(9, preset.sequence.Length, $"{preset.id}: 9箱(3問×3ループ)のはず");
                Assert.AreEqual(3, preset.chuteSamples.Length, $"{preset.id}: シュートは3口のはず");
            }
        }

        [Test]
        public void TrashCount_IsBetweenOneAndThree()
        {
            var settings = LoadSettingsOrSkip();
            foreach (var preset in settings.Presets)
            {
                int trashCount = preset.sequence.Count(b => b.isTrash);
                Assert.GreaterOrEqual(trashCount, 1, $"{preset.id}: ゴミが1個もない(条件3違反)");
                Assert.LessOrEqual(trashCount, 3, $"{preset.id}: ゴミが3個を超えている(条件3違反)");
            }
        }

        [Test]
        public void FirstBoxIsNeverTrash()
        {
            var settings = LoadSettingsOrSkip();
            foreach (var preset in settings.Presets)
            {
                Assert.IsFalse(preset.sequence[0].isTrash, $"{preset.id}: 1個目の箱がゴミになっている(条件4違反)");
            }
        }

        [Test]
        public void NoLoopContainsThreeTrashBoxes()
        {
            var settings = LoadSettingsOrSkip();
            foreach (var preset in settings.Presets)
            {
                for (int loop = 0; loop < 3; loop++)
                {
                    int trashInLoop = 0;
                    for (int i = loop * 3; i < loop * 3 + 3; i++)
                    {
                        if (preset.sequence[i].isTrash) trashInLoop++;
                    }
                    Assert.Less(trashInLoop, 3, $"{preset.id}: ループ{loop + 1}が3箱ともゴミになっている(条件5違反)");
                }
            }
        }

        [Test]
        public void NonTrashBoxes_HaveValidChuteIndex()
        {
            var settings = LoadSettingsOrSkip();
            foreach (var preset in settings.Presets)
            {
                foreach (var box in preset.sequence)
                {
                    if (box.isTrash) continue;
                    Assert.GreaterOrEqual(box.chuteIndex, 0, $"{preset.id}: 通常箱のchuteIndexが範囲外");
                    Assert.LessOrEqual(box.chuteIndex, 2, $"{preset.id}: 通常箱のchuteIndexが範囲外");
                }
            }
        }
    }
}
