namespace Santa.Game
{
    /// <summary>1ミニゲームの進行フェーズ。`10_Screen_GamePlay.md` §5(状態遷移図)。</summary>
    public enum GameSessionPhase
    {
        Idle,
        Countdown,
        SelectAndPrepare,
        Intro,
        Prompt,
        Play,
        Judge,
        Teardown,

        /// <summary>
        /// ★2026-09-15 新設。終了演出(1.5秒)。`Judge`/`Teardown` から `EndSession` の間に必ず通る
        /// (共通仕様 §3.9、`10_Screen_GamePlay.md` §13)。この間 T1 は停止済みで再稼働しない。
        /// </summary>
        Finish,

        EndSession,
    }
}
