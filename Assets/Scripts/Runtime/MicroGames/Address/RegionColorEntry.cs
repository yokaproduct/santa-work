using System;
using UnityEngine;

namespace Santa.MicroGames.Address
{
    /// <summary>
    /// 世界地図の1地域ぶんの「代表色」定義(色キー判定用)。
    /// 取りまとめ役の実装依頼(2026-09-10 M2 地図の色キー判定化)。
    ///
    /// `WorldMap.png` を実際にサンプリングして確定した値を Inspector で保持する
    /// (`WorldMapHitTester.regionColors`)。塗りの内部でも数値がわずかに揺れるため、
    /// 完全一致ではなく <see cref="WorldMapRegionSampler"/> が距離判定で使う。
    /// </summary>
    [Serializable]
    public struct RegionColorEntry
    {
        [Tooltip("asia / europe / africa / north_america / south_america / oceania のいずれか。")]
        public string regionId;

        [Tooltip("この地域の「塗り」の代表色(輪郭線の色ではない)。WorldMap.png から実際にサンプリングした値。")]
        public Color32 fillColor;
    }
}
