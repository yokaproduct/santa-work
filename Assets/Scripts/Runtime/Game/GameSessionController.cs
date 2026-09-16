using System;
using System.Collections;
using System.Collections.Generic;
using Santa.Core;
using Santa.MicroGames;
using UnityEngine;

namespace Santa.Game
{
    /// <summary>
    /// 90秒タイマー(T1)・12秒タイマー(T2)・スコア・コンボ・出題順を管理する中核クラス。
    /// 共通仕様 00_共通仕様.md §3 / §4 / §5、`10_Screen_GamePlay.md` §4 / §5 / §12 / §13 の実装。
    ///
    /// 【G1(共通仕様 §13-10-G1)の実装方針】常設サービスとして Services に置く(Screen_GamePlay の
    /// Prefabには載せない)。理由: 画面は都度Instantiate/Destroyされる(ScreenFlowManagerの実装)ため、
    /// Screen_Result へ結果を渡す経路をシンプルに保つには、画面の生存期間から独立させたほうが扱いやすい。
    /// この判断はまだ確定ではなく(§13ではG1は「基盤実装時」に開発チームが決める項目)、
    /// 動作を確認しながら見直してよい。
    ///
    /// 【G2の実装方針】ミニゲームのイベント購読は素朴な C# event(MicroGameBase 側)をそのまま使う。
    /// 【G5の実装方針】T1/T2 は Time.unscaledDeltaTime の累積。Time.timeScale は変更しない
    /// (演出を止めずにゲームだけ止めたいため。ポーズは SetPaused(true) で個別に止める)。
    /// 【G7の実装方針】フェーズ進行は Coroutine で書く。
    ///
    /// ★このクラスは「問題」という概念を一切知らない。知っているのは
    /// OnUnitCleared / OnMissed / OnAllUnitsCleared の3イベントだけ(共通仕様 §4.1)。
    ///
    /// ★2026-09-15 改訂(決定ログ §4-6): 業務提示 0.8→1.5秒 + 演出、終了演出(Finishフェーズ)を新設。
    /// `PhaseElapsed` を公開し、`PromptEffectPlayer`/`FinishEffectPlayer` がそれをサンプリングして
    /// 演出タイムラインを再生する(このクラスはUI要素を一切知らない。§12.4/§13.4)。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameSessionController : MonoBehaviour, IGameSessionService
    {
        [Header("参照")]
        [SerializeField] private GameBalanceSettings balance;
        [SerializeField] private MicroGameCatalog catalog;

        [Header("デバッグ(共通仕様 §13-10 / `10_Screen_GamePlay.md` §7)")]
        [Tooltip("設定すると、出題順の制約を無視してこの種目だけを繰り返す。")]
        [SerializeField] private MicroGameDefinition debugForceMicroGame;
        [SerializeField] private bool debugUseFixedSeed;
        [SerializeField] private int debugFixedSeed;
        [Tooltip("0以下なら balance の値を使う。")]
        [SerializeField] private float debugSessionDurationOverride = -1f;
        [Tooltip("0以下なら balance の値を使う。")]
        [SerializeField] private float debugPlayDurationOverride = -1f;

        private RectTransform _microGameSlot;
        private GameObject _currentMicroGameInstance;
        private MicroGameBase _currentMicroGame;
        private bool _paused;
        private bool _introDismissed;
        private bool _allUnitsCleared;
        private Coroutine _sessionRoutine;

        /// <summary>現在のフェーズ(Prompt / Finish)に入ってからの経過秒。ポーズ中は進まない。§12.4/§13.4。</summary>
        private float _phaseElapsedTime;

        /// <summary>
        /// `[Prompt]` 中にバックグラウンド移行でポーズしたとき true にする。
        /// 復帰(SetPaused(false))後、業務提示を t=0 からやり直すために使う(§12.7)。
        /// </summary>
        private bool _promptNeedsRestart;

        /// <summary>`[Finish]` で既にセーブ済みか。バックグラウンド移行時の即時セーブと二重にならないようにする。</summary>
        private bool _finishSaved;

        /// <summary>`[Finish]` 中にバックグラウンドへ移行したら true。以後は残りの演出を飛ばして即座に結果画面へ(§13.6)。</summary>
        private bool _skipFinishRemainder;

        public SessionState State { get; private set; }
        public GameModeDefinition Mode { get; private set; }
        public GameSessionPhase Phase { get; private set; } = GameSessionPhase.Idle;
        public float MicroGameTimeRemaining { get; private set; }
        public MicroGameDefinition CurrentMicroGameDefinition { get; private set; }
        public float PhaseElapsed => _phaseElapsedTime;
        public float PromptDuration => balance != null ? balance.PromptDuration : 0f;
        public float FinishSequenceDuration => balance != null ? balance.FinishSequenceDuration : 0f;

        /// <summary>
        /// 直近に Finish() を呼んだ理由。Prompt中断(§3.5)のように Finish が一度も
        /// 呼ばれていないミニゲームでは更新されない(前回値が残る/nullのまま)。
        /// テスト・デバッグ用の観測点として公開する
        /// (Instantiate されたミニゲームの実インスタンスはPrefab呼び出し元から直接見えないため)。
        /// </summary>
        public MicroGameFinishReason? LastMicroGameFinishReason { get; private set; }

        public float CurrentMicroGamePlayDuration { get; private set; }
        public SessionResult? LastResult { get; private set; }

        /// <summary>現在ポーズ中か。UI側はこの値だけを信頼すること(ローカルで別途状態を持たない)。</summary>
        public bool IsPaused => _paused;

        public event Action<SessionResult> SessionEnded;
        public event Action<MicroGameDefinition> MicroGamePrepared;
        public event Action ScoreOrComboChanged;
        public event Action<int> UnitClearedGained;
        public event Action<GameSessionPhase> PhaseChanged;
        public event Action<bool> PausedChanged;

        private void Awake()
        {
            ServiceLocator.Register<IGameSessionService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IGameSessionService>();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (Phase == GameSessionPhase.Idle) return;

            // ★2026-09-15 修正(共通仕様 §3.9 / 見-6): 終了演出中はポーズに入らない。
            // 結果は確定済みで、ポーズの「やめる」を選べると確定したスコアを捨てられてしまうため。
            if (Phase == GameSessionPhase.Finish)
            {
                if (pauseStatus) HandleFinishBackgrounded();
                return;
            }

            if (!pauseStatus) return;

            // ★2026-09-15 追加(§12.7): [Prompt]中のバックグラウンド移行は、復帰後に
            // 業務提示をt=0からやり直す。1.42秒の単発SEは途中から再開できないため、鳴らし直す。
            if (Phase == GameSessionPhase.Prompt)
            {
                _promptNeedsRestart = true;
                if (ServiceLocator.TryGet<IAudioManager>(out var audio))
                {
                    audio.StopSe(AudioIds.Se.MicroGameStart);
                }
            }

            SetPaused(true);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (Phase == GameSessionPhase.Idle) return;
            if (Phase == GameSessionPhase.Finish)
            {
                if (!hasFocus) HandleFinishBackgrounded();
                return;
            }
            if (!hasFocus) SetPaused(true);
        }

        private void HandleFinishBackgrounded()
        {
            // ★2026-09-15 追加(§13.6): 終了演出中にバックグラウンドへ移行したら、
            // 未保存ならその場で即座に保存し(OSにアプリを終了されても結果が残るように)、
            // 音を一時停止し、残りの演出は飛ばして即座に結果画面へ向かう。
            if (!_finishSaved)
            {
                SaveResult();
            }
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PauseAll();
            }
            _skipFinishRemainder = true;
        }

