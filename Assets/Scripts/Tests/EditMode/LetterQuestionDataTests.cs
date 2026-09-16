using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Santa.MicroGames.Letter;
using UnityEditor;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// M1(手紙)の実データ(`Assets/Settings/Questions/LetterQuestionTable.asset`)を、
    /// `20_MicroGame_Letter.md` §5.3 のバリデーションでインポータとは独立に再検証する。
    /// インポータ自身にバグがあっても検出できるよう、あえて別経路でチェックする。
    /// </summary>
    public class LetterQuestionDataTests
    {
        private const string AssetPath = "Assets/Settings/Questions/LetterQuestionTable.asset";

        private static LetterQuestionTable LoadOrSkip()
        {
            var table = AssetDatabase.LoadAssetAtPath<LetterQuestionTable>(AssetPath);
            if (table == null || table.Questions.Count == 0)
            {
                Assert.Ignore($"{AssetPath} が空です。先に Tools/データ/問題データをインポート を実行してください。");
            }
            return table;
        }

        [Test]
        public void TenApprovedJapaneseQuestionsExist()
        {
            var table = LoadOrSkip();
            var ja = table.GetQuestionsForLanguage("ja");
            Assert.AreEqual(10, ja.Count, "プロトタイプのM1問題は10問のはず(共通仕様 §8.1)");
        }

        [Test]
        public void EveryLine_IsAtMost14Characters()
        {
            var table = LoadOrSkip();
            foreach (var q in table.Questions)
            {
                foreach (var line in new[] { q.line1, q.line2, q.line3 })
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    Assert.LessOrEqual(line.Length, 14, $"{q.id}: 1行が14文字を超えている(\"{line}\")");
                }
            }
        }

        [Test]
        public void TotalLength_IsAtMost35Characters()
        {
            var table = LoadOrSkip();
            foreach (var q in table.Questions)
            {
                int total = (q.line1?.Length ?? 0) + (q.line2?.Length ?? 0) + (q.line3?.Length ?? 0);
                Assert.LessOrEqual(total, 35, $"{q.id}: 合計文字数が35を超えている({total}文字)");
            }
        }

        [Test]
        public void AverageLength_IsWithinTargetRange()
        {
            var table = LoadOrSkip();
            var totals = table.Questions.Select(q =>
                (q.line1?.Length ?? 0) + (q.line2?.Length ?? 0) + (q.line3?.Length ?? 0));
            double average = totals.Average();
            // §5.3-9 は警告のみだが、テストとしては値を可視化する目的でここに置く。
            Assert.That(average, Is.InRange(20.0, 23.0),
                $"平均文字数が目標(20〜23)から外れている(実測 {average:F1})。仕様は警告扱いなので即エラーではないが要確認");
        }

        [Test]
        public void EveryQuestion_HasExactlyThreeDistractors_NotOverlappingCorrectItem()
        {
            var table = LoadOrSkip();
            foreach (var q in table.Questions)
            {
                Assert.AreEqual(3, q.distractorIds.Length, $"{q.id}: distractor_ids が3件ではない");
                Assert.IsFalse(q.distractorIds.Contains(q.correctItemId), $"{q.id}: 誤答に正解と同じIDが混入している");
            }
        }

        [Test]
        public void EveryQuestion_HasNonEmptyUniquenessNote()
        {
            var table = LoadOrSkip();
            foreach (var q in table.Questions)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(q.uniquenessNote), $"{q.id}: uniqueness_note が空");
            }
        }

        [Test]
        public void NoLine_ContainsFullWidthSpace()
        {
            var table = LoadOrSkip();
            foreach (var q in table.Questions)
            {
                foreach (var line in new[] { q.line1, q.line2, q.line3 })
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    Assert.IsFalse(line.Contains('　'), $"{q.id}: 全角スペースが含まれている(半角に統一すること)");
                }
            }
        }

        [Test]
        public void AllReferencedItemIds_ExistInStockItemMaster()
        {
            var table = LoadOrSkip();
            var knownIds = new HashSet<string>(table.StockItems.Select(s => s.id));
            foreach (var q in table.Questions)
            {
                Assert.IsTrue(knownIds.Contains(q.correctItemId), $"{q.id}: correct_item_id '{q.correctItemId}' がStockItemマスタに無い");
                foreach (var d in q.distractorIds)
                {
                    Assert.IsTrue(knownIds.Contains(d), $"{q.id}: distractor '{d}' がStockItemマスタに無い");
                }
            }
        }

        [Test]
        public void TemplateTypeDistribution_MatchesSpec_A4_B3_C3()
        {
            var table = LoadOrSkip();
            var counts = table.Questions.GroupBy(q => q.templateType).ToDictionary(g => g.Key, g => g.Count());
            int CountOf(string key) => counts.TryGetValue(key, out var v) ? v : 0;
            Assert.AreEqual(4, CountOf("A"), "型Aは4問のはず(§7)");
            Assert.AreEqual(3, CountOf("B"), "型Bは3問のはず(§7)");
            Assert.AreEqual(3, CountOf("C"), "型Cは3問のはず(§7)");
        }
    }
}
