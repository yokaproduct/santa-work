using Santa.MicroGames;

namespace Santa.MicroGames.Wrap
{
    /// <summary>
    /// M5のセッション永続状態(共通仕様 00_共通仕様.md §4.7 / §8.7)。
    ///
    /// `LetterSessionState`(M1)と同じ考え方: `ShuffledPoolCursor` は「1プレイの開始時に1回シャッフルし、
    /// 末尾に達したら先頭へ戻る」というセッション単位の状態を持つため、Prefabが破棄・再生成されるたびに
    /// シャッフルし直すと「同じセッション内でプリセットが早期に繰り返されない」という要件が壊れる。
    /// </summary>
    public class WrapSessionState : IMicroGameSessionState
    {
        public ShuffledPoolCursor<WrapPreset> Pool;
    }
}
