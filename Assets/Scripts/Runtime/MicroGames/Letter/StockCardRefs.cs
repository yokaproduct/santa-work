using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames.Letter
{
    /// <summary>
    /// 在庫カード1枚の参照集約。`StockCard0`〜`StockCard3` それぞれに付ける。
    /// ★2026-09-13 ディレクター決定: 2レイヤー着色方式(iconBase+iconLine)を廃止し、
    ///   `icon` 1枚に完成画像を差し込む方式に変更。画像が無い間は `baseColor` でのプレースホルダ表示を維持する。
    /// ★2026-09-17 ディレクター指示: 名称は表示しない方針のため、名称用のプレースホルダ表示
    ///   (旧 `debugLabel`)を廃止した。空いた領域は `icon` を赤枠いっぱいに広げるために使う
    ///   (`Image.preserveAspect = true` で引き伸ばさない)。
    /// </summary>
    public class StockCardRefs : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon; // 完成画像1枚(Image.color は画像がある間は白のまま。無い間はプレースホルダ色)

        public Button Button => button;
        public Image Icon => icon;
    }
}
