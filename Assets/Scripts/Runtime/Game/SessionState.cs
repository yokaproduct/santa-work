using System;
using System.Collections.Generic;
using Santa.MicroGames;

namespace Santa.Game
{
    /// <summary>
    /// 1セッション(1プレイ)の内部状態。`10_Screen_GamePlay.md` §6 / 共通仕様 §4.7。
    /// 「もう一回」では必ず新しいインスタンスを作る(持ち越さない。共通仕様 §2.3)。
    /// </summary>
    public class SessionState
    {
        /// <summary>T1の残り秒数。無制限モード(mode.IsEndless)では未使用。</summary>
        public float RemainingTime;

        /// <summary>経過秒数。無制限モードの表示用(`99_廃止_Screen_ModeSelect.md` §6.1 R3)。</summary>
        public float ElapsedTime;

        public int Score;
        public int ClearedUnits;
        public int MissedUnits;

        public readonly ComboCounter Combo = new ComboCounter();

        /// <summary>連続で同じ種目を出さないため、直前に出した種目を覚えておく(共通仕様 §3.6)。</summary>
        public MicroGameDefinition PreviousDefinition;

        /// <summary>種目別の (cleared, missed) 集計。キーは definition.id(共通仕様 §4.6 の必須要件)。</summary>
        public readonly Dictionary<string, (int cleared, int missed)> PerMicroGame =
            new Dictionary<string, (int cleared, int missed)>();

        /// <summary>
        /// 種目ごとのセッション永続状態(共通仕様 §4.7)。キーは definition.id。
        /// GameSessionController は中身を知らない。ミニゲーム側が初回アクセス時に生成して入れる。
        /// </summary>
        public readonly Dictionary<string, IMicroGameSessionState> MicroGameStates =
            new Dictionary<string, IMicroGameSessionState>();

        /// <summary>セッション専用の乱数(共通仕様 §3.6)。UnityEngine.Random を直接使わない。</summary>
        public readonly Random Rng;

        /// <summary>これまでに開始したミニゲームの本数(0始まりの次のインデックス)。</summary>
        public int MicroGamesPlayed;

        public SessionState(Random rng)
        {
            Rng = rng;
        }

        public void AddMicroGameResult(string microGameId, MicroGameSummary summary)
        {
            PerMicroGame.TryGetValue(microGameId, out var current);
            PerMicroGame[microGameId] = (current.cleared + summary.UnitsCleared, current.missed + summary.UnitsMissed);
        }
    }
}
