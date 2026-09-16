using System.Collections.Generic;

namespace Santa.Core
{
    /// <summary>
    /// セーブデータ(PlayerPrefs)への唯一の入口。共通仕様 00_共通仕様.md §10。
    /// 各画面は直接 PlayerPrefs を叩かず、必ずこれを経由する。
    /// </summary>
    public interface ISaveManager
    {
        int SaveVersion { get; }
        int HighScore { get; }
        int BestRankIndex { get; }
        int TotalUnits { get; }
        int TotalPlays { get; }
        bool FirstLaunchDone { get; set; }
        float BgmVolume { get; set; }
        float SeVolume { get; set; }

        /// <summary>""(端末追従) / "ja" / "en"。</summary>
        string Language { get; set; }

        (int cleared, int attempted) GetMicroGameStats(string microGameId);

        bool IsMicroIntroSeen(string microGameId);
        void SetMicroIntroSeen(string microGameId, bool seen);

        /// <summary>直近の出題ID(カンマ区切り)。MVPでは書き込みのみ / 参照しない(共通仕様 §8.7)。</summary>
        void SetRecentQuestions(string microGameId, string commaSeparatedIds);

        /// <summary>
        /// セッション終了時に1回だけ呼ぶ。ハイスコア・ランク・累計統計・種目別統計をまとめて更新し、
        /// PlayerPrefs.Save() を1回だけ呼ぶ(共通仕様 §10「保存タイミング」)。
        /// </summary>
        /// <returns>ハイスコアを更新したら true。</returns>
        bool CommitSessionResult(
            int score,
            int rankIndex,
            int clearedUnits,
            IReadOnlyDictionary<string, (int cleared, int missed)> perMicroGame);

        /// <summary>
        /// 設定画面の「データを けす」から呼ぶ。santa.* 接頭辞のキーを全削除する。
        /// キー一覧をハードコードしないため、種目別キーは catalog から動的に組み立てる
        /// (共通仕様 §10「実装ルール」)。
        /// </summary>
        void ResetAllData(Santa.MicroGames.MicroGameCatalog catalog);
    }
}
