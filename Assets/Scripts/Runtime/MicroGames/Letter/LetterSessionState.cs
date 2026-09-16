using Santa.MicroGames;

namespace Santa.MicroGames.Letter
{
    /// <summary>
    /// M1のセッション永続状態(共通仕様 §4.7 / §8.7)。
    ///
    /// ★共通仕様 §4.7 は「M2以外にこの仕組みを使う種目は現状ない」としていたが、これは誤り。
    /// `ShuffledPoolCursor`(§8.7)は「1プレイの開始時に1回シャッフルし、末尾に達したら先頭へ戻る」
    /// という**セッション単位**の状態を持つため、Prefabが破棄・再生成されるたびにシャッフルし直すと
    /// 「同じセッション内で問題が繰り返されない」という要件が壊れる。
    /// M1(本実装)・M3・M5も本来この仕組みを使うべきであり、取りまとめ役へ報告する
    /// (実装時に見つけた仕様の穴)。
    /// </summary>
    public class LetterSessionState : IMicroGameSessionState
    {
        public ShuffledPoolCursor<LetterQuestion> Pool;
    }
}
