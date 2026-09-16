using UnityEngine;

namespace Santa.Game
{
    /// <summary>
    /// 終了演出フェーズ(`[Finish]`)の演出タイムライン。`10_Screen_GamePlay.md` §13.2 / §13.4。
    /// `PromptEffectSettings` と同じ考え方(演出値のみ。レイアウト値は持たない)。
    /// </summary>
    [CreateAssetMenu(menuName = "Santa/Finish Effect Settings", fileName = "FinishEffectSettings")]
    public class FinishEffectSettings : ScriptableObject
    {
        [Header("暗幕(FinishBlocker)のalpha")]
        [SerializeField] private float blockerAlphaDuring = 0.45f;
        [SerializeField] private float blockerAlphaFadeInEndTime = 0.10f;
        [SerializeField] private float blockerAlphaFinal = 0.8f;
        [Tooltip("blockerAlphaFinalへ向けて上げ始める時刻(finishSequenceDuration起点の逆算ではなく絶対時刻)。")]
        [SerializeField] private float blockerAlphaFinalStartTime = 1.30f;

        [Header("T1バーの流し込み(閾値未満で終わった場合のみ)")]
        [SerializeField] private float timeBarFlushDuration = 0.20f;

        [Header("フラッシュ(FinishFlash)")]
        [SerializeField] private float flashAlpha = 0.6f;
        [SerializeField] private float flashFadeOutEndTime = 0.15f;

        [Header("テロップ(FinishTelopAnim)のスケール")]
        [SerializeField] private float telopScaleStart = 3.0f;      // t=0.00
        [SerializeField] private float telopScalePunch = 0.9f;      // t=punchTime
        [SerializeField] private float telopScaleOvershoot = 1.1f;  // t=overshootTime
        [SerializeField] private float telopScaleSettle = 1.0f;     // t=settleTime
        [SerializeField] private float telopScaleGrooveEnd = 1.06f; // t=grooveEndTime
        [SerializeField] private float telopScaleExitEnd = 1.3f;    // t=finishSequenceDuration

        [Header("テロップの内訳時刻(秒。t=0起点の絶対時刻)")]
        [SerializeField] private float alphaInEndTime = 0.03f;
        [SerializeField] private float punchTime = 0.09f;
        [SerializeField] private float overshootTime = 0.15f;
        [SerializeField] private float settleTime = 0.24f;
        [SerializeField] private float grooveEndTime = 1.30f;

        [Header("揺れ(テロップのみ。位置・回転)")]
        [SerializeField] private float shakeStartTime = 0.06f;
        [SerializeField] private float shakeEndTime = 0.30f;
        [SerializeField] private float shakePositionAmplitude = 30f;
        [SerializeField] private float shakeRotationAmplitudeDeg = 5f;
        [SerializeField] private float shakeIntervalSeconds = 0.02f;

        public float BlockerAlphaDuring => blockerAlphaDuring;
        public float BlockerAlphaFadeInEndTime => blockerAlphaFadeInEndTime;
        public float BlockerAlphaFinal => blockerAlphaFinal;
        public float BlockerAlphaFinalStartTime => blockerAlphaFinalStartTime;

        public float TimeBarFlushDuration => timeBarFlushDuration;

        public float FlashAlpha => flashAlpha;
        public float FlashFadeOutEndTime => flashFadeOutEndTime;

        public float TelopScaleStart => telopScaleStart;
        public float TelopScalePunch => telopScalePunch;
        public float TelopScaleOvershoot => telopScaleOvershoot;
        public float TelopScaleSettle => telopScaleSettle;
        public float TelopScaleGrooveEnd => telopScaleGrooveEnd;
        public float TelopScaleExitEnd => telopScaleExitEnd;

        public float AlphaInEndTime => alphaInEndTime;
        public float PunchTime => punchTime;
        public float OvershootTime => overshootTime;
        public float SettleTime => settleTime;
        public float GrooveEndTime => grooveEndTime;

        public float ShakeStartTime => shakeStartTime;
        public float ShakeEndTime => shakeEndTime;
        public float ShakePositionAmplitude => shakePositionAmplitude;
        public float ShakeRotationAmplitudeDeg => shakeRotationAmplitudeDeg;
        public float ShakeIntervalSeconds => shakeIntervalSeconds;
    }
}
