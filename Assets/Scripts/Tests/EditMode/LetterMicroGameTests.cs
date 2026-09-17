using NUnit.Framework;
using Santa.MicroGames.Letter;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// M1(手紙)の `MicroGame_Letter.prefab` に対するレイアウト系の検証。
    ///
    /// ★2026-09-17 ディレクター指示: 在庫カード(`StockCard0`〜`3`)の画像が引き伸ばされて
    /// 見えていた不具合を修正した。名称は表示しない方針のため名称用の領域(旧 `DebugLabel`)を
    /// 廃止し、赤枠の内側全体(`CardInner` と同じ矩形)をアイコンの表示領域にした。
    /// このテストはその結果(`preserveAspect` が有効・名称要素が存在しない)をPrefab資産に対して直接検証する。
    /// </summary>
    public class LetterMicroGameTests
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MicroGames/MicroGame_Letter.prefab";

        [Test]
        public void Prefab_StockCards_IconPreservesAspect_AndHasNoNameElement()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Assert.Ignore($"{PrefabPath} が見つかりません。");
                return;
            }

            var refs = prefab.GetComponent<LetterMicroGameRefs>();
            Assert.IsNotNull(refs, "LetterMicroGameRefs が付いているべき");
            Assert.IsNotNull(refs.StockCards, "stockCards が未設定");
            Assert.AreEqual(4, refs.StockCards.Length, "在庫カードは常に4枚のはず");

            foreach (var card in refs.StockCards)
            {
                Assert.IsNotNull(card, "StockCardRefs が未設定の要素がある");
                Assert.IsNotNull(card.Icon, $"{card.name}: icon が未設定");
                Assert.IsTrue(card.Icon.preserveAspect,
                    $"{card.name}: 画像が引き伸ばされないよう Image.preserveAspect を有効にするべき");

                Assert.IsNull(card.transform.Find("DebugLabel"),
                    $"{card.name}: 名称表示(DebugLabel)は廃止されているべき");
                Assert.AreEqual(0, card.GetComponentsInChildren<TMP_Text>(true).Length,
                    $"{card.name}: カード内にテキスト要素(名称)が残っている");
            }
        }

        [Test]
        public void Prefab_StockCards_IconRect_MatchesCardInner()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Assert.Ignore($"{PrefabPath} が見つかりません。");
                return;
            }

            var refs = prefab.GetComponent<LetterMicroGameRefs>();
            Assert.IsNotNull(refs);

            foreach (var card in refs.StockCards)
            {
                var cardInner = card.transform.Find("CardInner") as RectTransform;
                Assert.IsNotNull(cardInner, $"{card.name}: CardInner が見つからない");

                var iconRect = card.Icon.rectTransform;
                Assert.AreEqual(cardInner.anchorMin, iconRect.anchorMin, $"{card.name}: iconのanchorMinがCardInnerと一致しない");
                Assert.AreEqual(cardInner.anchorMax, iconRect.anchorMax, $"{card.name}: iconのanchorMaxがCardInnerと一致しない");
                Assert.AreEqual(cardInner.sizeDelta, iconRect.sizeDelta, $"{card.name}: iconが赤枠の内側いっぱいに広がっていない");
                Assert.AreEqual(cardInner.anchoredPosition, iconRect.anchoredPosition,
                    $"{card.name}: iconの位置がCardInnerとずれている");
            }
        }
    }
}
