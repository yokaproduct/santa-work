using System;

namespace Santa.Game
{
    /// <summary>
    /// コンボ倍率テーブルの1段。共通仕様 00_共通仕様.md §5.2。
    /// minCombo の降順に探索して最初に一致したものを使う。4段固定でハードコードしない。
    /// </summary>
    [Serializable]
    public struct ComboTier
    {
        public int minCombo;
        public float multiplier;
    }
}
