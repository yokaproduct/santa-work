using System.Collections.Generic;

namespace Santa.Game
{
    /// <summary>
    /// セッション終了時に `Screen_Result` へ渡す結果。`10_Screen_GamePlay.md` §4.4 / §6.1(R4)。
    /// </summary>
    public readonly struct SessionResult
    {
        public GameModeDefinition Mode { get; }
        public int Score { get; }

        /// <summary>mode.EvaluateRank が false の場合は無視してよい(値は0)。</summary>
        public int RankIndex { get; }

        public int ClearedUnits { get; }
        public int MissedUnits { get; }
        public int MaxCombo { get; }
        public bool IsNewRecord { get; }

        /// <summary>キーは definition.id。</summary>
        public IReadOnlyDictionary<string, (int cleared, int missed)> PerMicroGame { get; }

        public SessionResult(
            GameModeDefinition mode,
            int score,
            int rankIndex,
            int clearedUnits,
            int missedUnits,
            int maxCombo,
            bool isNewRecord,
            IReadOnlyDictionary<string, (int cleared, int missed)> perMicroGame)
        {
            Mode = mode;
            Score = score;
            RankIndex = rankIndex;
            ClearedUnits = clearedUnits;
            MissedUnits = missedUnits;
            MaxCombo = maxCombo;
            IsNewRecord = isNewRecord;
            PerMicroGame = perMicroGame;
        }
    }
}
