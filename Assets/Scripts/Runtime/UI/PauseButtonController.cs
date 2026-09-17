using Santa.Core;
using Santa.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// HUDのポーズボタン。`Overlay_Pause` を開く唯一の手段(共通仕様 §6.4)。
    /// ★2026-09-17実装: `Overlay_Pause` を実装したため、押すと `IGameSessionService.RequestPause()`
    /// (自動ポーズと共通の入口。`31_Overlay_Pause.md` §1.1)を呼ぶだけにした。
    /// 再開はこのボタンではなく `Overlay_Pause` の「つづける」からのみ行う
    /// (ポーズ中は `Overlay_Pause` の `Blocker` がHUD全面を覆うため、そもそもこのボタンは押せない)。
    ///
    /// ★過去の不具合の教訓(修正済み): 以前はこのクラス自身が `_paused` をローカルに保持して
    /// トグルしていたため、バックグラウンド移行等の自動ポーズとローカル状態がずれ、
    /// 「ポーズボタンを1回押しても再開しない」不具合になっていた。押す操作を「ポーズに入る」
    /// 一方向だけに限定した今回の実装では、この種のズレはそもそも起こり得ない。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PauseButtonController : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (!ServiceLocator.TryGet<IGameSessionService>(out var session)) return;

            session.RequestPause();

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Button);
            }
        }
    }
}
