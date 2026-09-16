namespace Santa.MicroGames
{
    /// <summary>
    /// ミニゲームが終了した理由。共通仕様 00_共通仕様.md §4.3。
    /// </summary>
    public enum MicroGameFinishReason
    {
        /// <summary>3件クリア(残り時間を待たずに終わる)。</summary>
        AllUnitsCleared,

        /// <summary>T2(12秒)が0になった。★失敗ではない。</summary>
        TimeUp,

        /// <summary>セッション90秒が尽きた。★失敗ではない。</summary>
        SessionEnded,
    }
}
