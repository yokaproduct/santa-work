using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// `Part_MicroGameTimerBar.prefab`(T2用)に付ける最小の参照集約。
    /// `Image.type = Filled` の帯を1つ持つだけの汎用部品。
    /// </summary>
    public class FillBarRefs : MonoBehaviour
    {
        [SerializeField] private Image fill;
        public Image Fill => fill;
    }
}
