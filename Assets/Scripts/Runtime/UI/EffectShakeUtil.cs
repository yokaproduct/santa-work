using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// 業務提示・終了演出の「揺れ」(位置±px・回転±度、一定間隔で向きを変えつつ振幅が直線で減衰)を
    /// 計算する共通ヘルパー。`10_Screen_GamePlay.md` §12.2 / §13.2。
    ///
    /// ★`UnityEngine.Random` ではなくハッシュ関数で疑似乱数を作る(同じフェーズ経過時刻を
    /// 何度サンプリングしても同じ値になるようにするため。`PromptEffectPlayer`/`FinishEffectPlayer`は
    /// Update毎回タイムラインを最初から評価し直す設計であり、状態を持つ乱数だと呼び出し順に依存してしまう)。
    /// </summary>
    public static class EffectShakeUtil
    {
        public static Vector2 GetShakeOffset(
            float t, float startTime, float endTime, float amplitude, float intervalSeconds, int seedSalt)
        {
            if (amplitude <= 0f || t < startTime || t > endTime || endTime <= startTime) return Vector2.zero;

            float amplitudeRatio = 1f - Mathf.InverseLerp(startTime, endTime, t); // 直線で0へ減衰
            int step = Mathf.FloorToInt((t - startTime) / Mathf.Max(0.0001f, intervalSeconds));
            float rx = HashToSigned(step * 2 + seedSalt * 1000);
            float ry = HashToSigned(step * 2 + 1 + seedSalt * 1000);
            return new Vector2(rx, ry) * amplitude * amplitudeRatio;
        }

        public static float GetShakeRotation(
            float t, float startTime, float endTime, float amplitudeDeg, float intervalSeconds, int seedSalt)
        {
            if (amplitudeDeg <= 0f || t < startTime || t > endTime || endTime <= startTime) return 0f;

            float amplitudeRatio = 1f - Mathf.InverseLerp(startTime, endTime, t);
            int step = Mathf.FloorToInt((t - startTime) / Mathf.Max(0.0001f, intervalSeconds));
            float r = HashToSigned(step + seedSalt * 1000 + 500);
            return r * amplitudeDeg * amplitudeRatio;
        }

        /// <summary>整数を [-1, 1] の疑似乱数へ。</summary>
        private static float HashToSigned(int n)
        {
            unchecked
            {
                uint h = (uint)n;
                h = (h ^ 61u) ^ (h >> 16);
                h += h << 3;
                h ^= h >> 4;
                h *= 0x27d4eb2d;
                h ^= h >> 15;
                float unit = (h & 0xFFFFFF) / (float)0xFFFFFF; // 0..1
                return unit * 2f - 1f;
            }
        }
    }
}
