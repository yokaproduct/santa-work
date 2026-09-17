using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// `Screen_Title`(★2026-09-17 `Screen_ModeSelect` を統合したホーム画面)の参照集約。
    /// `02_Screen_Title.md` §3。ロゴ・背景・ラベル類はコードから触らないので持たない。
    /// </summary>
    public class TitleScreenRefs : ScreenRefsBase
    {
        [Header("この画面固有")]
        [SerializeField] private Button playButtonTime90;
        [SerializeField] private Button settingsButton;
        [SerializeField] private TMP_Text highScoreValueText;
        [SerializeField] private TMP_Text bestRankValueText;
        [SerializeField] private CanvasGroup firstTimeHint;
        [SerializeField] private CanvasGroup comingSoonPanel;
        [SerializeField] private CanvasGroup newContentBadge;
        [SerializeField] private TMP_Text modeTitleText;
        [SerializeField] private TMP_Text modeDescText;

        public Button PlayButtonTime90 => playButtonTime90;
        public Button SettingsButton => settingsButton;
        public TMP_Text HighScoreValueText => highScoreValueText;
        public TMP_Text BestRankValueText => bestRankValueText;
        public CanvasGroup FirstTimeHint => firstTimeHint;
        public CanvasGroup ComingSoonPanel => comingSoonPanel;
        public CanvasGroup NewContentBadge => newContentBadge;
        public TMP_Text ModeTitleText => modeTitleText;
        public TMP_Text ModeDescText => modeDescText;
    }
}
