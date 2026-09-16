using TMPro;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>`Screen_Result` の仮置き版Refs。見た目は問わない指示のため最小限。</summary>
    public class ResultScreenRefs : ScreenRefsBase
    {
        [SerializeField] private TMP_Text summaryText;
        public TMP_Text SummaryText => summaryText;
    }
}
