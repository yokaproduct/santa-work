using System.Collections;
using System.Globalization;
using Santa.Core;
using Santa.Game;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// `Screen_Title`(★2026-09-17 `Screen_ModeSelect` を統合したホーム画面)のロジック。
    /// `02_Screen_Title.md` §4。`PlayButton_Time90` に付いている `StartSessionAndNavigateButton`、
    /// `SettingsButton` に付いている `ScreenNavButton` の遷移処理はそのまま残し(§4.3)、
    /// このクラスは「記録の表示」「初回ヒント/告知の出し分け」「0.3秒の入力遅延」
    /// 「firstLaunchDoneの保存」「二重遷移の防止」「初回の脈動演出」だけを足す。
    /// </summary>
    public class TitleScreenController : ScreenControllerBase<TitleScreenRefs>
    {
        [Tooltip("0.3秒の入力遅延(§4.2)を読む。未設定なら0.3秒扱い。")]
        [SerializeField] private GameBalanceSettings balance;

        [Header("脈動演出(§4.5。演出値)")]
        [SerializeField] private float pulsePeriodSeconds = 1.2f;
        [SerializeField] private float pulseScaleAmplitude = 0.03f;

        private bool _locked;
        private Coroutine _pulseRoutine;

        protected override void OnEnable()
        {
            base.OnEnable();
            _locked = false;

            Refs.PlayButtonTime90.onClick.AddListener(HandlePlayButtonClicked);
            Refs.SettingsButton.onClick.AddListener(HandleSettingsButtonClicked);

            bool firstTime = true;
            if (ServiceLocator.TryGet<ISaveManager>(out var save))
            {
                firstTime = !save.FirstLaunchDone;
                ReflectRecordPanel(save);
            }
            else
            {
                // ★ISaveManager未登録(単体テスト等)でも表示だけは壊れないようにする。
                if (Refs.HighScoreValueText != null) Refs.HighScoreValueText.text = "—";
                if (Refs.BestRankValueText != null) Refs.BestRankValueText.text = ResolveRankTitle(0);
            }

            SetGroupVisible(Refs.FirstTimeHint, firstTime);
            SetGroupVisible(Refs.ComingSoonPanel, !firstTime); // §3.3: 同時に出さない
            SetGroupVisible(Refs.NewContentBadge, false); // MVPは常に非表示

            if (firstTime)
            {
                _pulseRoutine = StartCoroutine(PulsePlayButton());
            }

            float delay = balance != null ? balance.TitleInputDelaySeconds : 0.3f;
            StartCoroutine(DelayedEnableButtons(delay));
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Refs.PlayButtonTime90 != null) Refs.PlayButtonTime90.onClick.RemoveListener(HandlePlayButtonClicked);
            if (Refs.SettingsButton != null) Refs.SettingsButton.onClick.RemoveListener(HandleSettingsButtonClicked);

            StopPulse();
        }

        /// <summary>§3.1 記録表示。「0」ではなく「—」にして0点に見えるのを避ける(未プレイ時)。</summary>
        private void ReflectRecordPanel(ISaveManager save)
        {
            if (save.TotalPlays == 0)
            {
                if (Refs.HighScoreValueText != null) Refs.HighScoreValueText.text = "—";
                if (Refs.BestRankValueText != null) Refs.BestRankValueText.text = ResolveRankTitle(0);
            }
            else
            {
                if (Refs.HighScoreValueText != null)
                {
                    Refs.HighScoreValueText.text = save.HighScore.ToString("N0", CultureInfo.InvariantCulture);
                }
                if (Refs.BestRankValueText != null)
                {
                    Refs.BestRankValueText.text = ResolveRankTitle(save.BestRankIndex);
                }
            }
        }

        /// <summary>
        /// `GameModeDefinition_Time90.RankTable.Ranks[index].titleTextKey` をローカライズして返す。
        /// 範囲外なら0番目にフォールバックする(§3.1)。ローカライズ未整備の間はキーをそのまま返す
        /// (`GamePlayScreenController.HandleMicroGamePrepared` と同じ方針)。
        /// </summary>
        private string ResolveRankTitle(int index)
        {
            var startButton = Refs.PlayButtonTime90 != null
                ? Refs.PlayButtonTime90.GetComponent<StartSessionAndNavigateButton>()
                : null;
            var rankTable = startButton != null && startButton.Mode != null ? startButton.Mode.RankTable : null;
            if (rankTable == null || rankTable.Ranks.Count == 0) return "";

            if (index < 0 || index >= rankTable.Ranks.Count) index = 0;
            string key = rankTable.Ranks[index].titleTextKey;
            if (string.IsNullOrEmpty(key)) return "";

            if (ServiceLocator.TryGet<ILocalizationService>(out var loc))
            {
                var localized = loc.GetString(key);
                if (!localized.StartsWith("!")) return localized;
            }
            return key;
        }

        private static void SetGroupVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            // ★H6/§3.2: RecordPanel・ComingSoonPanel・FirstTimeHintはいずれもタップ不可(表示専用)。
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        /// <summary>§4.5: `localScale` だけを 1.0 ⇔ 1.03 で往復させる(1周期 約1.2秒)。sizeDelta/anchoredPositionには触れない。</summary>
        private IEnumerator PulsePlayButton()
        {
            var target = Refs.PlayButtonTime90.transform;
            float t = 0f;
            float period = Mathf.Max(0.01f, pulsePeriodSeconds);

            while (true)
            {
                t += Time.unscaledDeltaTime;
                float phase = (t % period) / period;
                float wave = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f); // 0→1→0
                float scale = 1f + pulseScaleAmplitude * wave;
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
        }

        private void StopPulse()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }
            if (Refs != null && Refs.PlayButtonTime90 != null)
            {
                Refs.PlayButtonTime90.transform.localScale = Vector3.one;
            }
        }

        /// <summary>§4.2: 画面に入ってから指定秒数、開始ボタンと設定ボタンを非活性にする。</summary>
        private IEnumerator DelayedEnableButtons(float delaySeconds)
        {
            Refs.PlayButtonTime90.interactable = false;
            Refs.SettingsButton.interactable = false;

            if (delaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            if (!_locked)
            {
                Refs.PlayButtonTime90.interactable = true;
                Refs.SettingsButton.interactable = true;
            }
        }

        /// <summary>§4.3①②③④: se_button自体とStartSession/ShowScreenはStartSessionAndNavigateButtonが行う。
        /// ここでは firstLaunchDone の保存・脈動停止・二重遷移防止(両ボタン非活性化)だけを足す。
        /// リスナーの呼ばれる順序に依存しない(H-3)。</summary>
        private void HandlePlayButtonClicked()
        {
            if (_locked) return;
            _locked = true;

            StopPulse();
            Refs.PlayButtonTime90.interactable = false;
            Refs.SettingsButton.interactable = false;

            if (ServiceLocator.TryGet<ISaveManager>(out var save))
            {
                save.FirstLaunchDone = true;
            }
        }

        /// <summary>§4.3: 二重遷移防止のため両ボタンを非活性化する。遷移自体はSettingsButtonのScreenNavButtonが行う。</summary>
        private void HandleSettingsButtonClicked()
        {
            if (_locked) return;
            _locked = true;

            Refs.PlayButtonTime90.interactable = false;
            Refs.SettingsButton.interactable = false;
        }
    }
}
