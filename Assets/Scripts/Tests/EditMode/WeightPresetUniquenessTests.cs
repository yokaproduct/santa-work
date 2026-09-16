using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Santa.MicroGames.Weight;
using UnityEditor;
using UnityEngine;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// M3(重さ)の6プリセットについて、`22_MicroGame_Weight.md` §5.2 の2条件
    /// (①目標値ちょうどになる部分集合がちょうど1通り ②同じ数値の箱が無い)を
    /// **手計算ではなくプログラムで**再検証する。
    ///
    /// 取りまとめ役からの依頼「M3の一意性検証などは手計算なので、プログラムで再検証する価値がある」
    /// に対応するテスト。`Tools/Scaffold/初期データを投入` の実行後、実際に投入された
    /// `Assets/Settings/Questions/WeightGeneratorSettings.asset` を読み込んで検証する
    /// (テストコード内に別途データを書き写さない。実データそのものを検証することに意味がある)。
    /// </summary>
    public class WeightPresetUniquenessTests
    {
        private const string AssetPath = "Assets/Settings/Questions/WeightGeneratorSettings.asset";

        private static WeightGeneratorSettings LoadSettingsOrSkip()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WeightGeneratorSettings>(AssetPath);
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
            Assert.AreEqual(6, settings.Presets.Count, "プロトタイプ用プリセットは6件のはず(共通仕様 §8.1)");
        }

        [Test]
        public void AllBoxValuesAreDistinctWithinEachPreset()
        {
            var settings = LoadSettingsOrSkip();
            foreach (var preset in settings.Presets)
            {
                var distinct = preset.boxValues.Distinct().Count();
                Assert.AreEqual(preset.boxValues.Length, distinct,
                    $"{preset.id}: 同じ数値の箱が2つ以上存在する(条件2違反)");
            }
        }

        [Test]
        public void ExactlyOneSubsetSumsToTarget_ForEveryPreset()
        {
            var settings = LoadSettingsOrSkip();
            foreach (var preset in settings.Presets)
            {
                int matchCount = CountSubsetsMatchingTarget(preset.boxValues, preset.target, out var matchingSubsets);
                Assert.AreEqual(1, matchCount,
                    $"{preset.id}: 目標値{preset.target}に一致する部分集合が{matchCount}通りある" +
                    $"(条件1違反。一致した組み合わせ: {DescribeSubsets(preset.boxValues, matchingSubsets)})");
            }
        }

        [Test]
        public void BoxCountIsSix_ForEveryPreset()
        {
            var settings = LoadSettingsOrSkip();
            foreach (var preset in settings.Presets)
            {
                Assert.AreEqual(6, preset.boxValues.Length, $"{preset.id}: 箱の数が6ではない(設計原則2)");
            }
        }

        /// <summary>6個の部分集合(2^6-1=63通り)を全探索し、目標値に一致する数を返す。</summary>
        private static int CountSubsetsMatchingTarget(int[] values, int target, out List<int> matchingMasks)
        {
            matchingMasks = new List<int>();
            int n = values.Length;
            for (int mask = 1; mask < (1 << n); mask++)
            {
                int sum = 0;
                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) != 0) sum += values[i];
                }
                if (sum == target)
                {
                    matchingMasks.Add(mask);
                }
            }
            return matchingMasks.Count;
        }

        private static string DescribeSubsets(int[] values, List<int> masks)
        {
            var descriptions = new List<string>();
            foreach (var mask in masks)
            {
                var parts = new List<string>();
                for (int i = 0; i < values.Length; i++)
                {
                    if ((mask & (1 << i)) != 0) parts.Add(values[i].ToString());
                }
                descriptions.Add(string.Join("+", parts));
            }
            return string.Join(" / ", descriptions);
        }
    }
}
