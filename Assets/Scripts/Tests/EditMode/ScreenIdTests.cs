using NUnit.Framework;
using Santa.Core;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// `ScreenId`(★2026-09-17 `ModeSelect` を廃止・統合)の数値固定を検証する。
    /// `02_Screen_Title.md` §0.3: `ScreenId` は Prefab・シーンに整数で保存されているため、
    /// `ModeSelect` を欠番にしたあとも残る項目の数値がずれてはならない。
    /// </summary>
    public class ScreenIdTests
    {
        [Test]
        public void RemainingScreenIdValues_AreFixed_AfterModeSelectWasRemoved()
        {
            Assert.AreEqual(0, (int)ScreenId.Boot);
            Assert.AreEqual(1, (int)ScreenId.Title);
            Assert.AreEqual(3, (int)ScreenId.Settings);
            Assert.AreEqual(4, (int)ScreenId.Credits);
            Assert.AreEqual(5, (int)ScreenId.GamePlay);
            Assert.AreEqual(6, (int)ScreenId.Result);
        }

        [Test]
        public void Value2_IsNotAssignedToAnyScreenId()
        {
            // ★ModeSelectの旧値(2)がPrefab・シーンに残っていても、別の画面を指すことがないように、
            //   2は欠番のまま維持されていること。
            foreach (ScreenId id in System.Enum.GetValues(typeof(ScreenId)))
            {
                Assert.AreNotEqual(2, (int)id, $"{id} が旧ModeSelectの値(2)を使ってしまっている");
            }
        }
    }
}
