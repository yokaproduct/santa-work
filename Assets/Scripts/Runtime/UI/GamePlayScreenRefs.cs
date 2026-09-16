using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>`Screen_GamePlay` の参照集約。`10_Screen_GamePlay.md` §2。</summary>
    public class GamePlayScreenRefs : ScreenRefsBase
    {
        [Header("この画面固有")]
        [SerializeField] private HudRefs hud;
        [SerializeField] private RectTransform microGameSlot; // ★差し替え領域
        [SerializeField] private FillBarRefs microGameTimerBar; // T2(12秒)。画面下部(2026-09-08 移動)
        [SerializeField] private UnitProgressDotsRefs unitProgressDots; // 画面下部(同上)
        [SerializeField] private TMP_Text promptText; // 業務提示の動詞1語
        [SerializeField] private Image promptBackdrop; // 業務提示中にContentRoot全面を覆う白背景(2026-09-09追加)

        [Header("★2026-09-15 追加(業務提示演出・終了演出。10_Screen_GamePlay.md §12/§13)")]
        [SerializeField] private PromptEffectPlayer promptEffectPlayer;
        [SerializeField] private FinishEffectPlayer finishEffectPlayer;

        public HudRefs Hud => hud;
        public RectTransform MicroGameSlot => microGameSlot;
        public FillBarRefs MicroGameTimerBar => microGameTimerBar;
        public UnitProgressDotsRefs UnitProgressDots => unitProgressDots;
        public TMP_Text PromptText => promptText;
        public Image PromptBackdrop => promptBackdrop;
        public PromptEffectPlayer PromptEffectPlayer => promptEffectPlayer;
        public FinishEffectPlayer FinishEffectPlayer => finishEffectPlayer;
    }
}
