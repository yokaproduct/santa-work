using System;
using Santa.MicroGames;
using UnityEngine;

namespace Santa.Game
{
    /// <summary>
    /// GameSessionController の契約。`Screen_GamePlay` 側(GamePlayScreenController)は
    /// これだけに依存する。共通仕様 §4.1 の責務分界そのもの。
    /// </summary>
    public interface IGameSessionService
    {
        SessionState State { get; }
        GameModeDefinition Mode { get; }
        GameSessionPhase Phase { get; }

        /// <summary>T2(ミニゲームタイマー)の残り秒数。Play中以外は定義値が不定なので表示側でフェーズを見て判断する。</summary>
        float MicroGameTimeRemaining { get; }

        MicroGameDefinition CurrentMicroGameDefinition { get; }

        /// <summary>直近に Finish() を呼んだ理由。デバッグ・テスト用の観測点。</summary>
        MicroGameFinishReason? LastMicroGameFinishReason { get; }

        /// <summary>[Play]開始時点の T2 の初期値。HUDのT2バーの正規化(残量/初期値)に使う。</summary>
        float CurrentMicroGamePlayDuration { get; }

        /// <summary>
        /// ★2026-09-15 追加。現在のフェーズ(主に Prompt / Finish)に入ってからの経過秒。
        /// ポーズ中は進まない。`PromptEffectPlayer` / `FinishEffectPlayer` はこれをサンプリングして
        /// タイムラインを再生する(`10_Screen_GamePlay.md` §12.4 / §13.4)。それ以外のフェーズでは 0。
        /// </summary>
        float PhaseElapsed { get; }

        /// <summary>業務提示フェーズの長さ(秒)。`GameBalanceSettings.PromptDuration`。</summary>
        float PromptDuration { get; }

        /// <summary>終了演出フェーズの長さ(秒)。`GameBalanceSettings.FinishSequenceDuration`。</summary>
        float FinishSequenceDuration { get; }

        /// <summary>直近に終了したセッションの結果。`Screen_Result` は SessionEnded を待たず、
        /// 自分の Start() でこれを読むだけでよい(画面遷移の前に発火するイベントを取りこぼさないため)。</summary>
        SessionResult? LastResult { get; }

        /// <summary>
        /// 現在ポーズ中か。★UIボタン側はこれを唯一の真実として扱うこと。
        /// バックグラウンド移行 / Editorでのフォーカス喪失(OnApplicationFocus(false))で
        /// 自動的にポーズされる経路があるため、ボタン側が独自に「押した/押していない」を
        /// ローカルで覚えておくと、自動ポーズと状態がずれて「ポーズボタンを1回押しても
        /// 再開しない」不具合になる(実際にこの不具合を作り込んだ。§報告参照)。
        /// </summary>
        bool IsPaused { get; }

        event Action<SessionResult> SessionEnded;
        event Action<MicroGameDefinition> MicroGamePrepared;
        event Action ScoreOrComboChanged;
        event Action<int> UnitClearedGained;
        event Action<GameSessionPhase> PhaseChanged;

        /// <summary>SetPaused の結果、実際にポーズ状態が変わったときに通知する。UIの表示同期に使う。</summary>
        event Action<bool> PausedChanged;

        /// <summary>GamePlayScreenController が MicroGameSlot を渡す。破棄時に null を渡して解除すること。</summary>
        void BindMicroGameSlot(RectTransform slot);

        void StartSession(GameModeDefinition mode, bool retry);
        void SetPaused(bool paused);

        /// <summary>Overlay_Pause の「タイトルへ」。スコア・統計を一切記録しない(共通仕様 §2.4)。</summary>
        void QuitWithoutRecording();

        /// <summary>Overlay_MicroGameIntro 側がタップを検知したら呼ぶ。</summary>
        void NotifyIntroDismissed();
    }
}
