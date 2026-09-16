using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames.Wrap
{
    /// <summary>
    /// `Part_WrapBoxItem.prefab` の参照集約。ベルト上を流れる箱1個ぶん。
    /// `23_MicroGame_Wrap.md` §3 / §5.3。
    ///
    /// ★形・模様・ゴミの画像素材はまだ無い(ディレクターが `Assets/Art/MicroGames/Wrap/` に用意する予定)。
    /// それまでは <see cref="Background"/> の色 + <see cref="DebugLabel"/> のテキストで代用する。
    /// 素材が揃ったら、このRefsの参照先(Source Image)を差し替えるだけで反映される構造にしてある。
    /// </summary>
    public class WrapBoxItemRefs : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image pattern; // ★色覚対策の模様(水玉/ストライプ/チェック)。形カテゴリのときは形シルエットの表示にも使う。素材が無いので既定で非表示
        [SerializeField] private TMP_Text debugLabel; // ★形・色のプレースホルダ表示。素材投入後も補助表示として残してよい
        [SerializeField] private GameObject trashMark; // ゴミの「大きな×」。isTrash のときだけ表示
        [SerializeField] private Image trashImage; // ★2026-09-13追加。ゴミ(つぶれた箱)の絵。素材が無い間は無効のまま(TrashMarkの×だけで代用)
        [SerializeField] private Image activeOutline; // ★2026-09-13追加。判定ゾーン先頭(アクティブ)の箱に付く光る輪郭。素材が無い間は無効のまま

        public Image Background => background;
        public Image Pattern => pattern;
        public TMP_Text DebugLabel => debugLabel;
        public GameObject TrashMark => trashMark;
        public Image TrashImage => trashImage;
        public Image ActiveOutline => activeOutline;
    }
}
