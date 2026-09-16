using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames.Letter
{
    /// <summary>`MicroGame_Letter.prefab` の参照集約。`20_MicroGame_Letter.md` §2。</summary>
    public class LetterMicroGameRefs : MonoBehaviour
    {
        [SerializeField] private TMP_Text letterText;
        [SerializeField] private StockCardRefs[] stockCards; // 常に4枚(2列×2行)
        [SerializeField] private Image ruleLines; // ★2026-09-13追加。罫線(破線4本。IMG-M1-03)。素材が無い間は無効のまま

        public TMP_Text LetterText => letterText;
        public StockCardRefs[] StockCards => stockCards;
        public Image RuleLines => ruleLines;
    }
}
