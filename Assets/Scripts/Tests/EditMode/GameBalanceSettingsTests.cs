using NUnit.Framework;
using Santa.Game;
using UnityEngine;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// `GameBalanceSettings` の既定値が決定ログ §4-6(2026-09-15)の数字と一致するかを検証する。
    /// ★これはC#側のデフォルト値の検証であり、既存の `GameBalanceSettings.asset` の
    /// シリアライズ済みの値までは検証しない(アセットは developer が別途更新している)。
    /// </summary>
    public class GameBalanceSettingsTests
    {
        private static GameBalanceSettings CreateDefault() => ScriptableObject.CreateInstance<GameBalanceSettings>();

        [Test]
        public void PromptDuration_DefaultsTo1_5Seconds()
        {
            var balance = CreateDefault();
            Assert.AreEqual(1.5f, balance.PromptDuration, 0.001f,
                "業務提示の既定値が決定ログ §4-6 決-1(1.5秒)と一致しない");
        }

        [Test]
        public void FinishSequenceDuration_DefaultsTo1_5Seconds()
        {
            var balance = CreateDefault();
            Assert.AreEqual(1.5f, balance.FinishSequenceDuration, 0.001f,
                "終了演出の既定値が決定ログ §4-6 決-4(1.5秒)と一致しない");
        }

        [Test]
        public void MinRemainingTimeToStartNewMicroGame_DefaultsTo2_7Seconds()
        {
            var balance = CreateDefault();
            Assert.AreEqual(2.7f, balance.MinRemainingTimeToStartNewMicroGame, 0.001f,
                "新規ミニゲームを開始しない残り時間の既定値が決定ログ §4-6 演-6(2.7秒)と一致しない");
        }

        [Test]
        public void MinRemainingTimeToStartNewMicroGame_IsGreaterThanPromptDuration()
        {
            // §12.8-c: この条件が成り立つ限り [Prompt] 中に T1 が0になることは起きない。
            var balance = CreateDefault();
            Assert.Greater(balance.MinRemainingTimeToStartNewMicroGame, balance.PromptDuration);
        }

        [Test]
        public void BootFadeInSeconds_DefaultsTo2_0Seconds()
        {
            var balance = CreateDefault();
            Assert.AreEqual(2.0f, balance.BootFadeInSeconds, 0.001f,
                "起動時フェードインの既定値がディレクター指示(2026-09-19、約2秒)と一致しない");
        }
    }
}
