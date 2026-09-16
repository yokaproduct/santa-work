using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Santa.MicroGames.Address;
using Santa.MicroGames.Letter;
using UnityEditor;
using UnityEngine;

namespace Santa.EditorTools
{
    /// <summary>
    /// docs/data/*.csv → Assets/Settings/Questions/*.asset のインポータ。共通仕様 00_共通仕様.md §8.2。
    ///
    /// 【インポータの実装規約(共通仕様 §8.2 を厳守)】
    /// 1. 既存アセットを削除して作り直さない。既存アセットの中身を上書き更新する(GUIDを保つ)。
    /// 2. review_status が approved の行だけをビルドに含める。draft はスキップし、件数をログに出す。
    /// 3. Sprite参照の解決漏れを検出し、警告として表示する(エラーにはしない。プレースホルダで進める方針のため)。
    /// 4. インポート結果のサマリ(件数・スキップ数・エラー)を必ずログに出す。
    ///
    /// これは継続的に使うツールなので Editor/Tools/ に置く(Editor/Scaffold/ ではない)。
    /// </summary>
    public static class QuestionDataImporter
    {
        private const string LettersCsvPath = "docs/data/letters.csv";
        private const string CountriesCsvPath = "docs/data/countries.csv";
        private const string LetterTablePath = "Assets/Settings/Questions/LetterQuestionTable.asset";
        private const string CountryTablePath = "Assets/Settings/Questions/CountryTable.asset";

        // 教育漢字1026字の一覧は未整備(共通仕様 §9.3)。誤った・不完全なリストを埋め込むと
        // 「チェックを通ったのに実は範囲外」という事故のほうが危険なため、あえて実装しない。
        // 検査したい場合は Assets/Settings/EducationalKanji1026.asset のようなデータを用意し、
        // ここに読み込み処理を追加すること。
        private const bool KanjiRangeCheckImplemented = false;

        [MenuItem("Tools/データ/問題データをインポート")]
        public static void ImportAll()
        {
            ImportLetters();
            ImportCountries();
        }

        [MenuItem("Tools/データ/M1 手紙データをインポート")]
        public static void ImportLetters()
        {
            if (!File.Exists(LettersCsvPath))
            {
                Debug.LogError($"[QuestionDataImporter] CSVが見つかりません: {LettersCsvPath}");
                return;
            }

            var rows = CsvUtil.Parse(File.ReadAllText(LettersCsvPath));
            if (rows.Count == 0)
            {
                Debug.LogError("[QuestionDataImporter] letters.csv が空です。");
                return;
            }

            var header = rows[0];
            int idxId = Array.IndexOf(header, "id");
            int idxLang = Array.IndexOf(header, "lang");
            int idxType = Array.IndexOf(header, "template_type");
            int idxLine1 = Array.IndexOf(header, "line1");
            int idxLine2 = Array.IndexOf(header, "line2");
            int idxLine3 = Array.IndexOf(header, "line3");
            int idxCorrect = Array.IndexOf(header, "correct_item_id");
            int idxDistractors = Array.IndexOf(header, "distractor_ids");
            int idxNote = Array.IndexOf(header, "uniqueness_note");
            int idxStatus = Array.IndexOf(header, "review_status");
            int idxVersion = Array.IndexOf(header, "added_version");

            var approved = new List<LetterQuestion>();
            var errors = new List<string>();
            int draftSkipped = 0;
            int totalCharCount = 0;

            for (int r = 1; r < rows.Count; r++)
            {
                var cells = rows[r];
                if (cells.Length == 1 && string.IsNullOrWhiteSpace(cells[0])) continue; // 空行

                string id = Get(cells, idxId);
                string status = Get(cells, idxStatus);

                if (status != "approved")
                {
                    draftSkipped++;
                    continue;
                }

                var q = new LetterQuestion
                {
                    id = id,
                    lang = Get(cells, idxLang),
                    templateType = Get(cells, idxType),
                    line1 = Get(cells, idxLine1),
                    line2 = Get(cells, idxLine2),
                    line3 = Get(cells, idxLine3),
                    correctItemId = Get(cells, idxCorrect),
                    distractorIds = Get(cells, idxDistractors).Split('|'),
                    uniquenessNote = Get(cells, idxNote),
                    reviewStatus = status,
                    addedVersion = Get(cells, idxVersion),
                };

                // --- バリデーション(20_MicroGame_Letter.md §5.3) ---
                var lines = new[] { q.line1, q.line2, q.line3 };
                int lineCountUsed = lines.Count(l => !string.IsNullOrEmpty(l));
                int total = lines.Sum(l => l?.Length ?? 0);

                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line) && line.Length > 14)
                    {
                        errors.Add($"{id}: 1行が14文字を超えています(\"{line}\" = {line.Length}文字)");
                    }
                    if (line != null && line.Contains('　'))
                    {
                        errors.Add($"{id}: 全角スペースを含んでいます(半角スペースに統一すること)");
                    }
                }
                if (total > 35)
                {
                    errors.Add($"{id}: 合計文字数が35を超えています({total}文字)");
                }
                if (lineCountUsed > 3)
                {
                    errors.Add($"{id}: 行数が3を超えています");
                }
                if (q.distractorIds.Length != 3)
                {
                    errors.Add($"{id}: distractor_ids が3件ではありません({q.distractorIds.Length}件)");
                }
                else if (q.distractorIds.Contains(q.correctItemId))
                {
                    errors.Add($"{id}: distractor_ids に correct_item_id と同じものが含まれています");
                }
                if (string.IsNullOrWhiteSpace(q.uniquenessNote))
                {
                    errors.Add($"{id}: uniqueness_note が空です(approvedにできません)");
                }

