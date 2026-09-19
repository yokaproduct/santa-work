using System.Collections.Generic;
using UnityEngine;

namespace Santa.Game
{
    /// <summary>
    /// 調整用パラメータ。共通仕様 00_共通仕様.md §12。すべて仮値であり、Editorから変更できること。
    ///
    /// ★セッション長(90秒)はここに置かない。GameModeDefinition.sessionDuration が唯一の情報源
    /// (`99_廃止_Screen_ModeSelect.md` §6.1 R1: 「GameSessionControllerはGameBalanceSettings.sessionDurationを
    /// 直接読まない」)。時間無制限モードの拡張性を壊さないための意図的な分離。
    /// </summary>
    [CreateAssetMenu(menuName = "Santa/Game Balance Settings", fileName = "GameBalanceSettings")]
    public class GameBalanceSettings : ScriptableObject
    {
        [Header("フェーズの長さ(秒)")]
        [Tooltip("業務提示。★2026-09-15 ディレクター決定で 0.8→1.5秒(決定ログ §4-6 決-1)。下限0.6秒(演出のグルーヴ区間が0以上になる長さ)。")]
        [SerializeField] private float promptDuration = 1.5f;   // 業務提示
        [SerializeField] private float playDuration = 12.0f;    // 操作(T2)
        [SerializeField] private float judgeEffectDuration = 0.35f; // 判定演出
        [Tooltip("★2026-09-15 新設(決定ログ §4-6 決-4)。終了演出(セッション終了→結果画面)。")]
        [SerializeField] private float finishSequenceDuration = 1.5f;

        [Tooltip("★2026-09-15 新設。終了演出のうち、この秒数(t≥)が経過した最初のフレームでセーブする" +
                 "(共通仕様 §3.9 / §10。衝撃の瞬間のフレーム落ちを避けるため0.25秒待つ)。")]
        [SerializeField] private float finishSaveAtTime = 0.25f;

        [Tooltip("★2026-09-15 新設。終了演出開始時、bgm_gameplayをこの秒数でフェードアウトして停止する。")]
        [SerializeField] private float finishBgmFadeSeconds = 0.15f;

        [Header("カウントダウン(秒)")]
        [SerializeField] private float countdownNormal = 3.0f;
        [SerializeField] private float countdownRetry = 1.5f;

        [Header("カウントダウンのステップ長(秒。30_Overlay_Countdown.md §1.2/§4.1)")]
        [Tooltip("例: 3.0秒÷0.75秒=4ステップ(3→2→1→スタート!)。se_countdownをステップごとに鳴らすタイミングに使う。")]
        [SerializeField] private float countdownNormalStepDuration = 0.75f;
        [Tooltip("例: 1.5秒÷0.5秒=3ステップ(2→1→スタート!)。")]
        [SerializeField] private float countdownRetryStepDuration = 0.5f;

        [Header("画面共通")]
        [Tooltip("★2026-09-17新設(02_Screen_Title.md §4.2)。Screen_Titleに入ってからPlayButton_Time90/" +
                 "SettingsButtonが操作可能になるまでの遅延(秒)。起動直後の連打対策・Resultの「やめる」からの" +
                 "残留タップ対策。")]
        [SerializeField] private float titleInputDelaySeconds = 0.3f;

        [Tooltip("★2026-09-19新設(ディレクター指示: 「ゲーム起動時に、フェードインからスタートする仕様に" +
                 "してください。2秒くらいで設定してみて」)。アプリ起動直後、黒(不透明)から画面が見えてくるまでの" +
                 "秒数。0を指定するとフェードなしで即表示になる。起動時の1回だけ再生される(BootFadeController)。")]
        [SerializeField] private float bootFadeInSeconds = 2.0f;

        [Header("件数・スコア")]
        [SerializeField] private int requiredUnits = 3;
        [SerializeField] private int baseScore = 100;

        [Tooltip("minCombo の降順に探索して最初に一致したものを使う(共通仕様 §5.2)。")]
        [SerializeField]
        private List<ComboTier> comboTiers = new List<ComboTier>
        {
            new ComboTier { minCombo = 10, multiplier = 2.0f },
            new ComboTier { minCombo = 6, multiplier = 1.5f },
            new ComboTier { minCombo = 3, multiplier = 1.2f },
            new ComboTier { minCombo = 0, multiplier = 1.0f },
        };

        [Header("セッション終了付近の挙動")]
        [Tooltip("T1の残りがこの秒数未満なら、新しいミニゲームを開始せず Result へ移る(共通仕様 §3.5 / §14-2)。" +
                 "★2026-09-15: 業務提示が1.5秒になったため推奨値を2.0→2.7秒に変更(=業務提示1.5+最低操作1.2)。" +
                 "この値は promptDuration より大きいこと(業務提示中にT1が尽きないための条件)。")]
        [SerializeField] private float minRemainingTimeToStartNewMicroGame = 2.7f;

        public float PromptDuration => promptDuration;
        public float PlayDuration => playDuration;
        public float JudgeEffectDuration => judgeEffectDuration;
        public float FinishSequenceDuration => finishSequenceDuration;
        public float FinishSaveAtTime => finishSaveAtTime;
        public float FinishBgmFadeSeconds => finishBgmFadeSeconds;
        public float CountdownNormal => countdownNormal;
        public float CountdownRetry => countdownRetry;
        public float CountdownNormalStepDuration => countdownNormalStepDuration;
        public float CountdownRetryStepDuration => countdownRetryStepDuration;
        public float TitleInputDelaySeconds => titleInputDelaySeconds;
        public float BootFadeInSeconds => bootFadeInSeconds;
        public int RequiredUnits => requiredUnits;
        public int BaseScore => baseScore;
        public IReadOnlyList<ComboTier> ComboTiers => comboTiers;
        public float MinRemainingTimeToStartNewMicroGame => minRemainingTimeToStartNewMicroGame;

        /// <summary>現在のコンボ数(加算後)に対応する倍率を返す。</summary>
        public float GetComboMultiplier(int combo)
        {
            float best = 1.0f;
            int bestMin = int.MinValue;
            foreach (var tier in comboTiers)
            {
                if (combo >= tier.minCombo && tier.minCombo > bestMin)
                {
                    bestMin = tier.minCombo;
                    best = tier.multiplier;
                }
            }
            return best;
        }
    }
}
