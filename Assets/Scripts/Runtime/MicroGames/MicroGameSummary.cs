namespace Santa.MicroGames
{
    /// <summary>
    /// 1ミニゲームぶんの結果サマリ。<see cref="MicroGameBase.Finish"/> の後、
    /// 共通側(GameSessionController)が <see cref="MicroGameBase.GetSummary"/> で1回だけ読む。
    /// 共通仕様 00_共通仕様.md §4.3。
    /// </summary>
    public readonly struct MicroGameSummary
    {
        /// <summary>成立した件数(0〜requiredUnits。通常は0〜3)。</summary>
        public int UnitsCleared { get; }

        /// <summary>誤答で失われた件数。</summary>
        public int UnitsMissed { get; }

        /// <summary>参考値。種目内部の問数(統計・チューニング用)。HUD・スコアには使わない。</summary>
        public int QuestionsAnswered { get; }

        public MicroGameSummary(int unitsCleared, int unitsMissed, int questionsAnswered)
        {
            UnitsCleared = unitsCleared;
            UnitsMissed = unitsMissed;
            QuestionsAnswered = questionsAnswered;
        }
    }
}
