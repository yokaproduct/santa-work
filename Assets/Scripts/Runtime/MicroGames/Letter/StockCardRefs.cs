using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames.Letter
{
    /// <summary>
    /// 在庫カード1枚の参照集約。`StockCard0`〜`StockCard3` それぞれに付ける。
    /// 実イラストが無いため、`debugLabel` に `item_id` を表示するプレースホルダ実装
    /// (ディレクター指示: 「どのカードを押したか区別できる状態でよい」)。
    /// ★2026-09-13 ディレクター決定: 2レイヤー着色方式(iconBase+iconLine)を廃止し、
    ///   `icon` 1枚に完成画像を差し込む方式に変更。画像が無い間は `baseColor` でのプレースホルダ表示を維持する。
    /// ★実素材が用意され次第、debugLabel は非表示にすること。
    /// </summary>
    public class StockCardRefs : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon; // 完成画像1枚(Image.color は画像がある間は白のまま。無い間はプレースホルダ色)
        [SerializeField] private TMP_Text debugLabel; // ★プレースホルダ。将来削除予定

        public Button Button => button;
        public Image Icon => icon;
        public TMP_Text DebugLabel => debugLabel;
    }
}
