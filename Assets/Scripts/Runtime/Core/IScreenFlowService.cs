using System;

namespace Santa.Core
{
    /// <summary>
    /// 画面遷移サービスの契約。<see cref="ServiceLocator"/> 経由で解決する
    /// (ガイドライン §5.3-(2) / 共通仕様 §1.4)。
    /// </summary>
    public interface IScreenFlowService
    {
        ScreenId CurrentScreen { get; }

        /// <summary>画面を遷移する。ScreenLayer には常に1画面だけが存在する状態を保つ。</summary>
        void ShowScreen(ScreenId id);

        /// <summary>
        /// オーバーレイを表示する。優先順位(共通仕様 §2.2)は呼び出し側ではなくこのサービスが調停する。
        /// ★戻り値は「実際に表示できたか」。Prefab未登録の場合は false を返す(何も表示しない)。
        /// 呼び出し側(GameSessionController の Overlay_MicroGameIntro 待ち等)は、
        /// false の場合に「オーバーレイが閉じるまで待つ」処理へ入ってはならない
        /// (誰も閉じない = 永久に待ち続けるハングを引き起こすため。実装時に実際に踏んだ不具合)。
        /// </summary>
        bool ShowOverlay(OverlayId id);

        /// <summary>指定オーバーレイが現在表示中なら閉じる。</summary>
        void HideOverlay(OverlayId id);

        event Action<ScreenId> ScreenChanged;
    }
}
