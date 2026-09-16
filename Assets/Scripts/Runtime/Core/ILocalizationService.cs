using System;

namespace Santa.Core
{
    /// <summary>
    /// 文言解決サービス。共通仕様 00_共通仕様.md §9。
    /// キーから ja/en の文字列を引く。日本語原文をキーに使わない(§9.1)。
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>"ja" / "en"。</summary>
        string CurrentLanguage { get; }

        /// <summary>キーが未登録の場合は `!key!` のような可視化された形で返し、実行時に画像上で気づけるようにする。</summary>
        string GetString(string key);

        /// <summary>`{0}` 形式のプレースホルダを埋めて返す(共通仕様 §9.2)。</summary>
        string GetString(string key, params object[] args);

        void SetLanguage(string languageCode);

        event Action LanguageChanged;
    }
}
