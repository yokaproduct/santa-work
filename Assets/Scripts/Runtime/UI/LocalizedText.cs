using Santa.Core;
using TMPro;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// Text か TextMeshPro か(★開発チーム決定。共通仕様 §13-2 / ガイドライン §8-3)。
    ///
    /// 【決定】TextMeshPro(TMP_Text)を採用する。理由は本タスクの報告を参照。
    ///
    /// Prefabに付け、Inspectorでキー(例 "title.start.label")を指定する。
    /// 文言はキーで持ち、原文を直接書かない(共通仕様 §9.1)。
    /// フォントサイズ・折り返し・ボックスサイズは Prefab側(ディレクターの所有物)であり、
    /// このコンポーネントは書き換えない。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text _text;
        private object[] _formatArgs;

        public string Key => key;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryGet<ILocalizationService>(out var loc))
            {
                loc.LanguageChanged += Refresh;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (ServiceLocator.TryGet<ILocalizationService>(out var loc))
            {
                loc.LanguageChanged -= Refresh;
            }
        }

        /// <summary>キーを差し替える(実行時に動的な文言に切り替えたい場合)。</summary>
        public void SetKey(string newKey, params object[] args)
        {
            key = newKey;
            _formatArgs = args;
            Refresh();
        }

        private void Refresh()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (string.IsNullOrEmpty(key) || !ServiceLocator.TryGet<ILocalizationService>(out var loc))
            {
                return;
            }

            _text.text = (_formatArgs != null && _formatArgs.Length > 0)
                ? loc.GetString(key, _formatArgs)
                : loc.GetString(key);
        }
    }
}
