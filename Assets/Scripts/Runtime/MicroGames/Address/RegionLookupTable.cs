using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.MicroGames.Address
{
    /// <summary>
    /// 「正規化座標(UV, 0〜1) → 地域ID」の小さな対応表。取りまとめ役の実装依頼(2026-09-10)。
    ///
    /// `WorldMap.png`(1920×1080 = RGBA32で約8MB)のピクセルを直接保持し続けるのはCPUメモリ上
    /// 無視できないため、<see cref="WorldMapRegionSampler.Build"/> が一度だけ間引いて生成する
    /// この小さな配列(既定 480×270 = 1バイト×129,600 ≈ 126.6KB)だけをセッション中保持する。
    /// </summary>
    public sealed class RegionLookupTable
    {
        /// <summary>「どの地域でもない」(海・輪郭・中間色)を表す値。</summary>
        public const byte NoRegion = 255;

        public int Width { get; }
        public int Height { get; }

        /// <summary>インデックス → 地域ID。<see cref="_cellRegionIndex"/> の値はこの配列の添字。</summary>
        public IReadOnlyList<string> RegionIds { get; }

        // 添字 = y * Width + x。値は RegionIds のインデックス、または NoRegion。
        private readonly byte[] _cellRegionIndex;

        // 地域ごとの集計(重心計算・診断用)。RegionIds と同じ並び。
        private readonly long[] _cellCounts;
        private readonly double[] _sumU;
        private readonly double[] _sumV;

        internal RegionLookupTable(int width, int height, IReadOnlyList<string> regionIds, byte[] cellRegionIndex)
        {
            if (width <= 0 || height <= 0) throw new ArgumentException("width/height は正の値が必要です。");
            if (cellRegionIndex == null || cellRegionIndex.Length != width * height)
                throw new ArgumentException("cellRegionIndex のサイズが width*height と一致しません。");

            Width = width;
            Height = height;
            RegionIds = regionIds;
            _cellRegionIndex = cellRegionIndex;

            int regionCount = regionIds.Count;
            _cellCounts = new long[regionCount];
            _sumU = new double[regionCount];
            _sumV = new double[regionCount];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte idx = _cellRegionIndex[y * width + x];
                    if (idx == NoRegion) continue;

                    _cellCounts[idx]++;
                    // ★セルの中心を代表点とする(0.5刻み)。
                    _sumU[idx] += (x + 0.5) / width;
                    _sumV[idx] += (y + 0.5) / height;
                }
            }
        }

        /// <summary>正規化座標(左下原点。x/y とも0〜1)から地域IDを引く。海・輪郭・中間色は false を返す。</summary>
        public bool TryGetRegionId(Vector2 uv, out string regionId)
        {
            regionId = null;
            if (uv.x < 0f || uv.x >= 1f || uv.y < 0f || uv.y >= 1f) return false;

            int x = Mathf.Clamp((int)(uv.x * Width), 0, Width - 1);
            int y = Mathf.Clamp((int)(uv.y * Height), 0, Height - 1);
            byte idx = _cellRegionIndex[y * Width + x];
            if (idx == NoRegion) return false;

            regionId = RegionIds[idx];
            return true;
        }

        /// <summary>その地域に分類されたセル数(間引き後の個数。実ピクセル数ではない。診断・テスト用)。</summary>
        public int GetCellCount(string regionId)
        {
            int idx = IndexOf(regionId);
            return idx < 0 ? 0 : (int)_cellCounts[idx];
        }

        /// <summary>その地域に分類されたセルの重心(正規化UV。左下原点)。1件もなければ (0.5, 0.5) を返す。</summary>
        public Vector2 GetCentroidUV(string regionId)
        {
            int idx = IndexOf(regionId);
            if (idx < 0 || _cellCounts[idx] == 0) return new Vector2(0.5f, 0.5f);

            return new Vector2((float)(_sumU[idx] / _cellCounts[idx]), (float)(_sumV[idx] / _cellCounts[idx]));
        }

        private int IndexOf(string regionId)
        {
            for (int i = 0; i < RegionIds.Count; i++)
            {
                if (RegionIds[i] == regionId) return i;
            }
            return -1;
        }
    }
}
