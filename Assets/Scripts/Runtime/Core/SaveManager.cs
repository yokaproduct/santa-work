using System.Collections.Generic;
using Santa.MicroGames;
using UnityEngine;

namespace Santa.Core
{
    /// <summary>
    /// <see cref="ISaveManager"/> の PlayerPrefs 実装。共通仕様 00_共通仕様.md §10。
    /// 暗号化・改ざん対策は不要(オンラインランキングがないため)。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SaveManager : MonoBehaviour, ISaveManager
    {
        private const int CurrentSaveVersion = 1;

        private const string KeySaveVersion = "santa.saveVersion";
        private const string KeyHighScore = "santa.highScore";
        private const string KeyBestRankIndex = "santa.bestRankIndex";
        private const string KeyTotalUnits = "santa.totalUnits";
        private const string KeyTotalPlays = "santa.totalPlays";
        private const string KeyFirstLaunchDone = "santa.firstLaunchDone";
        private const string KeyVolumeBgm = "santa.volume.bgm";
        private const string KeyVolumeSe = "santa.volume.se";
        private const string KeyLanguage = "santa.language";

        // 種目別キーは definition.id から動的に組み立てる(種目追加時にコード変更不要にするため。§4.6 / §10)。
        private static string KeyMicroCleared(string id) => $"santa.micro.{id}.cleared";
        private static string KeyMicroAttempted(string id) => $"santa.micro.{id}.attempted";
        private static string KeyMicroIntroSeen(string id) => $"santa.microIntroSeen.{id}";
        private static string KeyRecentQuestions(string id) => $"santa.recentQuestions.{id}";

        // ResetAllData で消す「種目に依存しない」静的キーの一覧。
        // ★このリストに追加なしで種目別キーが増減できることが §4.6 の必須要件。
        private static readonly string[] StaticKeys =
        {
            KeySaveVersion, KeyHighScore, KeyBestRankIndex, KeyTotalUnits, KeyTotalPlays,
            KeyFirstLaunchDone, KeyVolumeBgm, KeyVolumeSe, KeyLanguage,
        };

        private void Awake()
        {
            if (!PlayerPrefs.HasKey(KeySaveVersion))
            {
                PlayerPrefs.SetInt(KeySaveVersion, CurrentSaveVersion);
                PlayerPrefs.Save();
            }

            ServiceLocator.Register<ISaveManager>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ISaveManager>();
        }

        public int SaveVersion => PlayerPrefs.GetInt(KeySaveVersion, CurrentSaveVersion);
        public int HighScore => PlayerPrefs.GetInt(KeyHighScore, 0);
        public int BestRankIndex => PlayerPrefs.GetInt(KeyBestRankIndex, 0);
        public int TotalUnits => PlayerPrefs.GetInt(KeyTotalUnits, 0);
        public int TotalPlays => PlayerPrefs.GetInt(KeyTotalPlays, 0);

        public bool FirstLaunchDone
        {
            get => PlayerPrefs.GetInt(KeyFirstLaunchDone, 0) != 0;
            set => PlayerPrefs.SetInt(KeyFirstLaunchDone, value ? 1 : 0);
        }

        public float BgmVolume
        {
            get => PlayerPrefs.GetFloat(KeyVolumeBgm, 1.0f);
            set => PlayerPrefs.SetFloat(KeyVolumeBgm, value);
        }

        public float SeVolume
        {
            get => PlayerPrefs.GetFloat(KeyVolumeSe, 1.0f);
            set => PlayerPrefs.SetFloat(KeyVolumeSe, value);
        }

        public string Language
        {
            get => PlayerPrefs.GetString(KeyLanguage, "");
            set => PlayerPrefs.SetString(KeyLanguage, value ?? "");
        }

        public (int cleared, int attempted) GetMicroGameStats(string microGameId)
        {
            int cleared = PlayerPrefs.GetInt(KeyMicroCleared(microGameId), 0);
            int attempted = PlayerPrefs.GetInt(KeyMicroAttempted(microGameId), 0);
            return (cleared, attempted);
        }

        public bool IsMicroIntroSeen(string microGameId)
        {
            return PlayerPrefs.GetInt(KeyMicroIntroSeen(microGameId), 0) != 0;
        }

        public void SetMicroIntroSeen(string microGameId, bool seen)
        {
            PlayerPrefs.SetInt(KeyMicroIntroSeen(microGameId), seen ? 1 : 0);
        }

        public void SetRecentQuestions(string microGameId, string commaSeparatedIds)
        {
            PlayerPrefs.SetString(KeyRecentQuestions(microGameId), commaSeparatedIds ?? "");
        }

        public bool CommitSessionResult(
            int score,
            int rankIndex,
            int clearedUnits,
            IReadOnlyDictionary<string, (int cleared, int missed)> perMicroGame)
        {
            bool isNewRecord = score > HighScore;
            if (isNewRecord)
            {
                PlayerPrefs.SetInt(KeyHighScore, score);
            }
            if (rankIndex > BestRankIndex)
            {
                PlayerPrefs.SetInt(KeyBestRankIndex, rankIndex);
            }

            PlayerPrefs.SetInt(KeyTotalUnits, TotalUnits + clearedUnits);
            PlayerPrefs.SetInt(KeyTotalPlays, TotalPlays + 1);

            if (perMicroGame != null)
            {
                foreach (var kv in perMicroGame)
                {
                    var (cleared, attempted) = GetMicroGameStats(kv.Key);
                    PlayerPrefs.SetInt(KeyMicroCleared(kv.Key), cleared + kv.Value.cleared);
                    // attempted = cleared + missed(時間切れの未回答は含めない。共通仕様 §6.1)
                    PlayerPrefs.SetInt(KeyMicroAttempted(kv.Key), attempted + kv.Value.cleared + kv.Value.missed);
                }
            }

            PlayerPrefs.Save();
            return isNewRecord;
        }

        public void ResetAllData(MicroGameCatalog catalog)
        {
            foreach (var key in StaticKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            if (catalog != null)
            {
                foreach (var def in catalog.Definitions)
                {
                    if (def == null) continue;
                    PlayerPrefs.DeleteKey(KeyMicroCleared(def.Id));
                    PlayerPrefs.DeleteKey(KeyMicroAttempted(def.Id));
                    PlayerPrefs.DeleteKey(KeyMicroIntroSeen(def.Id));
                    PlayerPrefs.DeleteKey(KeyRecentQuestions(def.Id));
                }
            }

            PlayerPrefs.SetInt(KeySaveVersion, CurrentSaveVersion);
            PlayerPrefs.Save();
        }
    }
}
