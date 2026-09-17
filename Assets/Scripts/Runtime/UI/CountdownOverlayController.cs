using Santa.Core;
using Santa.Game;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// `Overlay_Countdown` の演出再生。`30_Overlay_Countdown.md` §4.2/§4.3。
    ///
    /// 自前の時計を持たず、毎フレーム <see cref="IGameSessionService"/> が公開する
    /// `CountdownStepIndex`/`CountdownStepCount`/`CountdownStepElapsed`/`CountdownStepDuration`
    /// (ポーズ中は進まない)を読んでタイムラインをサンプリングする。これは
    /// `PromptEffectPlayer` が `PhaseElapsed` を読むのと同じ方式(§4.3の要件)。
    ///
    /// 触ってよい値は `CountAnim` の `localScale` と `CanvasGroup.alpha` だけ(§4.2)。
    ///
    /// ★2026-09-17追加(31_Overlay_Pause.md §6.2): `Overlay_Pause` の「つづける」でも
    /// このオーバーレイを再利用して短縮カウントダウンを表示する。その間 `Phase` は
    /// 直前のフェーズ(Play/Prompt/Intro等)のままで `Countdown` に変わらないため、
    /// `CountdownStepIndex` の有無だけで表示可否を判定する(`Phase` を条件にしない)。
    /// </summary>
    public class CountdownOverlayController : MonoBehaviour
    {
        [SerializeField] private CountdownOverlayRefs refs;

        [Header("演出値(§4.2。ディレクターがInspectorで変えてよい)")]
        [SerializeField] private float appearScale = 1.3f;
        [SerializeField] private float appearDuration = 0.10f;
        [SerializeField] private float fadeOutDuration = 0.10f;

        private IGameSessionService _session;
        private int _lastRenderedStepIndex = -1;

        private void OnEnable()
        {
            ServiceLocator.TryGet(out _session);
            _lastRenderedStepIndex = -1;

            if (refs.CountAnim != null) refs.CountAnim.localScale = Vector3.one;
            if (refs.CountAnimGroup != null) refs.CountAnimGroup.alpha = 0f;
        }

        private void Update()
        {
            if (_session == null && !ServiceLocator.TryGet(out _session)) return;

            int stepIndex = _session.CountdownStepIndex;
            int stepCount = _session.CountdownStepCount;
            if (stepIndex < 0 || stepCount <= 0) return;

            if (stepIndex != _lastRenderedStepIndex)
            {
                _lastRenderedStepIndex = stepIndex;
                ApplyStepText(stepIndex, stepCount);
            }

            AnimateStep(_session.CountdownStepElapsed, Mathf.Max(0.0001f, _session.CountdownStepDuration));
        }

        /// <summary>
        /// §1.2: i &lt; N-1 なら数字 (N-1-i)、最後のステップ(i = N-1)なら「スタート!」。
        /// </summary>
        private void ApplyStepText(int stepIndex, int stepCount)
        {
            bool isLastStep = stepIndex == stepCount - 1;
            int number = stepCount - 1 - stepIndex;

            if (refs.CountNumberText != null)
            {
                refs.CountNumberText.text = number.ToString();
                refs.CountNumberText.gameObject.SetActive(!isLastStep);
            }
            if (refs.StartText != null)
            {
                refs.StartText.gameObject.SetActive(isLastStep);
            }
        }

        /// <summary>
        /// §4.2: t=0で scale 1.3・alpha 1即表示 → 0〜0.10秒で scale 1.3→1.0(イーズアウト)
        /// → 静止 → 終了0.10秒前からalpha 1→0。ステップが0.2秒未満なら登場とフェードをステップの半分ずつに縮める。
        /// </summary>
        private void AnimateStep(float t, float stepDuration)
        {
            t = Mathf.Clamp(t, 0f, stepDuration);

            float appear = Mathf.Min(appearDuration, stepDuration * 0.5f);
            float fadeOut = Mathf.Min(fadeOutDuration, stepDuration * 0.5f);
            float fadeStart = stepDuration - fadeOut;

            float scale;
            float alpha;

            if (t <= appear)
            {
                float p = appear > 0f ? t / appear : 1f;
                scale = Mathf.Lerp(appearScale, 1f, EaseOut(p));
                alpha = 1f;
            }
            else if (t < fadeStart)
            {
                scale = 1f;
                alpha = 1f;
            }
            else
            {
                float span = Mathf.Max(0.0001f, fadeOut);
                float p = Mathf.Clamp01((t - fadeStart) / span);
                scale = 1f;
                alpha = Mathf.Lerp(1f, 0f, p);
            }

            if (refs.CountAnim != null) refs.CountAnim.localScale = new Vector3(scale, scale, 1f);
            if (refs.CountAnimGroup != null) refs.CountAnimGroup.alpha = alpha;
        }

        private static float EaseOut(float p) => 1f - (1f - p) * (1f - p);
    }
}
