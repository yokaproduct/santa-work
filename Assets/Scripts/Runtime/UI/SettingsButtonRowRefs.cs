using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// タップで値を切り替える行(現状は言語切替 S3 のみが使う仮置き行)の参照集約。
    /// `04_Screen_Settings.md` §7-2「言語切替UI(S3)」は本来 `Row_SettingsSelect.prefab` の
    /// 検討事項だが、ディレクター指示(2026-09-16)により最小構成として本Prefabで仮置きする。
    /// </summary>
    public class SettingsButtonRowRefs : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Button button;

        public TMP_Text LabelText => labelText;
        public TMP_Text ValueText => valueText;
        public Button Button => button;
    }
}