        public void BindMicroGameSlot(RectTransform slot)
        {
            _microGameSlot = slot;
        }

        public void StartSession(GameModeDefinition mode, bool retry)
        {
            if (_sessionRoutine != null)
            {
                StopCoroutine(_sessionRoutine);
                CleanUpCurrentMicroGame();
            }

            // ★常設サービスとして残り続けるための必須リセット(§13-10-G1で取りまとめ役から指摘された
            // 「前回のセッション状態が残るリスク」への対応)。特に _paused は「はじめから」
            // (Overlay_Pause → StartSession(retry:true))のように、一時停止したまま次のセッションを
            // 開始する経路が実在するため、ここで戻さないと新セッションのT1が最初から進まなくなる。
            _paused = false;
            _introDismissed = false;
            _allUnitsCleared = false;
            _phaseElapsedTime = 0f;
            _promptNeedsRestart = false;
            _finishSaved = false;
            _skipFinishRemainder = false;
            LastMicroGameFinishReason = null;
            CurrentMicroGameDefinition = null;
            LastResult = null;

            Mode = mode;
            int seed = debugUseFixedSeed ? debugFixedSeed : Environment.TickCount;
            // ★System.Random と UnityEngine.Random が同名のため明示的に修飾する。
            State = new SessionState(new System.Random(seed));
            if (!mode.IsEndless)
            {
                State.RemainingTime = EffectiveSessionDuration(mode);
            }

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlayBgm(AudioIds.Bgm.GamePlay);
            }

