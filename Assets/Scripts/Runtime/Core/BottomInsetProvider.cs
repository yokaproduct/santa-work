using System;
using UnityEngine;

namespace Santa.Core
{
    /// <summary>
    /// <see cref="IBottomInsetProvider"/> の実装。共通仕様 00_共通仕様.md §7.4。
    ///
    /// bottomInset = Screen.safeArea の下端インセット(px換算)
    ///             + バナーの実測高さ(非表示時は0ではなく「表示された場合の高さ」を使う)
    ///             + 固定余白(基準解像度換算)
    ///
    /// 算出タイミング: 起動時 / セーフエリア変化時 / 画面遷移時 / バナー読込完了時。
    /// Unity には Safe Area 変化の直接イベントが無いため、低頻度のポーリングで代替する
    /// (Update毎フレームではなく一定間隔。iPhone専用・縦画面固定のため変化は稀だが、
    /// OS更新・マルチタスキングでの変動に備える)。
    ///
    /// ★上書きしてよいのは Apply() が触れる2値だけ。PrefabUtility 系のAPIは一切呼ばない
    /// (実行時のみの変更であり、Prefabアセットへは書き戻さない)。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class BottomInsetProvider : MonoBehaviour, IBottomInsetProvider
    {
        [Header("基準解像度(共通仕様 §1.2で確定値)")]
        [SerializeField] private RectTransform referenceCanvasRect; // scaleFactor 取得用(CanvasのRectTransform)
        [SerializeField] private Canvas rootCanvas;

        [Header("初期値(ディレクター調整可。実測が無い間のフォールバック)")]
        [Tooltip("バナーが表示された場合の想定高さ(基準解像度px)。共通仕様 §7.1 の初期値 160px。")]
        [SerializeField] private float fallbackBannerHeightPx = 160f;

        [Tooltip("バナー予約領域の上に置く固定余白(共通仕様 §7.3-3)。")]
        [SerializeField] private float fixedMarginPx = 40f;

        [Tooltip("Safe Area変化を検知するポーリング間隔(秒)。")]
        [SerializeField] private float pollIntervalSeconds = 1.0f;

        private float _bannerHeightPx = -1f; // -1 = 未計測。フォールバック値を使う
        private float _pollTimer;
        private Rect _lastSafeArea;

        public float CurrentInsetPx { get; private set; }
        public event Action<float> InsetChanged;

        private void Awake()
        {
            ServiceLocator.Register<IBottomInsetProvider>(this);
        }

        private void Start()
        {
            if (ServiceLocator.TryGet<IScreenFlowService>(out var flow))
            {
                flow.ScreenChanged += _ => Recalculate();
            }

            Recalculate();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IBottomInsetProvider>();
        }

        private void Update()
        {
            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer < pollIntervalSeconds) return;
            _pollTimer = 0f;

            if (Screen.safeArea != _lastSafeArea)
            {
                Recalculate();
            }
        }

        public void NotifyBannerHeight(float bannerHeightPx)
        {
            _bannerHeightPx = bannerHeightPx;
            Recalculate();
        }

        public void Recalculate()
        {
            _lastSafeArea = Screen.safeArea;

            float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
            if (scaleFactor <= 0f) scaleFactor = 1f;

            float safeAreaBottomPx = Screen.safeArea.yMin / scaleFactor;
            float bannerHeightPx = _bannerHeightPx >= 0f ? _bannerHeightPx : fallbackBannerHeightPx;

            CurrentInsetPx = safeAreaBottomPx + bannerHeightPx + fixedMarginPx;
            InsetChanged?.Invoke(CurrentInsetPx);
        }

        public void Apply(RectTransform bottomReserve, RectTransform contentRoot)
        {
            if (bottomReserve != null)
            {
                var size = bottomReserve.sizeDelta;
                size.y = CurrentInsetPx;
                bottomReserve.sizeDelta = size;
            }

            if (contentRoot != null)
            {
                var min = contentRoot.offsetMin;
                min.y = CurrentInsetPx;
                contentRoot.offsetMin = min;
            }
        }
    }
}
