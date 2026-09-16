using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// Safe Area 対応方式(★開発チーム決定。共通仕様 §13-3 / ガイドライン §8-9)。
    ///
    /// 【決定】画面ごとに持つ。各画面Prefabの `SafeAreaRoot`(共通仕様 §1.3 で全画面が持つことが
    /// 確定している要素)に、この再利用可能コンポーネントを1つ付ける。
    /// ルートに1つだけ置く全画面共通のフィッターは採用しない。理由は本タスクの報告を参照。
    ///
    /// 【責務の分離が重要】このコンポーネントは上端・左右のみを扱う。
    /// ★下端は一切触らない(anchorMin.y は常に 0 に固定する)。
    /// 下端予約高さは <see cref="Santa.Core.IBottomInsetProvider"/> が
    /// BottomReserve.sizeDelta.y / ContentRoot.offsetMin.y の2値だけを別途計算する
    /// (共通仕様 §7.4)。両者が同時に下端へ触れると二重補正になるため、責務を明確に分ける。
    ///
    /// ★これは §0.1 のレイアウト所有権ルールに対する明示的な例外(実行時のみ。anchorMin/anchorMaxを
    /// 上書きする)。Prefabアセットへは書き戻さない(PrefabUtility系のAPIは呼ばない)。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float pollIntervalSeconds = 1.0f;

        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private float _pollTimer;

        private void Awake()
        {
            if (target == null) target = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer < pollIntervalSeconds) return;
            _pollTimer = 0f;

            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            var safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0) return;

            var anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            var anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

            // ★下端は BottomInsetProvider の専管。ここでは常に 0 に固定する。
            anchorMin.y = 0f;

            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
        }
    }
}
