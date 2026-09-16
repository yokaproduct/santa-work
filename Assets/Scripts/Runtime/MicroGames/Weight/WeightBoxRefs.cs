using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames.Weight
{
    /// <summary>
    /// 箱1個ぶんの参照集約。`Box0`〜`Box5` それぞれに付ける。`22_MicroGame_Weight.md` §2 / §3.1。
    ///
    /// 実素材が無いため、`boxImage` は単色の矩形で仮置きしている。
    /// ★実素材が用意され次第、`boxImage.sprite` を差し替えるだけで済む(Simple型のためSprite未設定でも
    /// 描画は崩れないが、9-slice等に変える場合は決定ログ 実-5 の注意を確認すること)。
    /// </summary>
    public class WeightBoxRefs : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image boxImage;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private GameObject shadow; // 選択中に表示する影(BoxShadow)
        [SerializeField] private Button button;
        [SerializeField] private Image selectionHighlight; // ★2026-09-13追加。選択中のハイライト枠(IMG-M3-07)。素材が無い間は無効のまま

        public RectTransform RectTransform => rectTransform;
        public Image BoxImage => boxImage;
        public TMP_Text ValueText => valueText;
        public GameObject Shadow => shadow;
        public Button Button => button;
        public Image SelectionHighlight => selectionHighlight;
    }
}
