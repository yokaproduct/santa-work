using UnityEngine;

namespace Santa.Core
{
    /// <summary>
    /// `Main.unity` 起動時に最初の画面を表示するだけの最小コンポーネント。
    /// `Screen_Boot` は今回のテストプレイ段階では省略しているため
    /// (取りまとめ役の指示: 最低限。無くても進めるなら省いてよい)、
    /// このコンポーネントが Boot の役割の一部(最初の画面表示)だけを肩代わりする。
    /// </summary>
    [DefaultExecutionOrder(100)] // 他サービスの Awake(-100)より後に実行する
    public class InitialScreenBootstrap : MonoBehaviour
    {
        [SerializeField] private ScreenId initialScreen = ScreenId.Title;

        private void Start()
        {
            ServiceLocator.Get<IScreenFlowService>().ShowScreen(initialScreen);
        }
    }
}
