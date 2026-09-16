using Santa.Core;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// 全画面共通のロジック基底。Refs経由でのみ要素を操作する(共通仕様 §1.3)。
    ///
    /// 下端予約高さ(BottomReserve.sizeDelta.y / ContentRoot.offsetMin.y)への反映は、
    /// ここで一括して行う。各画面の <画面名>ScreenController は個別にこの処理を書かない
    /// (共通仕様 §7.4 の「反映先はこの2値だけ」を1箇所に閉じ込めるため)。
    /// </summary>
    /// <typeparam name="TRefs">この画面の ScreenRefs 型。同じ GameObject に付いている前提。</typeparam>
    public abstract class ScreenControllerBase<TRefs> : MonoBehaviour where TRefs : ScreenRefsBase
    {
        protected TRefs Refs { get; private set; }

        protected virtual void Awake()
        {
            Refs = GetComponent<TRefs>();
            if (Refs == null)
            {
                Debug.LogError($"[{GetType().Name}] 同じGameObjectに {typeof(TRefs).Name} が見つかりません。" +
                                "Prefabのルート構成を確認してください(共通仕様 §1.3)。");
            }
        }

        protected virtual void OnEnable()
        {
            if (ServiceLocator.TryGet<IBottomInsetProvider>(out var inset))
            {
                inset.InsetChanged += HandleBottomInsetChanged;
                inset.Apply(Refs.BottomReserve, Refs.ContentRoot);
            }
        }

        protected virtual void OnDisable()
        {
            if (ServiceLocator.TryGet<IBottomInsetProvider>(out var inset))
            {
                inset.InsetChanged -= HandleBottomInsetChanged;
            }
        }

        private void HandleBottomInsetChanged(float _)
        {
            if (ServiceLocator.TryGet<IBottomInsetProvider>(out var inset))
            {
                inset.Apply(Refs.BottomReserve, Refs.ContentRoot);
            }
        }
    }
}
