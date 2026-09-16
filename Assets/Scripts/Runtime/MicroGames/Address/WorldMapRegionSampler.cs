using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.MicroGames.Address
{
    /// <summary>
    /// `WorldMap.png` のピクセルから <see cref="RegionLookupTable"/> を一度だけ焼き込むビルダー。
    /// 取りまとめ役の実装依頼(2026-09-10)。
    ///
    /// 各地域の代表色(塗り)との距離が最も近いものを選び、かつ閾値内に収まっている場合のみ
    /// その地域とみなす。閾値外(海・輪郭・中間色)は <see cref="RegionLookupTable.NoRegion"/> になる。
    ///
    /// ★間引いた解像度(既定480×270)でサンプリングすることで、1920×1080 RGBA32(約8MB)の
    /// フル解像度ピクセル配列を保持し続けずに済む。この関数はテクスチャの `GetPixels32()` を
    /// 1回だけ呼び、戻り値の小さな <see cref="RegionLookupTable"/> だけを呼び出し側が保持すればよい。
    /// </summary>
    public static class WorldMapRegionSampler
    {
        public static RegionLookupTable Build(
            Texture2D texture,
            IReadOnlyList<RegionColorEntry> regionColors,
            int lookupWidth,
            int lookupHeight,
            float matchThreshold)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (regionColors == null || regionColors.Count == 0)
                throw new ArgumentException("regionColors が空です。", nameof(regionColors));
            if (lookupWidth <= 0 || lookupHeight <= 0)
                throw new ArgumentException("lookupWidth/lookupHeight は正の値が必要です。");

            int texWidth = texture.width;
            int texHeight = texture.height;
            Color32[] pixels = texture.GetPixels32(); // ★フル解像度の読み取りはこの1回だけ。

            var regionIds = new string[regionColors.Count];
            for (int i = 0; i < regionColors.Count; i++) regionIds[i] = regionColors[i].regionId;

            var cellRegionIndex = new byte[lookupWidth * lookupHeight];
            float thresholdSq = matchThreshold * matchThreshold;

            for (int gy = 0; gy < lookupHeight; gy++)
            {
                // ★セル中心のUV(左下原点)をそのまま元テクスチャのピクセル座標へ最近傍で写像する
                // (バイリニア補間はしない。境界のにじみをこれ以上増やさないため)。
                int texY = Mathf.Clamp((int)(((gy + 0.5f) / lookupHeight) * texHeight), 0, texHeight - 1);

                for (int gx = 0; gx < lookupWidth; gx++)
                {
                    int texX = Mathf.Clamp((int)(((gx + 0.5f) / lookupWidth) * texWidth), 0, texWidth - 1);
                    Color32 sample = pixels[texY * texWidth + texX];

                    byte bestIndex = RegionLookupTable.NoRegion;
                    float bestDistSq = float.MaxValue;
                    for (int r = 0; r < regionColors.Count; r++)
                    {
                        float distSq = ColorDistanceSquared(sample, regionColors[r].fillColor);
                        if (distSq < bestDistSq)
                        {
                            bestDistSq = distSq;
                            bestIndex = (byte)r;
                        }
                    }

                    if (bestDistSq > thresholdSq)
                    {
                        bestIndex = RegionLookupTable.NoRegion;
                    }

                    cellRegionIndex[gy * lookupWidth + gx] = bestIndex;
                }
            }

            return new RegionLookupTable(lookupWidth, lookupHeight, regionIds, cellRegionIndex);
        }

        private static float ColorDistanceSquared(Color32 a, Color32 b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }
    }
}
