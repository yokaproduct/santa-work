using System;
using UnityEngine;

namespace Santa.Core
{
    /// <summary>
    /// 下端予約高さ(バナー領域)の実行時計算サービス。共通仕様 00_共通仕様.md §7.4。
    ///
    /// ★これはレイアウト所有権(§0.1)に対する明示的な例外。上書きしてよいのは
    /// BottomReserve.sizeDelta.y と ContentRoot.offsetMin.y の2値だけであり、
    /// 他のRectTransform値には絶対に触れない。実行時のみの変更であり、Prefabアセットは書き換えない。
    /// </summary>
    public interface IBottomInsetProvider
    {
        /// <summary>現在の下端予約高さ(基準解像度 1080×1920 換算のpx)。</summary>
        float CurrentInsetPx { get; }

        event Action<float> InsetChanged;

        /// <summary>バナーの実測高さが分かったときに広告サービス側から呼ぶ(基準解像度換算px)。</summary>
        void NotifyBannerHeight(float bannerHeightPx);

        /// <summary>Safe Area・バナー高さのいずれかが変わったときに再計算する。</summary>
        void Recalculate();

        /// <summary>
        /// 現在値を指定した2つのRectTransformへ反映するヘルパー。
        /// 各画面(ScreenRefsBase 等)はこれを呼ぶだけでよい。
        /// </summary>
        void Apply(RectTransform bottomReserve, RectTransform contentRoot);
    }
}
