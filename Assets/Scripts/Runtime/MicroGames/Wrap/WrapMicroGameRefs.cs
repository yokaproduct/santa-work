using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames.Wrap
{
    /// <summary>`MicroGame_Wrap.prefab` の参照集約。`23_MicroGame_Wrap.md` §3。</summary>
    public class WrapMicroGameRefs : MonoBehaviour
    {
        [Header("ベルト")]
        [SerializeField] private RectTransform boxContainer; // BoxContainer。実行時に BoxItem が並ぶ親
        [SerializeField] private GameObject boxItemPrefab;   // Part_WrapBoxItem.prefab(実行時に Instantiate する)

        [Header("ループ進捗")]
        [SerializeField] private TMP_Text loopLabelText;     // 「ループ 2 / 3」
        [SerializeField] private WrapLoopDotRefs[] loopDots; // 常に3件。このループ内の3箱の進捗

        [Header("シュート")]
        [SerializeField] private WrapChuteRefs[] chutes;     // 常に3件。左・中・右の順

        [Header("スキップ口")]
        [SerializeField] private Button skipButton;

        public RectTransform BoxContainer => boxContainer;
        public GameObject BoxItemPrefab => boxItemPrefab;
        public TMP_Text LoopLabelText => loopLabelText;
        public WrapLoopDotRefs[] LoopDots => loopDots;
        public WrapChuteRefs[] Chutes => chutes;
        public Button SkipButton => skipButton;
    }
}
