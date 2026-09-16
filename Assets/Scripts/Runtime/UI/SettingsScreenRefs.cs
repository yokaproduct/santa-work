using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// `Screen_Settings.prefab` の参照集約。`04_Screen_Settings.md` §2。
    ///
    /// ★2026-09-16 ディレクター指示による仮置きの最小構成。
    /// BGM音量(S1)・SE音量(S2)・言語切替(S3・仮置き)・もどるボタンのみを持つ。
    /// プライバシー・利用規約・クレジット・データ削除等の行(S4〜S10)は今回は作らない
    /// (未確定事項が多く、URL等の情報待ちのため。§7の未確定事項を参照)。
    /// もどるボタンは `HeaderRoot` の `BackButton` が `ScreenNavButton` で完結するため、
    /// このRefsからの参照は持たない。
    /// </summary>
    public class SettingsScreenRefs : ScreenRefsBase
    {
        [Header("S1 BGM音量 / S2 SE音量")]
        [SerializeField] private SettingsSliderRowRefs bgmRow;
        [SerializeField] private SettingsSliderRowRefs seRow;

        [Header("S3 言語切替(仮置き)")]
        [SerializeField] private SettingsButtonRowRefs languageRow;

        public SettingsSliderRowRefs BgmRow => bgmRow;
        public SettingsSliderRowRefs SeRow => seRow;
        public SettingsButtonRowRefs LanguageRow => languageRow;
    }
}
