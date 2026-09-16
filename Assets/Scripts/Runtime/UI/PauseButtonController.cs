using Santa.Core;
using Santa.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// HUDのポーズボタン。`Overlay_Pause` を開く唯一の手段(共通仕様 §6.4)。
    /// ★このタスクでは `Overlay_Pause` のPrefabを作成していない(仮置きフェーズのため省略可の指示)。
    /// そのため、暫定的にオーバーレイ無しで直接 `GameSessionController.SetPaused` を
    /// トグルするだけの最小実装にしている。Overlay_Pause 実装時にここを差し替えること。
    ///
    /// ★重要な修正(実プレイで踏んだ不具合): 以前はこのクラス自身が `_paused` を
    /// ローカルに保持してトグルしていたが、`GameSessionController` はバックグラウンド移行や
    /// Editorでのフォーカス喪失(OnApplicationFocus(false))で**自動的に**ポーズすることがある。
    /// その自動ポーズはこのボタンの知らないところで起きるため、ローカルの `_paused` と
    /// 実際のポーズ状態がずれ、「ポーズボタンを1回押しても再開しない」不具合になっていた。
    /// 今は `IGameSessionService.IsPaused` を唯一の真実として読み、トグルする。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PauseButtonController : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private IGameSessionService _session;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(TogglePause);
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryGet<IGameSessionService>(out _session))
            {
                _session.PausedChanged += RefreshLabel;
            }
            RefreshLabel(_session?.IsPaused ?? false);
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.PausedChanged -= RefreshLabel;
            }
        }

        private void TogglePause()
        {
            if (!ServiceLocator.TryGet<IGameSessionService>(out var session)) return;

            // ★ローカルの状態を持たず、常に「今の実際の状態」を見て反転させる。
            session.SetPaused(!session.IsPaused);

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Button);
            }
        }

        private void RefreshLabel(bool paused)
        {
            if (label != null) label.text = paused ? "つづける" : "II";
        }
    }
}
