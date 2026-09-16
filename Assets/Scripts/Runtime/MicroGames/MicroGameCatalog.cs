using System.Collections.Generic;
using UnityEngine;

namespace Santa.MicroGames
{
    /// <summary>
    /// 出題対象の種目一覧。共通仕様 00_共通仕様.md §4.2。
    /// <see cref="MicroGameDefinition.Enabled"/> が true のものだけが出題対象になる。
    /// </summary>
    [CreateAssetMenu(menuName = "Santa/MicroGame Catalog", fileName = "MicroGameCatalog")]
    public class MicroGameCatalog : ScriptableObject
    {
        [SerializeField] private List<MicroGameDefinition> definitions = new List<MicroGameDefinition>();

        public IReadOnlyList<MicroGameDefinition> Definitions => definitions;

        /// <summary>出題対象(enabled == true)の種目だけを返す。</summary>
        public List<MicroGameDefinition> GetEnabledDefinitions()
        {
            var result = new List<MicroGameDefinition>();
            foreach (var def in definitions)
            {
                if (def != null && def.Enabled)
                {
                    result.Add(def);
                }
            }
            return result;
        }
    }
}
