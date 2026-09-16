namespace Santa.Core
{
    /// <summary>
    /// BGM / SE の識別子。共通仕様 00_共通仕様.md §11。
    /// 文字列そのものをあちこちに書かず、ここを参照することでタイプミスを防ぐ。
    /// </summary>
    public static class AudioIds
    {
        public static class Bgm
        {
            public const string Title = "bgm_title";
            public const string GamePlay = "bgm_gameplay";
            public const string Result = "bgm_result";
        }

        public static class Se
        {
            public const string MicroGameStart = "se_microgame_start";

            /// <summary>★2026-09-15 新規。終了演出の開始(T1が0になった瞬間)。共通仕様 §11.2。</summary>
            public const string Finish = "se_finish";
            public const string Correct = "se_correct";
            public const string Wrong = "se_wrong";
            public const string TimeUp = "se_timeup";
            public const string Combo = "se_combo";
            public const string Tap = "se_tap";
            public const string Button = "se_button";
            public const string Screen = "se_screen";
            public const string Countdown = "se_countdown";
            public const string Start = "se_start";
            public const string Rank = "se_rank";
            public const string NewRecord = "se_newrecord";
            public const string Belt = "se_belt";
            public const string Skip = "se_skip";
            public const string Sled = "se_sled";
            public const string ItemOk = "se_item_ok";
            public const string Overload = "se_overload";
        }
    }
}
