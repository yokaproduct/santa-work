using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.MicroGames.Address
{
    /// <summary>M2(住所)の問題データ。`21_MicroGame_Address.md` §5.1。CSVインポート結果。</summary>
    [CreateAssetMenu(menuName = "Santa/MicroGames/Country Table", fileName = "CountryTable")]
    public class CountryTable : ScriptableObject
    {
        [SerializeField] private List<Country> countries = new List<Country>();

        public List<Country> Countries => countries;
    }

    [Serializable]
    public class Country
    {
        public string id;             // "fr"
        public string nameJa;         // "フランス"(外務省表記)
        public string nameEn;         // "France"
        public Sprite flagSprite;     // ★プロトタイプは未設定でよい(プレースホルダ)
        public string regionId;       // asia / europe / africa / north_america / south_america / oceania
        public float weight = 1f;     // 出題の重み。★MVPでは未使用(§8-6)
        public string reviewStatus;   // "draft" / "approved"
        public string addedVersion;

        /// <summary>共通仕様 §8-6 で定義された6地域の正規値。</summary>
        public static readonly string[] ValidRegionIds =
        {
            "asia", "europe", "africa", "north_america", "south_america", "oceania",
        };
    }
}
