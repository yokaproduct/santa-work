using System.Collections.Generic;
using System.Text;

namespace Santa.EditorTools
{
    /// <summary>
    /// 最小限のCSVパーサ(RFC4180準拠。ダブルクォート囲み・エスケープ`""`・カンマ区切りに対応)。
    /// 継続的に使うツールの一部(共通仕様 §8.2)。
    /// </summary>
    public static class CsvUtil
    {
        /// <summary>1行を1レコード(セル配列)として返す。先頭行(ヘッダ)も含めて返すので、呼び出し側で読み飛ばすこと。</summary>
        public static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int i = 0;
            int length = text.Length;

            void EndField()
            {
                row.Add(field.ToString());
                field.Clear();
            }

            void EndRow()
            {
                EndField();
                rows.Add(row.ToArray());
                row.Clear();
            }

            while (i < length)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                            continue;
                        }
                        inQuotes = false;
                        i++;
                        continue;
                    }
                    field.Append(c);
                    i++;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        i++;
                        break;
                    case ',':
                        EndField();
                        i++;
                        break;
                    case '\r':
                        i++;
                        break;
                    case '\n':
                        EndRow();
                        i++;
                        break;
                    default:
                        field.Append(c);
                        i++;
                        break;
                }
            }

            // 末尾に改行が無い最終行を回収する。
            if (field.Length > 0 || row.Count > 0)
            {
                EndRow();
            }

            return rows;
        }
    }
}
