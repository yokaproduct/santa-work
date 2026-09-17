using Santa.Core;
using Santa.Game;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// `Overlay_Pause` のボタン配線。`31_Overlay_Pause.md` §4.2。
    ///
    /// ★2026-09-17実装時の判断(取りまとめ役への報告事項): 仕様書は「やめる」「はじめから」に
    /// 確認ダイアログ(`Overlay_Confirm`)を挟むかを未確定事項としており(§5-1/§5-2)、
    /// `Overlay_Confirm` 自体がまだ実装されていない。今回は範囲を広げすぎないため、
    /// 両ボタンとも確認なしで即座に実行する。確認を挟む場合は本クラスの
    /// `HandleRestart`/`HandleQuit` の先頭に `Overlay_Confirm` 呼び出しを挟む形になる想定。
    /// </summary>
    public class PauseOverlayController : MonoBehaviour
    {
        [SerializeField] private PauseOverlayRefs refs;

        private void Awake()
        {
            if (refs == null) refs = GetComponent<PauseOverlayRefs>();

            refs.ResumeButton.onClick.AddListener(HandleResume);
            refs.RestartButton.onClick.AddListener(HandleRestart);
            refs.QuitButton.onClick.AddListener(HandleQuit);
        }

        private void HandleResume()
        {
            PlayButtonSe();
            if (ServiceLocator.TryGet<IGameSessionService>(out var session))
            {
                session.ResumeFromPause();
            }
        }

        private void HandleRestart()
        {
            PlayButtonSe();
            if (!ServiceLocator.TryGet<IGameSessionService>(out var session)) return;

            // ★先に Overlay_Pause を閉じる(31_Overlay_Pause.md §4.2 / 30_Overlay_Countdown.md §7-3。
            // 表示中のままだと優先順位でOverlay_Countdownを表示できない)。
            if (ServiceLocator.TryGet<IScreenFlowService>(out var flow))
            {
                flow.HideOverlay(OverlayId.Pause);
            }

            session.StartSession(session.Mode, retry: true);
        }

        private void HandleQuit()
        {
            PlayButtonSe();
            // ★QuitWithoutRecording → ShowScreen(Title) が CloseCurrentOverlay() を内部で呼ぶため、
            // ここで明示的に Overlay_Pause を閉じる必要はない(ScreenFlowManager.ShowScreen参照)。
            if (ServiceLocator.TryGet<IGameSessionService>(out var session))
            {
                session.QuitWithoutRecording();
            }
        }

        private static void PlayButtonSe()
        {
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Button);
            }
        }
    }
}
