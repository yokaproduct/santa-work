using System;
using System.Collections.Generic;
using System.Linq;

namespace Santa.MicroGames.Weight
{
    /// <summary>
    /// M3の問題生成器。`22_MicroGame_Weight.md` §5.2 の実装。
    ///
    /// バリデーションは2条件のみ(確定):
    /// 1. 目標値ちょうどになる組み合わせが、6個の箱の中にちょうど1通りだけ存在する(解の一意性)
    /// 2. 同じ数値の箱が2つ以上存在しない(重複禁止)
    ///
    /// 条件2は「相異なる値を選ぶ」という生成方法そのもので構成的に満たす(§5.2 のコメントどおり)。
    /// 条件1は 2^boxCount - 1 通りの部分集合を全列挙して確認する(boxCount=6なら63通り。計算量は軽い)。
    ///
    /// ★2026-09-16 ディレクター決定(テストプレイ用の暫定難易度): 目標値(target)は
    /// <see cref="WeightGeneratorSettings.MaxTargetValue"/>(既定20)を超えてはならない。
    /// 従来は6個の値をまず決めてから答えの部分集合をあとから選んでいたため、valueMax(既定35)が
    /// そのまま目標値の上限になってしまっていた。これを「先に答えの箱の値(合計がmaxTarget以下)を
    /// 決めてから、残りの箱(おとり)を埋める」方式に変えて、目標値の上限を構造的に保証する。
    /// また、答えの箱数は3個を超えないことも <see cref="MaxAllowedAnswerBoxCount"/> で構造的に保証する
    /// (settings側の設定値が誤って3を超えていても、ここで必ずクランプする)。
    /// </summary>
    public static class WeightQuestionGenerator
    {
        /// <summary>★2026-09-16: 「選択できる箱は最大3個」ルールに合わせ、答えの箱数もここで必ず3以下に抑える。</summary>
        private const int MaxAllowedAnswerBoxCount = 3;

        /// <summary>
        /// 1台ぶんの問題を生成する。<paramref name="settings"/>.MaxRetry 回まで試し、
        /// 条件を満たす組み合わせが見つからなければ FallbackPreset(無ければ Presets[0])を返す。
        /// ★無限ループにしない(必ず何かを返す)。
        /// </summary>
        public static WeightPreset Generate(WeightGeneratorSettings settings, Random rng)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int boxCount = settings.BoxCount;
            int range = settings.ValueMax - settings.ValueMin + 1;
            if (range < boxCount)
            {
                // valueMin/valueMax が boxCount 未満の値域では相異なる6個を選べない。無限ループになるため即フォールバックする。
                return ResolveFallback(settings);
            }

            int maxAnswerCount = Math.Max(1, Math.Min(Math.Min(settings.AnswerBoxCountMax, boxCount), MaxAllowedAnswerBoxCount));
            int minAnswerCount = Math.Max(1, Math.Min(settings.AnswerBoxCountMin, maxAnswerCount));
            int maxTarget = Math.Max(1, settings.MaxTargetValue);

            for (int retry = 0; retry < settings.MaxRetry; retry++)
            {
                int answerCount = rng.Next(minAnswerCount, maxAnswerCount + 1);

                int[] answerValues = TryPickAnswerValues(answerCount, settings.ValueMin, maxTarget, rng);
                if (answerValues == null) continue; // この試行では条件(合計<=maxTarget)を満たす組み合わせを作れなかった

                int target = answerValues.Sum();

                int[] values = TryFillRemainingValues(answerValues, boxCount, settings.ValueMin, settings.ValueMax, rng);
                if (values == null) continue;

                if (CountSubsetsSummingTo(values, target) == 1)
                {
                    return new WeightPreset
                    {
                        id = "generated",
                        boxValues = values,
                        target = target,
                        answerIndex = Enumerable.Range(0, answerValues.Length).ToArray(),
                    };
                }
            }

            return ResolveFallback(settings);
        }

        private static WeightPreset ResolveFallback(WeightGeneratorSettings settings)
        {
            if (settings.FallbackPreset != null)
            {
                return settings.FallbackPreset;
            }

            if (settings.Presets != null && settings.Presets.Count > 0)
            {
                return settings.Presets[0];
            }

            throw new InvalidOperationException(
                "[WeightQuestionGenerator] 生成に失敗し、FallbackPreset も Presets も設定されていません。");
        }

        /// <summary>
        /// 「答えの箱」ぶんの値を先に決める。<paramref name="count"/> 個の相異なる値(すべて
        /// <paramref name="min"/> 以上)を、合計が <paramref name="maxSum"/> 以下になるまで試行する。
        /// 見つからなければ null(呼び出し側は外側のretryへ回す)。
        /// </summary>
        private static int[] TryPickAnswerValues(int count, int min, int maxSum, Random rng)
        {
            if ((long)min * count > maxSum) return null; // 最小値だけを足しても超えるなら不可能
            if (maxSum - min + 1 < count) return null;   // 相異なる値を選ぶだけの値域が無い

            const int MaxAttempts = 500;
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var set = new HashSet<int>();
                while (set.Count < count)
                {
                    set.Add(rng.Next(min, maxSum + 1));
                }

                int sum = set.Sum();
                if (sum <= maxSum)
                {
                    return set.ToArray();
                }
            }

            return null;
        }

        /// <summary>
        /// 答えの箱(<paramref name="answerValues"/>)以外の残り(おとり)の箱を、
        /// <paramref name="min"/>〜<paramref name="max"/> の範囲から相異なる値で埋める。
        /// </summary>
        private static int[] TryFillRemainingValues(int[] answerValues, int boxCount, int min, int max, Random rng)
        {
            var used = new HashSet<int>(answerValues);
            var all = new List<int>(answerValues);

            const int MaxAttempts = 2000;
            int attempts = 0;
            while (all.Count < boxCount && attempts < MaxAttempts)
            {
                attempts++;
                int v = rng.Next(min, max + 1);
                if (used.Add(v))
                {
                    all.Add(v);
                }
            }

            return all.Count == boxCount ? all.ToArray() : null;
        }

        /// <summary>全 2^n - 1 通りの空でない部分集合のうち、合計が target になるものの数を数える。</summary>
        public static int CountSubsetsSummingTo(IReadOnlyList<int> values, int target)
        {
            int n = values.Count;
            int subsetCount = 1 << n;
            int matches = 0;
            for (int mask = 1; mask < subsetCount; mask++)
            {
                int sum = 0;
                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        sum += values[i];
                    }
                }
                if (sum == target)
                {
                    matches++;
                }
            }
            return matches;
        }
    }
}
