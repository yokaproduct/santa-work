using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.Core
{
    /// <summary>
    /// <see cref="ILocalizationService"/> の実装。共通仕様 00_共通仕様.md §9。
    /// 既定は端末言語追従(Application.systemLanguage が Japanese なら ja、それ以外は en)。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class LocalizationService : MonoBehaviour, ILocalizationService
    {
        [SerializeField] private LocalizationTable table;

        private readonly Dictionary<string, (string ja, string en)> _map = new Dictionary<string, (string, string)>();

        public string CurrentLanguage { get; private set; } = "ja";
        public event Action LanguageChanged;

        private void Awake()
        {
            BuildMap();

            if (ServiceLocator.TryGet<ISaveManager>(out var save) && !string.IsNullOrEmpty(save.Language))
            {
                CurrentLanguage = save.Language;
            }
            else
            {
                CurrentLanguage = Application.systemLanguage == SystemLanguage.Japanese ? "ja" : "en";
            }

            ServiceLocator.Register<ILocalizationService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ILocalizationService>();
        }

        private void BuildMap()
        {
            _map.Clear();
            if (table == null) return;

            foreach (var entry in table.Entries)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;
                _map[entry.key] = (entry.ja, entry.en);
            }
        }

        public string GetString(string key)
        {
            if (_map.TryGetValue(key, out var pair))
            {
                var value = CurrentLanguage == "ja" ? pair.ja : pair.en;
                if (!string.IsNullOrEmpty(value)) return value;
            }

            // 未登録・未翻訳を実行時に可視化する(ローカライズ漏れに気づけるようにするため)。
            return $"!{key}!";
        }

        public string GetString(string key, params object[] args)
        {
            var format = GetString(key);
            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                Debug.LogError($"[LocalizationService] 書式エラー: key={key}, format=\"{format}\"");
                return format;
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                languageCode = Application.systemLanguage == SystemLanguage.Japanese ? "ja" : "en";
            }

            CurrentLanguage = languageCode;

            if (ServiceLocator.TryGet<ISaveManager>(out var save))
            {
                save.Language = languageCode;
            }

            LanguageChanged?.Invoke();
        }
    }
}
