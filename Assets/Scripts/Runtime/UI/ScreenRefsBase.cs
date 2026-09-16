using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// 全画面共通の参照集約の基底(共通仕様 00_共通仕様.md §1.3 / §1.4)。
    /// ★開発チーム決定(§13-8): 共通3フィールドをここへ切り出す。
    ///
    /// 「全画面が ContentRoot と BottomReserve を持つ」ことは仕様として確定している。
    /// ロジックは持たない。RectTransformの値には一切触れない(初回生成後はディレクターの所有物)。
    ///
    /// AIはPrefab内の要素を transform.Find などの名前検索で取得してはならない。
    /// 必ずこの [SerializeField] 参照を経由する(共通仕様 §1.5)。
    /// </summary>
    public abstract class ScreenRefsBase : MonoBehaviour
    {
        [Header("共通(全画面が持つ)")]
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform bottomReserve;

        public RectTransform SafeAreaRoot => safeAreaRoot;
        public RectTransform ContentRoot => contentRoot;
        public RectTransform BottomReserve => bottomReserve;
    }
}
