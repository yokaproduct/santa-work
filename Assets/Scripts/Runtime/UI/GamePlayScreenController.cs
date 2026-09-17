using Santa.Core;
using Santa.Game;
using Santa.MicroGames;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// `Screen_GamePlay` のロジック。`GameSessionController`(常設サービス)のイベントを
    /// HUD・T2バー・進捗ドット・業務提示テキストへ反映するだけで、ゲームロジック自体は持たない
    /// (共通仕様 §4.1 の責務分界)。
    ///
    /// ★このタスクの時点では Overlay_MicroGameIntro / Overlay_Countdown / Part_JudgeEffect /
    /// Part_ScorePopup は未実装(仮置きフェーズのため意図的に省略)。GameSessionController 側は
    /// それらが無くても正しく進行できるように作られている(サービス未登録時は自動スキップ)。
    /// </summary>
    public class GamePlayScreenController : ScreenControllerBase<GamePlayScreenRefs>
    {
        [Header("MVPでは常にこのモードを使う(§13-10-G1 / 99_廃止_Screen_ModeSelect.md §6.1)")]
        [SerializeField] private GameModeDefinition defaultMode;

        private IGameSessionService _session;
        private int _unitsClearedInCurrentMicroGame;

        /// <summary>`[Finish]` 開始時点のT1バーの表示値。閾値未満で終わった場合は0まで流し込む(§13.2)。</summary>
        private float _t1FillAtFinishStart = 1f;

        public GameModeDefinition DefaultMode => defaultMode;

        protected override void OnEnable()
        {
            base.OnEnable();

            _session = ServiceLocator.Get<IGameSessionService>();
            _session.BindMicroGameSlot(Refs.MicroGameSlot);

            // 通常は呼び出し元(ModeSelectの開始ボタン / Resultのもう一回ボタン)が
            // 画面遷移の前に StartSession を呼んでいる。万一まだ始まっていなければ
            // (例: Editorでこの画面を単体テストする場合)ここでフォールバックする。
            if (_session.State == null && defaultMode != null)
            {
                _session.StartSession(defaultMode, retry: false);
            }

            _session.MicroGamePrepared += HandleMicroGamePrepared;
            _session.ScoreOrComboChanged += HandleScoreOrComboChanged;
            _session.UnitClearedGained += HandleUnitClearedGained;
            _session.PhaseChanged += HandlePhaseChanged;
            _session.PausedChanged += HandlePausedChanged;

            ResetHudToInitialState();

            // ★2026-09-15: 業務提示・終了演出の見た目は PromptEffectPlayer / FinishEffectPlayer が
            // 自分の OnEnable() で初期状態(非表示)にする。ここでは重複して触らない。
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_session != null)
            {
                _session.MicroGamePrepared -= HandleMicroGamePrepared;
                _session.ScoreOrComboChanged -= HandleScoreOrComboChanged;
                _session.UnitClearedGained -= HandleUnitClearedGained;
                _session.PhaseChanged -= HandlePhaseChanged;
                _session.PausedChanged -= HandlePausedChanged;
            }
        }

        private void Update()
        {
            if (_session == null || _session.State == null) return;

            var phase = _session.Phase;

            // T1(90秒)。HUDの下辺=区切り線を兼ねる(共通仕様 §6.1.2)。
            if (phase == GameSessionPhase.Finish)
            {
                // ★2026-09-15(§13.2): 閾値未満で終わった場合のみ、開始値→0を一定時間で流し込む。
                // T1=0で終わった場合は開始値が既に0なので見た目は変わらない。
                float flushDuration = Refs.FinishEffectPlayer != null
                    ? Refs.FinishEffectPlayer.TimeBarFlushDuration : 0.2f;
                float flushT = flushDuration > 0f ? Mathf.Clamp01(_session.PhaseElapsed / flushDuration) : 1f;
                Refs.Hud.SessionTimeBarFill.fillAmount = Mathf.Lerp(_t1FillAtFinishStart, 0f, flushT);
            }
            else if (_session.Mode != null && !_session.Mode.IsEndless && _session.Mode.SessionDuration > 0f)
            {
                Refs.Hud.SessionTimeBarFill.fillAmount =
                    Mathf.Clamp01(_session.State.RemainingTime / _session.Mode.SessionDuration);
            }

            // T2(12秒)。画面下部(2026-09-08 モックアップ反映で移動)。
            float t2Fill;
            if (phase == GameSessionPhase.Play && _session.CurrentMicroGamePlayDuration > 0f)
            {
                t2Fill = Mathf.Clamp01(_session.MicroGameTimeRemaining / _session.CurrentMicroGamePlayDuration);
            }
            else if (phase == GameSessionPhase.Prompt || phase == GameSessionPhase.Intro)
            {
                t2Fill = 1f; // 業務提示中は満タンで静止(§3.2)
            }
            else
            {
                t2Fill = 0f; // 判定演出中・終了演出中などは非表示相当
            }
            Refs.MicroGameTimerBar.Fill.fillAmount = t2Fill;
        }

        private void HandleMicroGamePrepared(MicroGameDefinition def)
        {
            string name = def.Id;
            if (ServiceLocator.TryGet<ILocalizationService>(out var loc) && !string.IsNullOrEmpty(def.TitleTextKey))
            {
                var localized = loc.GetString(def.TitleTextKey);
                if (!localized.StartsWith("!")) name = localized; // ローカライズ未登録なら種目idのまま表示する
            }
            Refs.Hud.MicroGameNameText.text = name;

            _unitsClearedInCurrentMicroGame = 0;
            Refs.UnitProgressDots.ResetDots();

            // ★2026-09-15 修正(見-1): se_microgame_start はここではなく [Prompt] 開始の瞬間に
            // GameSessionController が1回だけ鳴らす(§12.2)。ここでも鳴らすと二重再生になっていた。

            if (Refs.PromptText != null)
            {
                Refs.PromptText.text = def.PromptTextKey; // ローカライズ未整備のため暫定的にキーそのものを表示
            }
        }

        private void HandleScoreOrComboChanged()
        {
            Refs.Hud.UnitCountValueText.text = _session.State.ClearedUnits.ToString();
            Refs.Hud.ComboValueText.text = _session.State.Combo.Current.ToString();
            SetComboVisible(_session.State.Combo.Current >= 3);
        }

        private void HandleUnitClearedGained(int gained)
        {
            _unitsClearedInCurrentMicroGame++;
            Refs.UnitProgressDots.SetCleared(_unitsClearedInCurrentMicroGame);
        }

        private void HandlePhaseChanged(GameSessionPhase phase)
        {
            // ★2026-09-15改訂(§12.6): 問題は [Prepare] から [Play] 開始フレームまでずっと隠す。
            // PromptBackdropの表示制御はPromptEffectPlayerに一本化した(GamePlayScreenControllerは
            // 直接SetActiveしない)。HudRoot/BottomReserveは覆わないため、業務提示中もT1(90秒)バーは
            // 点滅せず動き続ける(共通仕様 §3.5)。
            switch (phase)
            {
                case GameSessionPhase.Countdown:
                    // ★2026-09-17(30_Overlay_Countdown.md §5.2 / 02_Screen_Title.md 実装差分):
                    // セッション開始のたびに必ず通るフェーズ。「はじめから」のように画面を作り直さず
                    // 同じ Screen_GamePlay インスタンスで再開する経路(OnEnableが再実行されない)でも、
                    // HUDとPromptBackdropを初期状態に戻すため、OnEnableと同じ初期化をここでも行う。
                    ResetHudToInitialState();
                    break;
                case GameSessionPhase.SelectAndPrepare:
                    Refs.PromptEffectPlayer?.CoverProblem();
                    break;
                case GameSessionPhase.Play:
                    // [Play]開始と同じフレームでハードカット(§12.2-C)。
                    Refs.PromptEffectPlayer?.ResetToIdle();
                    break;
                case GameSessionPhase.Finish:
                    _t1FillAtFinishStart = Refs.Hud.SessionTimeBarFill.fillAmount;
                    Refs.FinishEffectPlayer?.Begin();
                    break;
            }

            RefreshPauseButtonInteractable();
        }

        private void HandlePausedChanged(bool paused)
        {
            // ★2026-09-15(§12.7): ポーズ時は業務提示の演出だけを初期状態へ戻す(背景は出したまま)。
            // Prompt/Intro以外のフェーズでは演出は既に非表示のため無害。
            if (paused)
            {
                Refs.PromptEffectPlayer?.ResetAnimationKeepCover();
            }

            // ★2026-09-17追加: Overlay_Pause実装に伴い、Phaseが変わらないままポーズ⇔復帰する経路
            // (例: [Play]中に自動ポーズ→Overlay_Pauseの「つづける」で復帰)が増えたため、
            // PhaseChangedだけでなくここでも再評価する(31_Overlay_Pause.md §6.2)。
            RefreshPauseButtonInteractable();
        }

        /// <summary>
        /// 共通仕様 §6.1 H4: ポーズボタンが押せるのは[Play]中かつ非ポーズのときだけ。
        /// (ポーズ中は`Overlay_Pause`のBlockerが覆うため物理的にも押せないが、念のため二重に保証する)。
        /// </summary>
        private void RefreshPauseButtonInteractable()
        {
            Refs.Hud.PauseButton.interactable = _session.Phase == GameSessionPhase.Play && !_session.IsPaused;
        }

        private void SetComboVisible(bool visible)
        {
            Refs.Hud.ComboGroup.alpha = visible ? 1f : 0f;
        }

        /// <summary>
        /// HUD・PromptBackdropを初期状態(件数0/コンボ非表示/T1バー満タン/ミニゲーム名は空欄/進捗ドットは空)
        /// に戻す(共通仕様 §4.1、30_Overlay_Countdown.md §5.2「実装の現状との差」)。
        /// 画面生成直後(OnEnable)と、同じ画面インスタンスのままセッションを開始し直す経路
        /// (Countdownフェーズ開始のたび。「はじめから」等)の両方から呼ぶ。
        /// </summary>
        private void ResetHudToInitialState()
        {
            Refs.Hud.UnitCountValueText.text = "0";
            SetComboVisible(false);
            Refs.Hud.SessionTimeBarFill.fillAmount = 1f;
            _t1FillAtFinishStart = 1f;

            // ★カウントダウン中は仮文字「ミニゲーム名」や前回の進捗ドットを見せない。
            Refs.Hud.MicroGameNameText.text = "";
            Refs.UnitProgressDots.ResetDots();
            _unitsClearedInCurrentMicroGame = 0;

            Refs.PromptEffectPlayer?.ResetToIdle();
        }
    }
}
