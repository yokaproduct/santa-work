using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.Game
{
    /// <summary>
    /// サンタランクの閾値と称号。共通仕様 00_共通仕様.md §5.5。仮値。プレイテストで確定させる。
    /// 昇順(scoreThreshold の小さい順)に並べて保持する想定。
    /// </summary>
    [CreateAssetMenu(menuName = "Santa/Rank Table", fileName = "RankTable")]
    public class RankTable : ScriptableObject
    {
        [Serializable]
        public struct RankEntry
        {
            public string titleTextKey;
            public int scoreThreshold;
            public Sprite badgeIcon;
        }

        [SerializeField]
        private List<RankEntry> ranks = new List<RankEntry>
        {
            new RankEntry { titleTextKey = "rank.trainee", scoreThreshold = 0 },
            new RankEntry { titleTextKey = "rank.junior", scoreThreshold = 800 },
            new RankEntry { titleTextKey = "rank.veteran", scoreThreshold = 1800 },
            new RankEntry { titleTextKey = "rank.super", scoreThreshold = 3000 },
            new RankEntry { titleTextKey = "rank.master", scoreThreshold = 4200 },
        };

        public IReadOnlyList<RankEntry> Ranks => ranks;
    }
}
