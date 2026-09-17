using TMPro;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// `Overlay_Countdown` の参照集約。`30_Overlay_Countdown.md` §2.1/§3。
    /// `Blocker` / `CountRing` はコードから触らないため持たない。
    /// </summary>
    public class CountdownOverlayRefs : MonoBehaviour
    {
        [SerializeField] private RectTransform countAnim;
        [SerializeField] private CanvasGroup countAnimGroup;
        [SerializeField] private TMP_Text countNumberText;
        [SerializeField] private TMP_Text startText;

        public RectTransform CountAnim => countAnim;
        public CanvasGroup CountAnimGroup => countAnimGroup;
        public TMP_Text CountNumberText => countNumberText;
        public TMP_Text StartText => startText;
    }
}
