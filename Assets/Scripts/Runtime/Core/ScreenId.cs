namespace Santa.Core
{
    /// <summary>
    /// 画面(Screen_*)の識別子。ScreenLayer には常に1つだけ存在する(共通仕様 §2.2)。
    /// 共通仕様 §16.3 の画面一覧(全7画面。Screen_Records は MVP除外)。
    /// </summary>
    public enum ScreenId
    {
        Boot,
        Title,
        ModeSelect,
        Settings,
        Credits,
        GamePlay,
        Result,
    }

    /// <summary>
    /// オーバーレイ(Overlay_*)の識別子。OverlayLayer には 0〜1つだけ存在する(共通仕様 §2.2)。
    /// 優先順位: Pause > MicroGameIntro > Countdown。Confirm は優先順位の外(Pauseの上に重ねてよい唯一の例外)。
    /// </summary>
    public enum OverlayId
    {
        Countdown,
        Pause,
        MicroGameIntro,
        Confirm,
    }
}
