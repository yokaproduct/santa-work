using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Santa.UI
{
    /// <summary>
    /// 同じGameObject(または子)へのポインタ操作が終わった瞬間(指を離した瞬間)を通知する。
    ///
    /// `Screen_Settings`(04_Screen_Settings.md §4.2)の音量スライダーは、ドラッグ中は
    /// `AudioManager` へ即座に反映するが、`SaveManager` への保存(および確認SE)は
    /// 「離した時点で1回だけ」行う必要がある(毎フレームPlayerPrefsへ書くと重いため)。
    /// `Slider` の `onValueChanged` だけではこの区別ができないため、この補助コンポーネントを
    /// `Slider` と同じGameObjectに追加し、`OnPointerUp` を購読して離した瞬間を検知する。
    ///
    /// ★UnityのEventSystemは、ヒットしたGameObjectに対象のインターフェースが無ければ
    /// 親をたどって実装を探す(`ExecuteEvents.GetEventHandler`)。`Slider` はハンドル画像等の
    /// 子をクリックしても自身がドラッグ処理を受け取れる仕組みになっており、これと同じ理由で、
    /// `Slider` の実体があるGameObjectに本コンポーネントを付ければ、ハンドルのどこを離しても検知できる。
    /// </summary>
    public class PointerUpNotifier : MonoBehaviour, IPointerUpHandler
    {
        public event Action Released;

        public void OnPointerUp(PointerEventData eventData)
        {
            Released?.Invoke();
        }
    }
}