                totalCharCount += total;
                approved.Add(q);
            }

            if (errors.Count > 0)
            {
                Debug.LogError($"[QuestionDataImporter] M1: {errors.Count}件のバリデーションエラーがあります。" +
                                "インポートは中止しました。\n" + string.Join("\n", errors));
                return;
            }

            // StockItem マスタは別CSVを持たない(共通仕様に列定義が無いため)。
            // 参照されている item_id を集めて、未知のものはプレースホルダとして自動登録する。
            var table = LoadOrCreate<LetterQuestionTable>(LetterTablePath);
            var existingItemIds = new HashSet<string>(table.StockItems.Select(s => s.id));
            var referencedIds = approved
                .SelectMany(q => new[] { q.correctItemId }.Concat(q.distractorIds))
                .Distinct();

            int newItems = 0;
            foreach (var itemId in referencedIds)
            {
                if (existingItemIds.Contains(itemId)) continue;
                table.StockItems.Add(new StockItem
                {
                    id = itemId,
                    nameKeyJa = $"item.{itemId}.ja",
                    nameKeyEn = $"item.{itemId}.en",
                    baseColor = GuessColorFromId(itemId),
                });
                existingItemIds.Add(itemId);
                newItems++;
            }

            table.Questions.Clear();
            table.Questions.AddRange(approved);

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            double avgLength = approved.Count > 0 ? (double)totalCharCount / approved.Count : 0;
            Debug.Log($"[QuestionDataImporter] M1 手紙データ: {approved.Count}件を取り込み / draft {draftSkipped}件スキップ / " +
                      $"StockItem新規{newItems}件(プレースホルダ) / 平均文字数 {avgLength:F1}" +
                      (avgLength < 20 || avgLength > 23 ? "(★20〜23の目安から外れています)" : "") +
                      (KanjiRangeCheckImplemented ? "" : " / ★教育漢字1026字の範囲チェックは未実装(要データ整備)"));
        }

        [MenuItem("Tools/データ/M2 住所データをインポート")]
        public static void ImportCountries()
        {
            if (!File.Exists(CountriesCsvPath))
            {
                Debug.LogError($"[QuestionDataImporter] CSVが見つかりません: {CountriesCsvPath}");
                return;
            }

            var rows = CsvUtil.Parse(File.ReadAllText(CountriesCsvPath));
            if (rows.Count == 0)
            {
                Debug.LogError("[QuestionDataImporter] countries.csv が空です。");
                return;
            }

            var header = rows[0];
            int idxId = Array.IndexOf(header, "id");
            int idxNameJa = Array.IndexOf(header, "name_ja");
            int idxNameEn = Array.IndexOf(header, "name_en");
            int idxFlag = Array.IndexOf(header, "flag_sprite_id");
            int idxRegion = Array.IndexOf(header, "region_id");
            int idxWeight = Array.IndexOf(header, "weight");
            int idxStatus = Array.IndexOf(header, "review_status");
            int idxVersion = Array.IndexOf(header, "added_version");

            // §6.2 の除外国ブラックリスト。ISOコードが明確なものだけを機械チェック対象にする。
            // ★「旧ユーゴ諸国」「英仏等の海外領土」は具体的な国コードが仕様書に明記されておらず、
            //   広く解釈するとクロアチア・スロベニアのような無関係な独立国まで誤って弾いてしまう
            //   おそれがあるため、意図的にブラックリストへ含めていない(取りまとめ役への報告事項)。
            var blacklist = new HashSet<string>(new[]
            {
                "tw", "ps", "il", // 主権・承認の争い(台湾/パレスチナ・イスラエル)
                "xk", "cy-north", "os", "ge-ab", "md-tr", "so-land", // コソボ等(簡易コード。実データにはほぼ出現しない)
                "ru", "ua", "eh", // 領土問題
                "kp", // 国旗の規制
                "tr", "eg", "kz", "ge", "am", "az", "cy", // 2州にまたがる
                "hk", "mo", "gl", // 「国」でない
                "va", "mc", "sm", "li", // 知名度が不安定
                "mm", "ir", "iq", "sy", "af", // 保留
            });

            var approved = new List<Country>();
            var errors = new List<string>();
            var warnings = new List<string>();
            int draftSkipped = 0;
            var seenIds = new HashSet<string>();
            var seenRegions = new HashSet<string>();

            for (int r = 1; r < rows.Count; r++)
            {
                var cells = rows[r];
                if (cells.Length == 1 && string.IsNullOrWhiteSpace(cells[0])) continue;

                string id = Get(cells, idxId);
                string status = Get(cells, idxStatus);

                if (status != "approved")
                {
                    draftSkipped++;
                    continue;
                }

                var c = new Country
                {
                    id = id,
                    nameJa = Get(cells, idxNameJa),
                    nameEn = Get(cells, idxNameEn),
                    regionId = Get(cells, idxRegion),
                    reviewStatus = status,
                    addedVersion = Get(cells, idxVersion),
                };
                float.TryParse(Get(cells, idxWeight), out var weight);
                c.weight = weight <= 0 ? 1f : weight;

                string flagId = Get(cells, idxFlag);
                if (string.IsNullOrEmpty(flagId))
                {
                    warnings.Add($"{id}: flag_sprite_id が空です(プレースホルダで進めます)");
                }
                // ★Sprite本体はまだアセットとして存在しない(素材待ち)。ここでは解決を試みず、
                //   プレースホルダのまま warning にとどめる(共通仕様 §8.4)。

                if (!Country.ValidRegionIds.Contains(c.regionId))
                {
                    errors.Add($"{id}: region_id が不正です({c.regionId})");
                }
                if (blacklist.Contains(id))
                {
                    errors.Add($"{id}: 除外国リストに該当します(§6.2)");
                }
                if (string.IsNullOrWhiteSpace(c.nameJa) || string.IsNullOrWhiteSpace(c.nameEn))
                {
                    errors.Add($"{id}: name_ja / name_en のいずれかが空です");
                }
                if (!seenIds.Add(id))
                {
                    errors.Add($"{id}: id が重複しています");
                }

                seenRegions.Add(c.regionId);
                approved.Add(c);
            }

            foreach (var region in Country.ValidRegionIds)
            {
                if (!seenRegions.Contains(region))
                {
                    errors.Add($"6地域のうち '{region}' に該当する国が1つもありません(§5.3-5。エラー扱い)");
                }
            }

            if (errors.Count > 0)
            {
                Debug.LogError($"[QuestionDataImporter] M2: {errors.Count}件のバリデーションエラーがあります。" +
                                "インポートは中止しました。\n" + string.Join("\n", errors));
                return;
            }

            var table = LoadOrCreate<CountryTable>(CountryTablePath);
            table.Countries.Clear();
            table.Countries.AddRange(approved);

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            Debug.Log($"[QuestionDataImporter] M2 住所データ: {approved.Count}件を取り込み / draft {draftSkipped}件スキップ / " +
                      $"6地域すべて充足を確認 / 警告{warnings.Count}件" +
                      (warnings.Count > 0 ? "\n" + string.Join("\n", warnings) : ""));
        }

        private static string Get(string[] cells, int index)
        {
            if (index < 0 || index >= cells.Length) return "";
            return cells[index]?.Trim() ?? "";
        }

        private static Color GuessColorFromId(string itemId)
        {
            if (itemId.Contains("red")) return Color.red;
            if (itemId.Contains("blue")) return new Color(0.2f, 0.4f, 0.9f);
            if (itemId.Contains("yellow")) return Color.yellow;
            if (itemId.Contains("green")) return Color.green;
            if (itemId.Contains("brown")) return new Color(0.5f, 0.3f, 0.1f);
            return Color.white;
        }

        /// <summary>
        /// 既存アセットを削除して作り直さない(GUIDを保つ)。無ければ初回のみ新規作成する。
        /// </summary>
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var dir = Path.GetDirectoryName(path)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                CreateFolderRecursive(dir);
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[QuestionDataImporter] 新規アセットを作成しました: {path}");
            return asset;
        }

        private static void CreateFolderRecursive(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
