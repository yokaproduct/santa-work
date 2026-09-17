namespace Santa.Core
{
    /// <summary>
    /// 画面(Screen_*)の識別子。ScreenLayer には常に1つだけ存在する(共通仕様 §2.2)。
    /// 共通仕様 §16.3 の画面一覧(Screen_Records は MVP除外)。
    ///
    /// ★2026-09-17(決定ログ §4-10): `ModeSelect` は `Screen_Title` へ統合され廃止した。
    /// ただしこの enum は Prefab・シーンに整数で保存されている(`02_Screen_Title.md` §0.3)ため、
    /// 残す項目の数値は変えない。`ModeSelect` の値(2)は欠番のまま明示的に振り直している。
    /// </summary>
    public enum ScreenId
    {
        Boot = 0,
        Title = 1,

        // ModeSelect = 2, // ★廃止(2026-09-17)。Screen_Title に統合。欠番として値を空けておく。

        Settings = 3,
        Credits = 4,
        GamePlay = 5,
        Result = 6,
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
