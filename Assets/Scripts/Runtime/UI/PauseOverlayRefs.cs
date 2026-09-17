using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// `Overlay_Pause` の参照集約。`31_Overlay_Pause.md` §2/§3。
    /// `Blocker` はコードから触らないため持たない。
    /// </summary>
    public class PauseOverlayRefs : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        public Button ResumeButton => resumeButton;
        public Button RestartButton => restartButton;
        public Button QuitButton => quitButton;
    }
}
