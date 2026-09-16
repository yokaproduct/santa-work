using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames
{
    /// <summary>
    /// 仮置きミニゲーム(M2/M3/M5)の参照集約。実装が来るまでの間、
    /// `MicroGameBase` の契約だけを満たす最小限のUIを持つ。
    /// </summary>
    public class PlaceholderMicroGameRefs : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button tapButton;

        public TMP_Text Label => label;
        public Button TapButton => tapButton;
    }
}
