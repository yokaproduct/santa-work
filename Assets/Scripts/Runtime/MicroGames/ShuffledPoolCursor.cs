using System;
using System.Collections.Generic;

namespace Santa.MicroGames
{
    /// <summary>
    /// 出題プールの共通管理クラス。共通仕様 00_共通仕様.md §8.7。
    /// M1・M2・M3・M5 のすべてがこれを使う(M3・M5は生成プリセットのプールとして使う)。
    ///
    /// ルール(確定):
    /// 1プレイの開始時に1回シャッフルし、先頭から順に消費する。末尾に達したら先頭に戻って繰り返す。
    /// 末尾到達後の再シャッフルはしない。セッションをまたいでは保持しない
    /// (「もう一回」で新しいインスタンスを作ること)。
    /// </summary>
    public class ShuffledPoolCursor<T>
    {
        private readonly List<T> _items;
        private int _cursor;

        public ShuffledPoolCursor(IEnumerable<T> source, Random rng)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            _items = new List<T>(source);
            if (_items.Count == 0)
            {
                throw new InvalidOperationException("ShuffledPoolCursor: プールが空です。問題データを確認してください。");
            }

            Shuffle(_items, rng);
            _cursor = 0;
        }

        public int Count => _items.Count;

        /// <summary>先頭から順に1件取り出す。末尾に達したら先頭に戻る(再シャッフルはしない)。</summary>
        public T Next()
        {
            var item = _items[_cursor];
            _cursor = (_cursor + 1) % _items.Count;
            return item;
        }

        private static void Shuffle(List<T> list, Random rng)
        {
            // Fisher-Yates
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
