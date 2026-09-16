using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// `Row_SettingsSlider.prefab` の参照集約。`04_Screen_Settings.md` §2.1(行Prefab)。
    /// BGM音量(S1)・SE音量(S2)の2行がこのPrefabをインスタンス化して使う。
    /// </summary>
    public class SettingsSliderRowRefs : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueText;
        [Tooltip("Sliderと同じGameObjectに付け、ドラッグを離した瞬間を検知する(§4.2: 保存は離した時点で1回)。")]
        [SerializeField] private PointerUpNotifier releaseNotifier;

        public TMP_Text LabelText => labelText;
        public Slider Slider => slider;
        public TMP_Text ValueText => valueText;
        public PointerUpNotifier ReleaseNotifier => releaseNotifier;
    }
}
