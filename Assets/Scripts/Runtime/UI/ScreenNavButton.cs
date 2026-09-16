using Santa.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// ボタンに付けて、遷移先の画面をInspectorのenumで指定するだけのコンポーネント。
    /// Prefab内で完結するため、Prefab単体で保存・編集できる
    /// (ガイドライン §5.3-(2) / 共通仕様 §1.4)。
    ///
    /// onClick にシーン上のメソッドを焼き込まない。遷移先の解決は実行時に
    /// <see cref="ServiceLocator"/> 経由で行う(★開発チーム決定。共通仕様 §13-1)。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ScreenNavButton : MonoBehaviour
    {
        [SerializeField] private ScreenId target;
        [Tooltip("true の場合、タップ音(se_button)を鳴らす。")]
        [SerializeField] private bool playButtonSe = true;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (playButtonSe && ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Button);
            }

            ServiceLocator.Get<IScreenFlowService>().ShowScreen(target);
        }
    }
}
