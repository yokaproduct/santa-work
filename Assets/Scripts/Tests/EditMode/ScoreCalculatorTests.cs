using NUnit.Framework;
using Santa.Game;
using UnityEngine;

namespace Santa.Tests.EditMode
{
    public class ScoreCalculatorTests
    {
        private static GameBalanceSettings CreateDefaultBalance()
        {
            return ScriptableObject.CreateInstance<GameBalanceSettings>();
        }

        [TestCase(1, 100)]
        [TestCase(2, 100)]
        [TestCase(3, 120)]
        [TestCase(5, 120)]
        [TestCase(6, 150)]
        [TestCase(9, 150)]
        [TestCase(10, 200)]
        [TestCase(25, 200)]
        public void ComputeGain_MatchesComboTierTable(int combo, int expectedGain)
        {
            var balance = CreateDefaultBalance();
            int gain = ScoreCalculator.ComputeGain(balance, combo);
            Assert.AreEqual(expectedGain, gain, $"combo={combo} の得点が共通仕様 §5.2 の表と一致しない");
        }
    }
}
