using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.Core
{
    /// <summary>
    /// 文言データ(ja / en)。共通仕様 00_共通仕様.md §9。
    /// Assets/Settings/LocalizationTable.asset として1つだけ存在する想定。
    /// </summary>
    [CreateAssetMenu(menuName = "Santa/Localization Table", fileName = "LocalizationTable")]
    public class LocalizationTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("例: title.start.label / hud.unitCount.format")]
            public string key;
            [TextArea] public string ja;
            [TextArea] public string en;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;
    }
}
