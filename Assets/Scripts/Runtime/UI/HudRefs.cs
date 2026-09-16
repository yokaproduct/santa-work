using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// GamePlayのHUD内要素への参照だけを集約する。ロジックを持たない。
    /// `10_Screen_GamePlay.md` §2(2026-09-08 モックアップ反映版)。
    /// RectTransform には一切触れないこと(ディレクターの所有物)。
    /// </summary>
    public class HudRefs : MonoBehaviour
    {
        [SerializeField] private TMP_Text microGameNameText;
        [SerializeField] private TMP_Text unitCountValueText;
        [SerializeField] private CanvasGroup comboGroup; // ★表示切替は alpha。SetActive は使わない(§6.2)
        [SerializeField] private TMP_Text comboValueText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Image sessionTimeBarFill; // T1(90秒)。HUDの下辺=区切り線を兼ねる(§6.1.2)

        public TMP_Text MicroGameNameText => microGameNameText;
        public TMP_Text UnitCountValueText => unitCountValueText;
        public CanvasGroup ComboGroup => comboGroup;
        public TMP_Text ComboValueText => comboValueText;
        public Button PauseButton => pauseButton;
        public Image SessionTimeBarFill => sessionTimeBarFill;
    }
}
