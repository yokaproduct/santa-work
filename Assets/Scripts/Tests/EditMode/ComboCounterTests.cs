using NUnit.Framework;
using Santa.Game;

namespace Santa.Tests.EditMode
{
    public class ComboCounterTests
    {
        [Test]
        public void Increment_AccumulatesAndTracksMax()
        {
            var combo = new ComboCounter();
            Assert.AreEqual(1, combo.Increment());
            Assert.AreEqual(2, combo.Increment());
            Assert.AreEqual(3, combo.Increment());
            Assert.AreEqual(3, combo.Max);
        }

        [Test]
        public void Reset_SetsCurrentToZero_ButKeepsMax()
        {
            var combo = new ComboCounter();
            combo.Increment();
            combo.Increment();
            combo.Reset();
            Assert.AreEqual(0, combo.Current);
            Assert.AreEqual(2, combo.Max, "誤答後もそれまでの最大コンボは保持されるべき(結果画面の「最高◯連続」用)");
        }

        [Test]
        public void IncrementAfterReset_StartsFromOneAgain()
        {
            var combo = new ComboCounter();
            combo.Increment();
            combo.Increment();
            combo.Reset();
            Assert.AreEqual(1, combo.Increment());
        }
    }
}