            _sessionRoutine = StartCoroutine(RunSession(retry));
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused) return;
            _paused = paused;
            PausedChanged?.Invoke(_paused);

            _currentMicroGame?.SetPaused(paused);

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                if (paused) audio.PauseAll(); else audio.ResumeAll();
            }
        }

        public void QuitWithoutRecording()
        {
            if (_sessionRoutine != null)
            {
                StopCoroutine(_sessionRoutine);
                _sessionRoutine = null;
            }
            CleanUpCurrentMicroGame();
            SetPhase(GameSessionPhase.Idle);

            if (ServiceLocator.TryGet<IScreenFlowService>(out var flow))
            {
                flow.ShowScreen(ScreenId.ModeSelect);
            }
        }

        public void NotifyIntroDismissed()
        {
            _introDismissed = true;
        }

        private float EffectiveSessionDuration(GameModeDefinition mode) =>
            debugSessionDurationOverride > 0f ? debugSessionDurationOverride : mode.SessionDuration;

        private float EffectivePlayDuration() =>
            debugPlayDurationOverride > 0f ? debugPlayDurationOverride : balance.PlayDuration;

        private void SetPhase(GameSessionPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private bool SessionTimeIsUp() => !Mode.IsEndless && State.RemainingTime <= 0f;

        /// <summary>
        /// 1フレームで進めてよい時間の上限(秒)。フレームの一時的な処理落ち・
        /// コルーチン再開直後の異常に大きい Time.unscaledDeltaTime(実測: バッチモードや
        /// WaitForSecondsRealtime直後のフレームで発生しうる)によって、
        /// T1/T2 が1フレームで大きく飛んでしまう事故を防ぐ安全弁。
        /// ★この問題はPlayModeテストで実際に踏んだ(90秒タイマーが1フレームで吹き飛ぶ)。
        /// 実機でもフレーム落ち時に同様の事故が起こりうるため、テストで見つかった不具合として
        /// 本実装に反映した。
        /// </summary>
        private const float MaxFrameDeltaSeconds = 0.25f;

        private static float ClampedUnscaledDeltaTime() => Mathf.Min(Time.unscaledDeltaTime, MaxFrameDeltaSeconds);

        /// <summary>
        /// 一時停止でない間だけ T1 を進める(非無制限モードなら減算、無制限モードなら経過時間を加算)。
        /// </summary>
        private void TickT1(float unscaledDt)
        {
            if (_paused) return;

            unscaledDt = Mathf.Min(unscaledDt, MaxFrameDeltaSeconds);

            if (Mode.IsEndless)
            {
                State.ElapsedTime += unscaledDt;
            }
            else
            {
                State.RemainingTime = Mathf.Max(0f, State.RemainingTime - unscaledDt);
            }
        }

        private IEnumerator RunSession(bool retry)
        {
            yield return RunCountdown(retry);

            bool runFinish = true;

            while (true)
            {
                var def = SelectNextDefinition();
                State.PreviousDefinition = def;

                SetPhase(GameSessionPhase.SelectAndPrepare);
                var aborted = PrepareMicroGame(def);
                if (aborted)
                {
                    // Prepare 自体は必ず1フレームで終わる想定のため、ここで中断することは基本無い。
                    // ★致命的な設定不備(MicroGameSlot未バインド)なので終了演出は出さない。
                    runFinish = false;
                    break;
                }

                // Intro(初出のみ)。PromptBackdrop はUI側が SelectAndPrepare で覆ったまま
                // Intro・Prompt を通して覆い続ける(§12.6。このクラスはUIを直接操作しない)。
                bool introSeen = ServiceLocator.TryGet<ISaveManager>(out var save) && save.IsMicroIntroSeen(def.Id);
                if (!introSeen)
                {
                    SetPhase(GameSessionPhase.Intro);
                    SetPaused(true);
                    yield return ShowIntroAndWait(def);
                    save?.SetMicroIntroSeen(def.Id, true);
                    SetPaused(false);
                    // ★2026-09-15 ディレクター決定(決-3): OK後のカウントダウン(Minimal 1.0秒)は廃止。
                    // すぐ [Prompt] へ進む。
                }

                // Prompt(★1.5秒。この間 T1 は稼働、T2 は未開始。演出は §12。
                // バックグラウンド移行でポーズした場合は t=0 からやり直す(§12.7))。
                bool sessionEndedDuringPrompt = false;
                yield return RunPromptPhase(() => sessionEndedDuringPrompt = true);
                if (sessionEndedDuringPrompt)
                {
                    // §3.5/§4.4: 業務提示を打ち切り、そのミニゲームは無かったことにする(集計に入れない)。
                    CleanUpCurrentMicroGame();
                    break;
                }

                // Play(最大12秒。T1・T2ともに稼働)
                SetPhase(GameSessionPhase.Play);
                _allUnitsCleared = false;
                CurrentMicroGamePlayDuration = EffectivePlayDuration();
                MicroGameTimeRemaining = CurrentMicroGamePlayDuration;
                _currentMicroGame.Begin();

                MicroGameFinishReason reason;
                while (true)
                {
                    if (_allUnitsCleared)
                    {
                        reason = MicroGameFinishReason.AllUnitsCleared;
                        break;
                    }
                    if (SessionTimeIsUp())
                    {
                        reason = MicroGameFinishReason.SessionEnded;
                        break;
                    }
                    if (MicroGameTimeRemaining <= 0f)
                    {
                        reason = MicroGameFinishReason.TimeUp;
                        break;
                    }

                    yield return null;
                    float dt = ClampedUnscaledDeltaTime();
                    TickT1(dt);
                    if (!_paused) MicroGameTimeRemaining -= dt;
                }

                if (reason == MicroGameFinishReason.TimeUp &&
                    ServiceLocator.TryGet<IAudioManager>(out var audioTimeUp))
                {
                    audioTimeUp.PlaySe(AudioIds.Se.TimeUp);
                }

                LastMicroGameFinishReason = reason;
                _currentMicroGame.Finish(reason);
                UnsubscribeMicroGameEvents(_currentMicroGame);

                var summary = _currentMicroGame.GetSummary();
                State.AddMicroGameResult(def.Id, summary);
                State.MissedUnits += summary.UnitsMissed;

                if (reason == MicroGameFinishReason.SessionEnded)
                {
                    // ★2026-09-15(§3.5/§13.2): セッション終了による中断は [Judge] を飛ばして [Finish] へ。
                    // 中断を判定演出で見せると失敗に見えるおそれがあるため。ミニゲームは破棄せず、
                    // 止まった状態のまま [Finish] の最後まで背後に残す。
                    break;
                }

                // Judge(0.35秒。T1は稼働したまま。時間切れでも最後まで再生する。§4.4)
                SetPhase(GameSessionPhase.Judge);
                // ★Part_JudgeEffect はまだ存在しない(Prefab未生成)。中身は保留(共通仕様 §3.8)。
                //   ここでは枠の代わりに、規定の長さだけ待つ(呼び出し契約は Finish→GetSummary→待機→Destroyの順で維持)。
                yield return WaitRealtimeWhileT1Runs(balance.JudgeEffectDuration);

                // Teardown(1フレームで完了させる)
                SetPhase(GameSessionPhase.Teardown);
                State.MicroGamesPlayed++;

                bool thresholdNotMet = !Mode.IsEndless && State.RemainingTime < balance.MinRemainingTimeToStartNewMicroGame;
                if (thresholdNotMet || SessionTimeIsUp())
                {
                    // ★2026-09-15(§4.4): 破棄は終了演出の最後まで遅らせる。ここでは破棄しない。
                    break;
                }

                CleanUpCurrentMicroGame();
            }

            if (runFinish)
            {
                yield return RunFinishPhase();
            }

            yield return EndSessionRoutine();
        }

        /// <summary>
        /// 業務提示フェーズ(§12)。バックグラウンド移行でポーズした場合、復帰後に t=0 からやり直す(§12.7)。
        /// T1が0になったら <paramref name="onSessionEndedDuringPrompt"/> を呼んで即座に抜ける。
        /// </summary>
        private IEnumerator RunPromptPhase(Action onSessionEndedDuringPrompt)
        {
            bool restart;
            do
            {
                restart = false;
                // ★PhaseChangedの購読者が同期的にPhaseElapsedを読むことがあるため、
                // SetPhase()を呼ぶ前に必ずリセットしておく(§12.4/§13.4)。
                _phaseElapsedTime = 0f;
                _promptNeedsRestart = false;
                SetPhase(GameSessionPhase.Prompt);

                if (ServiceLocator.TryGet<IAudioManager>(out var audio))
                {
                    audio.PlaySe(AudioIds.Se.MicroGameStart);
                }

                float promptDuration = balance.PromptDuration;
                while (_phaseElapsedTime < promptDuration)
                {
                    if (SessionTimeIsUp())
                    {
                        onSessionEndedDuringPrompt?.Invoke();
                        yield break;
                    }

                    yield return null;

                    if (_paused)
                    {
                        continue;
                    }

                    if (_promptNeedsRestart)
                    {
                        restart = true;
                        break;
                    }

                    float dt = ClampedUnscaledDeltaTime();
                    TickT1(dt);
                    _phaseElapsedTime += dt;
                }
            } while (restart);
        }

        /// <summary>
        /// 終了演出フェーズ(★2026-09-15 新設。§13)。T1は既に停止済み(呼び出し前提)で、以後動かさない。
        /// この間はポーズに入らない(§13.6)。バックグラウンド移行時は <see cref="HandleFinishBackgrounded"/>
        /// が `_skipFinishRemainder` を立て、残りの演出を打ち切って即座に次へ進む。
        /// </summary>
        private IEnumerator RunFinishPhase()
        {
            // ★PhaseChangedの購読者(FinishEffectPlayer.Begin() 等)が同期的にPhaseElapsedを
            // 読むため、SetPhase()を呼ぶ前に必ずリセットしておく(§13.4)。
            _phaseElapsedTime = 0f;
            _finishSaved = false;
            _skipFinishRemainder = false;
            SetPhase(GameSessionPhase.Finish);

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                // ①ループSE全停止 ②再生中の se_microgame_start があれば停止 ③BGMフェードアウト ④se_finish(§13.2)
                audio.StopAllLoopSe();
                audio.StopSe(AudioIds.Se.MicroGameStart);
                audio.FadeOutBgm(balance.FinishBgmFadeSeconds);
                audio.PlaySe(AudioIds.Se.Finish);
            }

            // ランク計算・SessionResult の作成はここで行う(t=0)。セーブ自体は t≥finishSaveAtTime まで遅らせる。
            ComputeResult();

            float duration = balance.FinishSequenceDuration;
            while (_phaseElapsedTime < duration && !_skipFinishRemainder)
            {
                yield return null;
                if (_skipFinishRemainder) break;

                float dt = ClampedUnscaledDeltaTime();
                _phaseElapsedTime += dt; // ★Finish中はポーズしない(§13.6)。_pausedを見ない。

                if (!_finishSaved && _phaseElapsedTime >= balance.FinishSaveAtTime)
                {
                    SaveResult();
                }
            }

            if (!_finishSaved)
            {
                SaveResult();
            }

            // 背後に残していたミニゲームを、ここで初めて破棄する(§13.2 t=1.50)。
            CleanUpCurrentMicroGame();
        }

        /// <summary>SessionResult を作る(ランク計算・isNewRecordの判定のみ。PlayerPrefsへは書かない)。</summary>
        private void ComputeResult()
        {
            int rankIndex = 0;
            if (Mode.EvaluateRank && Mode.RankTable != null)
            {
                rankIndex = RankEvaluator.Evaluate(Mode.RankTable, State.Score);
            }

            bool isNewRecord = false;
            if (Mode.RecordHighScore && ServiceLocator.TryGet<ISaveManager>(out var save))
            {
                // ★CommitSessionResult() を呼ぶまでは HighScore は更新されないため、
                // ここで比較しても t=finishSaveAtTime で比較しても結果は同じ(§13.2)。
                isNewRecord = State.Score > save.HighScore;
            }

            var perMicroGameCopy = new Dictionary<string, (int cleared, int missed)>(State.PerMicroGame);
            LastResult = new SessionResult(
                Mode, State.Score, rankIndex, State.ClearedUnits, State.MissedUnits,
                State.Combo.Max, isNewRecord, perMicroGameCopy);
        }

        /// <summary>PlayerPrefs への書き込み(1回だけ呼ぶ想定)。</summary>
        private void SaveResult()
        {
            _finishSaved = true;
            if (Mode.RecordHighScore && ServiceLocator.TryGet<ISaveManager>(out var save) && LastResult.HasValue)
            {
                save.CommitSessionResult(State.Score, LastResult.Value.RankIndex, State.ClearedUnits, State.PerMicroGame);
            }
        }

        private bool PrepareMicroGame(MicroGameDefinition def)
        {
            if (_microGameSlot == null)
            {
                Debug.LogError("[GameSessionController] MicroGameSlot が未バインドです。" +
                               "GamePlayScreenController.BindMicroGameSlot を先に呼んでください。");
                return true;
            }

            var instance = Instantiate(def.Prefab.gameObject, _microGameSlot);
            // ★MicroGameSlotのRectTransformには一切触れない。自Prefabのルートは stretch で親に合わせる契約(共通仕様 §4.3)。
            _currentMicroGameInstance = instance;
            _currentMicroGame = instance.GetComponent<MicroGameBase>();
            CurrentMicroGameDefinition = def;

            SubscribeMicroGameEvents(_currentMicroGame);

            var context = new MicroGameContext(
                def,
                balance.RequiredUnits,
                EffectivePlayDuration(),
                State.Rng,
                State.MicroGamesPlayed,
                State.MicroGameStates);

            _currentMicroGame.Prepare(context);
            MicroGamePrepared?.Invoke(def);
            return false;
        }

        private void SubscribeMicroGameEvents(MicroGameBase mg)
        {
            mg.OnUnitCleared += HandleUnitCleared;
            mg.OnMissed += HandleMissed;
            mg.OnAllUnitsCleared += HandleAllUnitsCleared;
        }

        private void UnsubscribeMicroGameEvents(MicroGameBase mg)
        {
            if (mg == null) return;
            mg.OnUnitCleared -= HandleUnitCleared;
            mg.OnMissed -= HandleMissed;
            mg.OnAllUnitsCleared -= HandleAllUnitsCleared;
        }

        private void HandleUnitCleared()
        {
            int combo = State.Combo.Increment();
            int gained = ScoreCalculator.ComputeGain(balance, combo);
            State.Score += gained;
            State.ClearedUnits += 1;

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Correct);
                if (combo >= 3) audio.PlaySe(AudioIds.Se.Combo);
            }

            UnitClearedGained?.Invoke(gained);
            ScoreOrComboChanged?.Invoke();
        }

        private void HandleMissed()
        {
            State.Combo.Reset();

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Wrong);
            }

            ScoreOrComboChanged?.Invoke();
        }

        private void HandleAllUnitsCleared()
        {
            _allUnitsCleared = true;
        }

        private void CleanUpCurrentMicroGame()
        {
            if (_currentMicroGame != null)
            {
                UnsubscribeMicroGameEvents(_currentMicroGame);
            }
            if (_currentMicroGameInstance != null)
            {
                Destroy(_currentMicroGameInstance);
            }
            _currentMicroGameInstance = null;
            _currentMicroGame = null;
            CurrentMicroGameDefinition = null;

            // ★ループSE(se_belt等)の安全弁。通常は各ミニゲームがFinish()で自分のループSEを止めるが、
            // StartSession()からの強制クリーンアップ(Finish()を経由しない経路)でも確実に止まるようにする。
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.StopAllLoopSe();
            }
        }

        private MicroGameDefinition SelectNextDefinition()
        {
            if (debugForceMicroGame != null)
            {
                return debugForceMicroGame;
            }

            var enabled = catalog.GetEnabledDefinitions();
            if (enabled.Count == 0)
            {
                throw new InvalidOperationException("[GameSessionController] MicroGameCatalog に有効な種目がありません。");
            }

            if (State.PreviousDefinition != null && enabled.Count > 1)
            {
                enabled.RemoveAll(d => d == State.PreviousDefinition);
            }

            int index = State.Rng.Next(enabled.Count);
            return enabled[index];
        }

        private IEnumerator RunCountdown(bool retry)
        {
            SetPhase(GameSessionPhase.Countdown);
            float duration = retry ? balance.CountdownRetry : balance.CountdownNormal;
            float stepDuration = retry ? balance.CountdownRetryStepDuration : balance.CountdownNormalStepDuration;

            if (ServiceLocator.TryGet<IScreenFlowService>(out var flow))
            {
                flow.ShowOverlay(OverlayId.Countdown);
            }

            yield return PlayCountdownSteps(duration, stepDuration);

            if (ServiceLocator.TryGet<IScreenFlowService>(out var flow2))
            {
                flow2.HideOverlay(OverlayId.Countdown);
            }
        }

        /// <summary>
        /// カウントダウンを「3・2・1・スタート!」のようなステップに分割し、
        /// 最後の1ステップ(「スタート!」)以外の頭で `se_countdown` を、最後のステップの頭で `se_start` を鳴らす
        /// (30_Overlay_Countdown.md §4.1)。
        /// ★`Overlay_Countdown` 自体(数字の見た目の表示)はまだ実装されていない(意図的に省略中)。
        /// ここでは音の再生タイミングだけを仕様どおりに再現し、合計の待ち時間は既存の
        /// `CountdownNormal` / `CountdownRetry`(duration)を厳密に守る(値そのものは変更しない)。
        /// </summary>
        private IEnumerator PlayCountdownSteps(float duration, float stepDuration)
        {
            // duration ぶんを stepDuration 単位で均等割りする。仕様どおりの値(3.0s/0.75s=4, 1.5s/0.5s=3)
            // ならちょうど割り切れるが、Editorで異なる値に調整されても最低2ステップ(カウント1回+スタート1回)を保証する。
            int totalSteps = Mathf.Max(2, Mathf.RoundToInt(duration / Mathf.Max(stepDuration, 0.0001f)));
            float actualStep = duration / totalSteps;

            IAudioManager audio = ServiceLocator.TryGet<IAudioManager>(out var am) ? am : null;

            float remaining = duration;
            for (int i = 0; i < totalSteps - 1; i++)
            {
                audio?.PlaySe(AudioIds.Se.Countdown);
                yield return new WaitForSecondsRealtime(actualStep);
                remaining -= actualStep;
            }

            audio?.PlaySe(AudioIds.Se.Start);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, remaining));
        }

        private IEnumerator ShowIntroAndWait(MicroGameDefinition def)
        {
            _introDismissed = false;

            // ★実際に表示できたときだけ「閉じるまで待つ」。IScreenFlowServiceが登録されていても、
            // Overlay_MicroGameIntro のPrefab自体が未登録なら ShowOverlay は false を返す。
            // ここを bool で確認せずに待ってしまうと、誰も NotifyIntroDismissed() を呼ばないまま
            // 永久にハングする(Overlay_MicroGameIntro 未実装の間、実際にこの不具合を踏んだ)。
            bool shown = ServiceLocator.TryGet<IScreenFlowService>(out var flow) &&
                         flow.ShowOverlay(OverlayId.MicroGameIntro);
            if (!shown)
            {
                Debug.LogWarning("[GameSessionController] Overlay_MicroGameIntro を表示できなかったため初出カードを自動スキップします。");
                yield break;
            }

            // 初出カードの表示 = se_screen(32_Overlay_MicroGameIntro.md §4.1)。
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Screen);
            }

            // ★タイムアウトなし。タップされるまで待つ(共通仕様 §2.2)。
            yield return new WaitUntil(() => _introDismissed);
            flow.HideOverlay(OverlayId.MicroGameIntro);
        }

        /// <summary>判定演出用。T1を進めながら固定秒数を待つ(セッション終了で打ち切らず最後まで再生する)。
        /// ★2026-09-15 修正(§10-G9): ポーズ中は remaining を減らさない(以前はポーズ中も
        /// 経過扱いになり、業務提示・判定演出の待ち時間がポーズ中に勝手に減っていた)。</summary>
        private IEnumerator WaitRealtimeWhileT1Runs(float duration)
        {
            float remaining = duration;
            while (remaining > 0f)
            {
                yield return null;
                if (_paused) continue;
                float dt = ClampedUnscaledDeltaTime();
                TickT1(dt);
                remaining -= dt;
            }
        }

        private IEnumerator EndSessionRoutine()
        {
            SetPhase(GameSessionPhase.EndSession);

            // ★通常経路では ComputeResult()/SaveResult() が [Finish] で既に済んでいる(§13.2)。
            // ここでは LastResult が無い異常系(致命的な設定不備で Finish をスキップした場合)にだけ
            // フォールバックとして計算する。
            if (!LastResult.HasValue)
            {
                ComputeResult();
                SaveResult();
            }

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlayBgm(AudioIds.Bgm.Result);
            }

            SessionEnded?.Invoke(LastResult.Value);

            if (ServiceLocator.TryGet<IScreenFlowService>(out var flow))
            {
                flow.ShowScreen(ScreenId.Result);
            }

            yield break;
        }
    }
}
