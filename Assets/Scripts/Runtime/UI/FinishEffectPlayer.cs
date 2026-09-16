using Santa.Core;
using Santa.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// 終了演出フェーズ(`[Finish]`)の演出再生。`10_Screen_GamePlay.md` §13.4。
    /// `PromptEffectPlayer` と同じ設計(自前の時計を持たず `IGameSessionService.PhaseElapsed` をサンプリング)。
    ///
    /// ★遷移(`finishSequenceDuration` 経過後の `Screen_Result` への遷移)は呼ばない。
    /// 見た目だけを担当し、進行の決定権は `GameSessionController` が持つ(§13.4)。
    /// </summary>
    public class FinishEffectPlayer : MonoBehaviour
    {
        [SerializeField] private FinishEffectSettings settings;
        [SerializeField] private Image finishBlocker;
        [SerializeField] private Image finishFlash;
        [SerializeField] private RectTransform finishTelopAnim;
        [SerializeField] private TMP_Text finishTelopText;

        private IGameSessionService _session;

        /// <summary>GamePlayScreenControllerがT1バーの流し込み(§13.2)に使う長さ。</summary>
        public float TimeBarFlushDuration => settings != null ? settings.TimeBarFlushDuration : 0.2f;

        private void OnEnable()
        {
            ServiceLocator.TryGet(out _session);
            ResetToIdle();
        }

        private void Update()
        {
            if (_session == null && !ServiceLocator.TryGet(out _session)) return;

            if (_session.Phase == GameSessionPhase.Finish)
            {
                Animate(_session.PhaseElapsed, Mathf.Max(_session.FinishSequenceDuration, 0.0001f));
            }
        }

        /// <summary>待機中の初期状態。`FinishRoot` 自体は常にアクティブのまま、見た目だけ隠す(§13.3)。</summary>
        public void ResetToIdle()
        {
            if (finishBlocker != null)
            {
                finishBlocker.raycastTarget = false;
                SetAlpha(finishBlocker, 0f);
            }
            if (finishTelopAnim != null)
            {
                finishTelopAnim.localScale = Vector3.one;
                finishTelopAnim.anchoredPosition = Vector2.zero;
                finishTelopAnim.localEulerAngles = Vector3.zero;
            }
            SetAlpha(finishTelopText, 0f);
            SetAlpha(finishFlash, 0f);
        }

        /// <summary>`[Finish]` 開始フレーム。以後は Update が t を進めて描画する(§13.5)。</summary>
        public void Begin()
        {
            if (finishBlocker != null) finishBlocker.raycastTarget = true;
            if (_session != null)
            {
                Animate(_session.PhaseElapsed, Mathf.Max(_session.FinishSequenceDuration, 0.0001f));
            }
        }

        private void Animate(float t, float duration)
        {
            if (settings == null) return;

            AnimateBlocker(t);
            AnimateFlash(t);
            AnimateTelop(t, duration);
        }

        private void AnimateBlocker(float t)
        {
            if (finishBlocker == null) return;
            float alpha;
            if (t <= settings.BlockerAlphaFadeInEndTime)
            {
                float p = settings.BlockerAlphaFadeInEndTime > 0f ? t / settings.BlockerAlphaFadeInEndTime : 1f;
                alpha = Mathf.Lerp(0f, settings.BlockerAlphaDuring, p);
            }
            else if (t <= settings.BlockerAlphaFinalStartTime)
            {
                alpha = settings.BlockerAlphaDuring;
            }
            else
            {
                float span = Mathf.Max(0.0001f, 1.5f - settings.BlockerAlphaFinalStartTime);
                float p = Mathf.Clamp01((t - settings.BlockerAlphaFinalStartTime) / span);
                alpha = Mathf.Lerp(settings.BlockerAlphaDuring, settings.BlockerAlphaFinal, p);
            }
            SetAlpha(finishBlocker, alpha);
        }

        private void AnimateFlash(float t)
        {
            if (finishFlash == null) return;
            float endTime = Mathf.Max(0.0001f, settings.FlashFadeOutEndTime);
            float alpha = t <= settings.FlashFadeOutEndTime
                ? Mathf.Lerp(settings.FlashAlpha, 0f, t / endTime)
                : 0f;
            SetAlpha(finishFlash, alpha);
        }

        private void AnimateTelop(float t, float duration)
        {
            float scale;
            float alpha;

            if (t <= settings.AlphaInEndTime)
            {
                alpha = Mathf.Lerp(0f, 1f, settings.AlphaInEndTime > 0f ? t / settings.AlphaInEndTime : 1f);
            }
            else
            {
                alpha = 1f;
            }

            if (t <= settings.PunchTime)
            {
                float p = settings.PunchTime > 0f ? Mathf.Clamp01(t / settings.PunchTime) : 1f;
                scale = Mathf.Lerp(settings.TelopScaleStart, settings.TelopScalePunch, EaseIn(p));
            }
            else if (t <= settings.OvershootTime)
            {
                float span = settings.OvershootTime - settings.PunchTime;
                float p = span > 0f ? (t - settings.PunchTime) / span : 1f;
                scale = Mathf.Lerp(settings.TelopScalePunch, settings.TelopScaleOvershoot, EaseOut(p));
            }
            else if (t <= settings.SettleTime)
            {
                float span = settings.SettleTime - settings.OvershootTime;
                float p = span > 0f ? (t - settings.OvershootTime) / span : 1f;
                scale = Mathf.Lerp(settings.TelopScaleOvershoot, settings.TelopScaleSettle, EaseOut(p));
            }
            else if (t <= settings.GrooveEndTime)
            {
                float span = settings.GrooveEndTime - settings.SettleTime;
                float p = span > 0f ? (t - settings.SettleTime) / span : 1f;
                scale = Mathf.Lerp(settings.TelopScaleSettle, settings.TelopScaleGrooveEnd, p);
            }
            else
            {
                float span = Mathf.Max(0.0001f, duration - settings.GrooveEndTime);
                float p = Mathf.Clamp01((t - settings.GrooveEndTime) / span);
                float eased = EaseIn(p);
                scale = Mathf.Lerp(settings.TelopScaleGrooveEnd, settings.TelopScaleExitEnd, eased);
                alpha = Mathf.Lerp(1f, 0f, eased);
            }

            if (finishTelopAnim != null) finishTelopAnim.localScale = Vector3.one * scale;
            SetAlpha(finishTelopText, alpha);

            if (finishTelopAnim != null)
            {
                Vector2 pos = EffectShakeUtil.GetShakeOffset(
                    t, settings.ShakeStartTime, settings.ShakeEndTime,
                    settings.ShakePositionAmplitude, settings.ShakeIntervalSeconds, seedSalt: 3);
                float rot = EffectShakeUtil.GetShakeRotation(
                    t, settings.ShakeStartTime, settings.ShakeEndTime,
                    settings.ShakeRotationAmplitudeDeg, settings.ShakeIntervalSeconds, seedSalt: 4);
                finishTelopAnim.anchoredPosition = pos;
                finishTelopAnim.localEulerAngles = new Vector3(0f, 0f, rot);
            }
        }

        private static float EaseIn(float p) => p * p;
        private static float EaseOut(float p) => 1f - (1f - p) * (1f - p);

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}
