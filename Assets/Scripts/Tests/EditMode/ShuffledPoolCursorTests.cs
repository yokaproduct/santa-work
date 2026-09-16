using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Santa.MicroGames;

namespace Santa.Tests.EditMode
{
    public class ShuffledPoolCursorTests
    {
        [Test]
        public void Next_CyclesThroughAllItems_ThenWrapsToStart_WithoutReshuffling()
        {
            var source = Enumerable.Range(0, 10).ToList();
            var cursor = new ShuffledPoolCursor<int>(source, new Random(42));

            var firstPass = new List<int>();
            for (int i = 0; i < 10; i++) firstPass.Add(cursor.Next());

            // 先頭10要素はシャッフル後の全件と一致するはず(重複や欠落が無い)。
            CollectionAssert.AreEquivalent(source, firstPass);

            var secondPass = new List<int>();
            for (int i = 0; i < 10; i++) secondPass.Add(cursor.Next());

            // 共通仕様 §8.7: 末尾到達後は「先頭に戻る」だけで再シャッフルしない。
            CollectionAssert.AreEqual(firstPass, secondPass, "末尾到達後に再シャッフルされている(仕様は『先頭に戻る』のみ)");
        }

        [Test]
        public void SameSeed_ProducesSameOrder()
        {
            var source = Enumerable.Range(0, 20).ToList();
            var cursorA = new ShuffledPoolCursor<int>(source, new Random(123));
            var cursorB = new ShuffledPoolCursor<int>(source, new Random(123));

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(cursorA.Next(), cursorB.Next(), "同じシードなのに出題順が一致しない(デバッグ用シード固定の要件)");
            }
        }

        [Test]
        public void EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => new ShuffledPoolCursor<int>(new List<int>(), new Random(1)));
        }
    }
}
