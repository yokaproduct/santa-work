using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Santa.EditorTools
{
    /// <summary>
    /// `docs/data/credits.csv`(唯一の正。共通仕様/`05_Screen_Credits.md` §4.1)から
    /// `docs/licenses.md` を生成する。手で書かない(§CR-2、二重管理を構造的に防ぐ)。
    /// 継続的に使うツールなので Editor/Tools/ に置く。
    ///
    /// ★Screen_Credits / CreditTable.asset のインポータ自体は本タスクの範囲外(未実装)。
    /// docs/data/credits.csv が導入されたので、Screen_Credits実装時にM1/M2と同じ
    /// 「CSV→Editorインポータ→ScriptableObject」の仕組みをそのまま流用できる。
    /// </summary>
    public static class LicensesMarkdownGenerator
    {
        private const string CsvPath = "docs/data/credits.csv";
        private const string OutputPath = "docs/licenses.md";

        [MenuItem("Tools/データ/ライセンス一覧(licenses.md)を生成")]
        public static void Generate()
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError($"[LicensesMarkdownGenerator] CSVが見つかりません: {CsvPath}");
                return;
            }

            var rows = Santa.EditorTools.CsvUtil.Parse(File.ReadAllText(CsvPath));
            if (rows.Count == 0)
            {
                Debug.LogError("[LicensesMarkdownGenerator] credits.csv が空です。");
                return;
            }

            var header = rows[0];
            int idxCategory = Array.IndexOf(header, "category");
            int idxName = Array.IndexOf(header, "name");
            int idxAuthor = Array.IndexOf(header, "author");
            int idxLicense = Array.IndexOf(header, "license");
            int idxUrl = Array.IndexOf(header, "url");
            int idxNote = Array.IndexOf(header, "note");

            var sb = new StringBuilder();
            sb.AppendLine("# 使用素材のライセンス一覧");
            sb.AppendLine();
            sb.AppendLine("**★このファイルは手で編集しない。** `docs/data/credits.csv` が唯一の正であり、");
            sb.AppendLine("`Tools/データ/ライセンス一覧(licenses.md)を生成` で本ファイルを再生成する。");
            sb.AppendLine("(`05_Screen_Credits.md` §4.1 の方針。CSVとMarkdownの二重管理を防ぐため)");
            sb.AppendLine();
            sb.AppendLine($"最終生成日時: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            int count = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                var cells = rows[r];
                if (cells.Length == 1 && string.IsNullOrWhiteSpace(cells[0])) continue;

                string category = Get(cells, idxCategory);
                string name = Get(cells, idxName);
                string author = Get(cells, idxAuthor);
                string license = Get(cells, idxLicense);
                string url = Get(cells, idxUrl);
                string note = Get(cells, idxNote);

                sb.AppendLine($"## {name}");
                sb.AppendLine();
                sb.AppendLine($"- 分類: {category}");
                sb.AppendLine($"- 作者: {author}");
                sb.AppendLine($"- ライセンス: {license}");
                if (!string.IsNullOrEmpty(url)) sb.AppendLine($"- URL: {url}");
                if (!string.IsNullOrEmpty(note)) sb.AppendLine($"- 備考: {note}");
                sb.AppendLine();
                count++;
            }

            File.WriteAllText(OutputPath, sb.ToString(), new UTF8Encoding(true));
            Debug.Log($"[LicensesMarkdownGenerator] {OutputPath} を生成しました({count}件)。");
        }

        private static string Get(string[] cells, int index)
        {
            if (index < 0 || index >= cells.Length) return "";
            return cells[index]?.Trim() ?? "";
        }
    }
}
