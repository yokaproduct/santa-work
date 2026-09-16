namespace Santa.UI
{
    /// <summary>
    /// テストプレイ用の仮置き画面のロジック。ボタンの遷移は `ScreenNavButton` /
    /// `StartSessionAndNavigateButton` が個別に持つため、ここでは共通の下端インセット反映
    /// (基底クラス)以外は何もしない。
    /// </summary>
    public class PlaceholderScreenController : ScreenControllerBase<PlaceholderScreenRefs>
    {
    }
}
