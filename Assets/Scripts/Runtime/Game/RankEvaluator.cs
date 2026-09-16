namespace Santa.Game
{
    /// <summary>スコアからサンタランクを求める。共通仕様 00_共通仕様.md §5.5。</summary>
    public static class RankEvaluator
    {
        /// <summary>該当ランクのインデックス(0=見習い〜)を返す。ranks は空であってはならない。</summary>
        public static int Evaluate(RankTable rankTable, int score)
        {
            int bestIndex = 0;
            int bestThreshold = int.MinValue;

            var ranks = rankTable.Ranks;
            for (int i = 0; i < ranks.Count; i++)
            {
                if (score >= ranks[i].scoreThreshold && ranks[i].scoreThreshold > bestThreshold)
                {
                    bestThreshold = ranks[i].scoreThreshold;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }
    }
}
