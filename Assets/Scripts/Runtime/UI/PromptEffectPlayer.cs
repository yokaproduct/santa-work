using Santa.Core;
using Santa.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// 業務提示フェーズ(`[Prompt]`)の演出再生。`10_Screen_GamePlay.md` §12.4。
    ///
    /// 自前の時計を持たず、毎フレーム <see cref="IGameSessionService.PhaseElapsed"/>(ポーズ中は進まない)
    /// を評価してタイムラインをサンプリングする。これにより演出・T1・ポーズが必ず揃う(§12.4)。
    ///
    /// 触ってよい値は `PromptTextAnim` の localScale/anchoredPosition/localEulerAngles、
    /// `PromptText` の alpha、各 Image の alpha、`PromptBurstImage` の localEulerAngles/localScale だけ
    /// (§12.4)。他の RectTransform 値(sizeDelta・anchor 等)には一切触れない。
    /// </summary>
    public class PromptEffectPlayer : MonoBehaviour
    {
        [SerializeField] private PromptEffectSettings settings;
        [SerializeField] private Image promptBackdrop;
        [SerializeField] private RectTransform promptTextAnim;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Image promptBurstImage;
        [SerializeField] private Image promptFlash;

        private IGameSessionService _session;
        private GameSessionPhase _lastPhase = GameSessionPhase.Idle;

        private void OnEnable()
        {
            ServiceLocator.TryGet(out _session);
            ResetToIdle();
        }

        private void Update()
        {
            if (_session == null && !ServiceLocator.TryGet(out _session)) return;

            var phase = _session.Phase;
            if (phase == GameSessionPhase.Prompt)
            {
                Animate(_session.PhaseElapsed, Mathf.Max(_session.PromptDuration, 0.0001f));
            }
            _lastPhase = phase;
        }

        /// <summary>`[Prepare]`〜`[Intro]` の間、問題を覆う(§12.6)。文字・集中線・フラッシュは出さない。</summary>
        public void CoverProblem()
        {
            if (promptBackdrop != null)
            {
                promptBackdrop.color = settings != null ? settings.BackdropColor : Color.black;
                promptBackdrop.gameObject.SetActive(true);
            }
            HideForegroundVisuals();
        }

        /// <summary>`[Play]` 開始フレームでハードカット。背景を消し、演出を初期値に戻す(§12.2-C)。</summary>
        public void ResetToIdle()
        {
            HideForegroundVisuals();
            if (promptBackdrop != null) promptBackdrop.gameObject.SetActive(false);
        }

        /// <summary>ポーズ・バックグラウンド移行用。背景は表示したまま、演出だけ初期状態に戻す(§12.7)。</summary>
        public void ResetAnimationKeepCover()
        {
            HideForegroundVisuals();
        }

        private void HideForegroundVisuals()
        {
            // ★見た目は SetActive ではなく alpha で出し分ける(§12.4)。GameObject 自体は常に
            // アクティブにしておく(そうしないと Update() のたびに alpha を変えても描画されない)。
            if (promptTextAnim != null)
            {
                promptTextAnim.localScale = Vector3.one;
                promptTextAnim.anchoredPosition = Vector2.zero;
                promptTextAnim.localEulerAngles = Vector3.zero;
            }
            if (promptText != null) promptText.gameObject.SetActive(true);
            SetAlpha(promptText, 0f);

            if (promptBurstImage != null)
            {
                promptBurstImage.rectTransform.localScale = Vector3.one;
                promptBurstImage.rectTransform.localEulerAngles = Vector3.zero;
                promptBurstImage.gameObject.SetActive(true);
                // ★Sprite未設定の間は描画しない(§12.3。素材待ちで他の演出を止めないため)。
                promptBurstImage.enabled = promptBurstImage.sprite != null;
            }
            SetAlpha(promptBurstImage, 0f);

            if (promptFlash != null) promptFlash.gameObject.SetActive(true);
            SetAlpha(promptFlash, 0f);
        }

        private void Animate(float t, float promptDuration)
        {
            if (settings == null) return;

            float pushIn = settings.PushInDuration;
            float exit = settings.ExitDuration;
            float grooveDuration = settings.GetGrooveDuration(promptDuration);
            float grooveEndAbs = pushIn + grooveDuration;

            AnimateText(t, pushIn, grooveEndAbs, exit);
            AnimateBurst(t, pushIn, grooveEndAbs, exit);
            AnimateFlash(t);
            AnimateShake(t);
        }

        private void AnimateText(float t, float pushIn, float grooveEndAbs, float exit)
        {
            float scale;
            float alpha = 1f;

            if (t <= pushIn)
            {
                scale = EvaluatePushInScale(t, pushIn);
            }
            else if (t <= grooveEndAbs)
            {
                float grooveDuration = grooveEndAbs - pushIn;
                float grooveT = grooveDuration > 0f ? (t - pushIn) / grooveDuration : 1f;
                scale = Mathf.Lerp(settings.TextScaleSettle, settings.TextScaleGrooveEnd, grooveT);
                scale += EvaluateBeatBump(t);
            }
            else
            {
                float exitT = exit > 0f ? Mathf.Clamp01((t - grooveEndAbs) / exit) : 1f;
                float eased = EaseIn(exitT);
                scale = Mathf.Lerp(settings.TextScaleGrooveEnd, settings.TextScaleExitEnd, eased);
                alpha = Mathf.Lerp(1f, 0f, eased);
            }

            if (promptTextAnim != null) promptTextAnim.localScale = Vector3.one * scale;
            SetAlpha(promptText, alpha);
        }

        private float EvaluatePushInScale(float t, float pushIn)
        {
            float punchTime = Mathf.Min(settings.PunchTime, pushIn);
            float overshootTime = Mathf.Clamp(settings.OvershootTime, punchTime, pushIn);

            if (t <= punchTime)
            {
                float p = punchTime > 0f ? t / punchTime : 1f;
                return Mathf.Lerp(settings.TextScaleStart, settings.TextScalePunch, EaseIn(p));
            }
            if (t <= overshootTime)
            {
                float span = overshootTime - punchTime;
                float p = span > 0f ? (t - punchTime) / span : 1f;
                return Mathf.Lerp(settings.TextScalePunch, settings.TextScaleOvershoot, EaseOut(p));
            }
            {
                float span = pushIn - overshootTime;
                float p = span > 0f ? (t - overshootTime) / span : 1f;
                return Mathf.Lerp(settings.TextScaleOvershoot, settings.TextScaleSettle, EaseOut(p));
            }
        }

        private float EvaluateBeatBump(float t)
        {
            if (settings.BeatTimes == null) return 0f;
            float total = 0f;
            float half = Mathf.Max(0.0001f, settings.BeatBumpDuration * 0.5f);
            foreach (var beat in settings.BeatTimes)
            {
                float dt = t - beat;
                if (dt < 0f || dt > settings.BeatBumpDuration) continue;
                total += dt < half
                    ? Mathf.Lerp(0f, settings.BeatBumpAmount, dt / half)
                    : Mathf.Lerp(settings.BeatBumpAmount, 0f, (dt - half) / half);
            }
            return total;
        }

        private void AnimateBurst(float t, float pushIn, float grooveEndAbs, float exit)
        {
            if (promptBurstImage == null) return;

            float scale;
            float alpha;
            float rotation = 0f;

            if (t <= pushIn)
            {
                float p = pushIn > 0f ? Mathf.Clamp01(t / pushIn) : 1f;
                scale = Mathf.Lerp(settings.BurstScaleStart, settings.BurstScalePunch, EaseOut(p));
                float alphaP = Mathf.Clamp01(t / Mathf.Max(0.0001f, settings.PunchTime));
                alpha = Mathf.Lerp(0f, 1f, EaseOut(alphaP));
                if (t > settings.PunchTime)
                {
                    float p2 = (pushIn - settings.PunchTime) > 0f
                        ? Mathf.Clamp01((t - settings.PunchTime) / (pushIn - settings.PunchTime))
                        : 1f;
                    scale = Mathf.Lerp(settings.BurstScalePunch, settings.BurstScaleSettle, EaseOut(p2));
                }
            }
            else if (t <= grooveEndAbs)
            {
                float grooveDuration = grooveEndAbs - pushIn;
                float grooveT = grooveDuration > 0f ? (t - pushIn) / grooveDuration : 1f;
                scale = settings.BurstScaleSettle;
                alpha = Mathf.Lerp(1f, settings.BurstAlphaGrooveEnd, grooveT);
                rotation = (settings.BurstRotationDegPerSecond * (t - pushIn)) % 360f;
            }
            else
            {
                float exitT = exit > 0f ? Mathf.Clamp01((t - grooveEndAbs) / exit) : 1f;
                scale = settings.BurstScaleSettle;
                alpha = Mathf.Lerp(settings.BurstAlphaGrooveEnd, 0f, exitT);
                rotation = (settings.BurstRotationDegPerSecond * (grooveEndAbs - pushIn)) % 360f;
            }

            promptBurstImage.rectTransform.localScale = Vector3.one * scale;
            promptBurstImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);
            SetAlpha(promptBurstImage, alpha);
        }

        private void AnimateFlash(float t)
        {
            if (promptFlash == null) return;
            float endTime = Mathf.Max(0.0001f, settings.FlashFadeOutEndTime);
            float alpha = t <= settings.FlashFadeOutEndTime
                ? Mathf.Lerp(settings.FlashAlpha, 0f, EaseOut(t / endTime))
                : 0f;
            SetAlpha(promptFlash, alpha);
        }

        private void AnimateShake(float t)
        {
            if (promptTextAnim == null) return;
            Vector2 pos = EffectShakeUtil.GetShakeOffset(
                t, settings.ShakeStartTime, settings.ShakeEndTime,
                settings.ShakePositionAmplitude, settings.ShakeIntervalSeconds, seedSalt: 1);
            float rot = EffectShakeUtil.GetShakeRotation(
                t, settings.ShakeStartTime, settings.ShakeEndTime,
                settings.ShakeRotationAmplitudeDeg, settings.ShakeIntervalSeconds, seedSalt: 2);

            promptTextAnim.anchoredPosition = pos;
            promptTextAnim.localEulerAngles = new Vector3(0f, 0f, rot);
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
