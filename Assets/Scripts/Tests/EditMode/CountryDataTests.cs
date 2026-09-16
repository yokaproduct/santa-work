using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Santa.MicroGames.Address;
using UnityEditor;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// M2(住所)の実データ(`Assets/Settings/Questions/CountryTable.asset`)を、
    /// `21_MicroGame_Address.md` §5.3 のバリデーションでインポータとは独立に再検証する。
    /// </summary>
    public class CountryDataTests
    {
        private const string AssetPath = "Assets/Settings/Questions/CountryTable.asset";

        private static CountryTable LoadOrSkip()
        {
            var table = AssetDatabase.LoadAssetAtPath<CountryTable>(AssetPath);
            if (table == null || table.Countries.Count == 0)
            {
                Assert.Ignore($"{AssetPath} が空です。先に Tools/データ/問題データをインポート を実行してください。");
            }
            return table;
        }

        [Test]
        public void TenApprovedCountriesExist()
        {
            var table = LoadOrSkip();
            Assert.AreEqual(10, table.Countries.Count, "プロトタイプのM2国データは10か国のはず(共通仕様 §8.1)");
        }

        [Test]
        public void AllSixRegionsAreRepresented()
        {
            var table = LoadOrSkip();
            var regions = new HashSet<string>(table.Countries.Select(c => c.regionId));
            foreach (var region in Country.ValidRegionIds)
            {
                Assert.IsTrue(regions.Contains(region), $"'{region}' に該当する国が1つもない(§5.3-5。エラー扱いの条件)");
            }
        }

        [Test]
        public void AllRegionIds_AreValid()
        {
            var table = LoadOrSkip();
            foreach (var c in table.Countries)
            {
                CollectionAssert.Contains(Country.ValidRegionIds, c.regionId, $"{c.id}: region_id '{c.regionId}' が不正");
            }
        }

        [Test]
        public void NoIdDuplicates()
        {
            var table = LoadOrSkip();
            var ids = table.Countries.Select(c => c.id).ToList();
            var distinct = new HashSet<string>(ids);
            Assert.AreEqual(ids.Count, distinct.Count, "id が重複している国がある");
        }

        [Test]
        public void NoNamesAreEmpty()
        {
            var table = LoadOrSkip();
            foreach (var c in table.Countries)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.nameJa), $"{c.id}: name_ja が空");
                Assert.IsFalse(string.IsNullOrWhiteSpace(c.nameEn), $"{c.id}: name_en が空");
            }
        }

        // 共通仕様 §6.2 の除外国ブラックリストは QuestionDataImporter 側で機械チェックしているが、
        // ここでは「今回投入した10か国」が明示的に除外リストの主要な国と重複していないかを
        // 人が読める形で再確認する(取りまとめ役への報告事項: 一部の除外リスト項目は
        // 具体的な国コードが仕様書に無く、機械チェックの対象にできていない)。
        [Test]
        public void ImportedCountries_DoNotObviouslyOverlapWithExclusionExamples()
        {
            var table = LoadOrSkip();
            var excludedExamples = new[] { "tw", "kp", "ru", "ua", "tr", "eg", "kz", "cy", "il", "ps" };
            foreach (var id in excludedExamples)
            {
                Assert.IsFalse(table.Countries.Any(c => c.id == id), $"除外対象のはずの '{id}' が投入されている");
            }
        }
    }
}
