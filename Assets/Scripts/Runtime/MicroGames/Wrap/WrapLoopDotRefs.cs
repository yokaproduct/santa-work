using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames.Wrap
{
    /// <summary>
    /// ループ内3箱の進捗ドット1つぶんの参照集約。`23_MicroGame_Wrap.md` §4.5。
    ///
    /// 状態と見た目の対応:
    /// - 未処理: <see cref="FillImage"/> を薄い白(枠っぽく見せる)、<see cref="MissMark"/> 非表示
    /// - 正解  : <see cref="FillImage"/> を塗りつぶし色、<see cref="MissMark"/> 非表示
    /// - 誤答  : <see cref="FillImage"/> を灰色、<see cref="MissMark"/>(×)を表示
    /// </summary>
    public class WrapLoopDotRefs : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private GameObject missMark;

        public Image FillImage => fillImage;
        public GameObject MissMark => missMark;
    }
}
