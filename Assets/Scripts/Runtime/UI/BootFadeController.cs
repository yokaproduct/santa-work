using System.Collections;
using Santa.Game;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// アプリ起動直後の1回だけ再生するフェードイン(黒→透明)。
    /// ディレクター指示(2026-09-19): 「ゲーム起動時に、フェードインからスタートする仕様にしてください。
    /// 2秒くらいで設定してみて」。長さは <see cref="GameBalanceSettings.BootFadeInSeconds"/>(既定2.0秒)。
    ///
    /// `Main.unity` の UICanvas 直下に、ScreenFlowManager が管理する ScreenLayer/OverlayLayer とは別の
    /// 常設オブジェクトとして置く(画面Prefab側は一切変更しない)。ScreenLayer/OverlayLayer より後の
    /// 兄弟にすることで、それらを常に手前(最前面)で覆う。画面遷移のたびに再生されないのは、
    /// ScreenFlowManager の管理外にある常設オブジェクトであり、誰も再度アクティブ化しないため。
    ///
    /// 入力遮断は「フェード中ずっと raycastTarget を持つ不透明な全画面Imageが最前面に居ること」自体で
    /// 成立する(CanvasGroup.alpha を下げても blocksRaycasts/raycastTarget には影響しない)。
    /// Screen_Title 側の0.3秒の入力遅延(TitleScreenController)とは独立に働き、フェード秒数の方が
    /// 長ければ結果的にこちらが効き続ける(優先順位を新設する必要はない)。
    ///
    /// `Time.timeScale` に依存せず `Time.unscaledDeltaTime` で進める(プロジェクトの既存方針)。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BootFadeController : MonoBehaviour
    {
        [Tooltip("フェードインの長さの設定元。未設定時のみコード既定値2.0秒を使う。")]
        [SerializeField] private GameBalanceSettings balance;

        private CanvasGroup _group;
        private bool _hasPlayed;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = true;
        }

        /// <summary>
        /// ★起動時の1回だけ再生するためのガード。Unityの通常のライフサイクルでは
        /// Start()自体が対象オブジェクトの生存中に1回しか呼ばれないためこのガードは冗長だが、
        /// 誤って再度呼ばれた場合(テストからの直接呼び出し等)でもフェードが再生されないことを
        /// 明示的に保証する。
        /// </summary>
        private void Start()
        {
            if (_hasPlayed) return;
            _hasPlayed = true;

            float duration = balance != null ? balance.BootFadeInSeconds : 2.0f;
            if (duration <= 0f)
            {
                Complete();
                return;
            }

            StartCoroutine(FadeRoutine(duration));
        }

        private IEnumerator FadeRoutine(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = 1f - Mathf.Clamp01(t / duration);
                yield return null;
            }

            Complete();
        }

        /// <summary>完全に透明化し、入力遮断も解除したうえで自身を非活性化する(以後は描画もヒットテストも行わない)。</summary>
        private void Complete()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
