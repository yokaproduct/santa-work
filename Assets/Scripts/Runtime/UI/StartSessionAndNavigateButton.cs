using Santa.Core;
using Santa.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// 「90びょうモードを始める」(Screen_ModeSelect)・「もう一回」(Screen_Result)の両方で使う。
    /// GameSessionController.StartSession を呼んでから画面遷移する
    /// (`10_Screen_GamePlay.md` §4.1: 「画面に入ったとき」ではなく「入る前」にセッションを開始する設計)。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StartSessionAndNavigateButton : MonoBehaviour
    {
        [SerializeField] private GameModeDefinition mode;
        [SerializeField] private bool retry;
        [SerializeField] private ScreenId target = ScreenId.GamePlay;

        /// <summary>
        /// ★2026-09-17追加。`TitleScreenController` がハイスコア・最高ランクの表示(`RankTable`)を
        /// 読むために、同じGameObjectに付いているこのコンポーネント経由でモード定義を参照する
        /// (02_Screen_Title.md §3.4「モードのプロパティを流し込む」)。新たに参照を複製して持たない。
        /// </summary>
        public GameModeDefinition Mode => mode;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Button);
            }

            ServiceLocator.Get<IGameSessionService>().StartSession(mode, retry);
            ServiceLocator.Get<IScreenFlowService>().ShowScreen(target);
        }
    }
}
