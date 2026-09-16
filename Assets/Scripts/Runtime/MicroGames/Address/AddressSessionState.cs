using System.Collections.Generic;
using Santa.MicroGames;

namespace Santa.MicroGames.Address
{
    /// <summary>
    /// M2のセッション永続状態(共通仕様 00_共通仕様.md §4.7 / `21_MicroGame_Address.md` §4.7)。
    ///
    /// `MicroGame_Address` のPrefabはミニゲーム終了のたびに `Destroy` されるため、
    /// 「地図の累積カウンタ」と「出題プールの消費位置」はPrefabの外(このクラス)に持つ必要がある。
    /// `MicroGameContext.GetOrCreateState` により、セッションを通じて同じインスタンスが返る。
    ///
    /// ★`Pool` を含めているのは `LetterSessionState` と同じ理由(`LetterSessionState.cs` のコメント参照):
    /// `ShuffledPoolCursor` はセッション単位で「1回シャッフルし、末尾で先頭へ戻る」状態を持つため、
    /// Prefab再生成のたびに作り直すと「同じセッション内で出題が偏らない」という共通仕様 §8.7 の要件が壊れる。
    /// </summary>
    public class AddressSessionState : IMicroGameSessionState
    {
        public ShuffledPoolCursor<Country> Pool;

        /// <summary>地域ID("asia" 等) → その地域へ届けた累積数。未登場の地域は0として扱う。</summary>
        public Dictionary<string, int> DeliveredPerRegion = new Dictionary<string, int>();

        /// <summary>
        /// 世界地図の「座標→地域ID」対応表(2026-09-10 実素材投入)。
        /// `WorldMap.png`(1920×1080 RGBA32で約8MB)のフル解像度ピクセルをセッション中保持し続けない
        /// ために、<see cref="WorldMapRegionSampler.Build"/> で一度だけ間引いて焼き込んだこの小さな表
        /// (既定480×270 ≈ 126.6KB)だけを持ち回る。<see cref="Pool"/> と同じ理由でここに置く:
        /// `MicroGame_Address` のPrefabはM2が終わるたびに破棄されるため、Prefab再生成のたびに
        /// フルサイズテクスチャを読み直すのを避ける。
        /// </summary>
        public RegionLookupTable MapLookup;
    }
}
