namespace Santa.Game
{
    /// <summary>
    /// スコア計算。共通仕様 00_共通仕様.md §5.1 / §5.2。
    /// 1件あたりの得点 = 基本点(100) × コンボ倍率。倍率は「加算後」のコンボ数で引く。
    /// </summary>
    public static class ScoreCalculator
    {
        /// <summary>件が成立した直後(コンボを+1した後)の点を計算する。</summary>
        public static int ComputeGain(GameBalanceSettings balance, int comboAfterIncrement)
        {
            float multiplier = balance.GetComboMultiplier(comboAfterIncrement);
            return Mathf_RoundToInt(balance.BaseScore * multiplier);
        }

        // UnityEngine.Mathf に依存させないための最小実装(このクラスは純粋ロジックとして
        // UnityEngine非依存に保ちたいが、四捨五入だけ必要なため自前で用意する)。
        private static int Mathf_RoundToInt(float value)
        {
            return (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
        }
    }
}
