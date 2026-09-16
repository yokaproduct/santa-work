using UnityEngine;

namespace Santa.Game
{
    /// <summary>
    /// 業務提示フェーズ(`[Prompt]`)の演出タイムライン。`10_Screen_GamePlay.md` §12.2 / §12.4。
    /// すべて「演出値」であり、ディレクターが Inspector で自由に調整してよい
    /// (共通仕様 §7.4.1 のレイアウト値とは別枠。演出用ラッパーの scale/位置/回転だけを動かす)。
    ///
    /// タイムラインは3区間: 入り(パンチイン) → グルーヴ → 抜け(ズームスルー)。
    /// 入りと抜けの長さは固定で、グルーヴだけが `GameBalanceSettings.PromptDuration` の増減を吸収する
    /// (§12.2)。
    /// </summary>
    [CreateAssetMenu(menuName = "Santa/Prompt Effect Settings", fileName = "PromptEffectSettings")]
    public class PromptEffectSettings : ScriptableObject
    {
        [Header("区間の長さ(秒)。入り+抜けの残りがグルーヴになる")]
        [SerializeField] private float pushInDuration = 0.22f;
        [SerializeField] private float exitDuration = 0.18f;

        [Header("背景(PromptBackdrop)")]
        [Tooltip("赤は避ける(光過敏性リスク)。赤×緑の組み合わせも作らない(全種目ルール)。" +
                 "★2026-09-16 白に変更(ディレクター指示。文字色との同化対策)。")]
        [SerializeField] private Color backdropColor = Color.white;

        [Header("文字(PromptTextAnim)のスケール")]
        [SerializeField] private float textScaleStart = 2.6f;      // t=0.00
        [SerializeField] private float textScalePunch = 0.9f;      // t=0.08(押し込み)
        [SerializeField] private float textScaleOvershoot = 1.15f; // t=0.14
        [SerializeField] private float textScaleSettle = 1.0f;     // t=pushInDuration(着地=グルーヴ開始)
        [SerializeField] private float textScaleGrooveEnd = 1.06f; // t=promptDuration-exitDuration
        [SerializeField] private float textScaleExitEnd = 1.35f;   // t=promptDuration

        [Header("文字の内訳時刻(pushInDuration内。秒)")]
        [SerializeField] private float punchTime = 0.08f;
        [SerializeField] private float overshootTime = 0.14f;

        [Header("集中線(PromptBurstImage)")]
        [SerializeField] private float burstScaleStart = 0.6f;
        [SerializeField] private float burstScalePunch = 1.1f;
        [SerializeField] private float burstScaleSettle = 1.0f;
        [SerializeField] private float burstAlphaGrooveEnd = 0.5f;
        [Tooltip("グルーヴ中、集中線が回転する速度(度/秒)。")]
        [SerializeField] private float burstRotationDegPerSecond = 30f;

        [Header("フラッシュ(PromptFlash)")]
        [SerializeField] private float flashAlpha = 0.7f;
        [Tooltip("フラッシュが0まで減衰し終わる時刻(秒。t=0起点)。毎秒3回を超えないよう1回だけ。")]
        [SerializeField] private float flashFadeOutEndTime = 0.12f;

        [Header("揺れ(文字のみ。位置・回転)")]
        [SerializeField] private float shakeStartTime = 0.06f;
        [SerializeField] private float shakeEndTime = 0.26f;
        [SerializeField] private float shakePositionAmplitude = 24f;
        [SerializeField] private float shakeRotationAmplitudeDeg = 4f;
        [Tooltip("この間隔ごとに乱数で揺れの向きを変える(秒)。")]
        [SerializeField] private float shakeIntervalSeconds = 0.02f;

        [Header("ビートバンプ(グルーヴ中。音のアクセントに合わせる)")]
        [Tooltip("`[Prompt]` 開始からの秒数。複数入れてもよい。音を聞いてディレクターが調整する。")]
        [SerializeField] private float[] beatTimes = { 0.55f };
        [SerializeField] private float beatBumpAmount = 0.08f;
        [Tooltip("上げにかかる時間+戻すのにかかる時間の合計(秒)。半分ずつ使う。")]
        [SerializeField] private float beatBumpDuration = 0.10f;

        public float PushInDuration => pushInDuration;
        public float ExitDuration => exitDuration;
        public Color BackdropColor => backdropColor;

        public float TextScaleStart => textScaleStart;
        public float TextScalePunch => textScalePunch;
        public float TextScaleOvershoot => textScaleOvershoot;
        public float TextScaleSettle => textScaleSettle;
        public float TextScaleGrooveEnd => textScaleGrooveEnd;
        public float TextScaleExitEnd => textScaleExitEnd;
        public float PunchTime => punchTime;
        public float OvershootTime => overshootTime;

        public float BurstScaleStart => burstScaleStart;
        public float BurstScalePunch => burstScalePunch;
        public float BurstScaleSettle => burstScaleSettle;
        public float BurstAlphaGrooveEnd => burstAlphaGrooveEnd;
        public float BurstRotationDegPerSecond => burstRotationDegPerSecond;

        public float FlashAlpha => flashAlpha;
        public float FlashFadeOutEndTime => flashFadeOutEndTime;

        public float ShakeStartTime => shakeStartTime;
        public float ShakeEndTime => shakeEndTime;
        public float ShakePositionAmplitude => shakePositionAmplitude;
        public float ShakeRotationAmplitudeDeg => shakeRotationAmplitudeDeg;
        public float ShakeIntervalSeconds => shakeIntervalSeconds;

        public float[] BeatTimes => beatTimes;
        public float BeatBumpAmount => beatBumpAmount;
        public float BeatBumpDuration => beatBumpDuration;

        /// <summary>グルーヴ区間の長さ。promptDuration が短すぎる場合でも0未満にはならない(下限0.6秒の想定)。</summary>
        public float GetGrooveDuration(float promptDuration) =>
            Mathf.Max(0f, promptDuration - pushInDuration - exitDuration);
    }
}
