namespace Santa.Game
{
    /// <summary>
    /// コンボの管理。共通仕様 00_共通仕様.md §5.2 / §5.3。
    /// リセットされるのは誤答のときだけ(時間切れ・セッション終了・ミニゲーム切替・M3のオーバーでは継続)。
    /// </summary>
    public class ComboCounter
    {
        public int Current { get; private set; }
        public int Max { get; private set; }

        /// <summary>件が成立したときに呼ぶ。加算後のコンボ数を返す(倍率はこの値で引く)。</summary>
        public int Increment()
        {
            Current += 1;
            if (Current > Max) Max = Current;
            return Current;
        }

        /// <summary>誤答したときに呼ぶ。</summary>
        public void Reset()
        {
            Current = 0;
        }
    }
}
