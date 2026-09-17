using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Santa.Core;
using Santa.Game;
using Santa.MicroGames;
using UnityEngine;
using UnityEngine.TestTools;

namespace Santa.Tests
{
    /// <summary>
    /// `GameSessionController` を、画面Prefabを使わずに検証するPlayModeテスト。
    /// 共通仕様 00_共通仕様.md §3〜§5 の確定事項をコードで裏付ける。
    ///
    /// 【検証の限界】ここで検証しているのは GameSessionController 単体の状態機械としての正しさであり、
    /// 実際の Screen_GamePlay Prefab・HUD表示・オーバーレイの見た目・実機のフレームレートは含まない。
    /// </summary>
    public class GameSessionControllerTests
    {
        private GameObject _root;
        private GameSessionController _controller;
        private RectTransform _slot;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("~TestRoot");
            var canvasGo = new GameObject("~Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(_root.transform);
            _slot = (RectTransform)canvasGo.transform;

            var controllerGo = new GameObject("~GameSessionController");
            controllerGo.transform.SetParent(_root.transform);
            _controller = controllerGo.AddComponent<GameSessionController>();
            _controller.BindMicroGameSlot(_slot);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Core.ServiceLocator.Clear();
        }

        private void ConfigureBalanceAndForcedMicroGame(
            MicroGameBase dummy, GameBalanceSettings balance, float sessionDuration,
            bool useFixedSeed = true, int seed = 1)
        {
            var def = TestFactory.CreateDefinition("dummy", dummy);
            ReflectionTestUtil.SetPrivateField(_controller, "balance", balance);
            ReflectionTestUtil.SetPrivateField(_controller, "debugForceMicroGame", def);
            ReflectionTestUtil.SetPrivateField(_controller, "debugUseFixedSeed", useFixedSeed);
            ReflectionTestUtil.SetPrivateField(_controller, "debugFixedSeed", seed);
            ReflectionTestUtil.SetPrivateField(_controller, "debugSessionDurationOverride", -1f);
            ReflectionTestUtil.SetPrivateField(_controller, "debugPlayDurationOverride", -1f);

            var mode = TestFactory.CreateMode(sessionDuration);
            _controller.StartSession(mode, retry: false);
        }

        private static IEnumerator WaitUntilOrFail(System.Func<bool> condition, float timeoutSeconds, string message)
        {
            float elapsed = 0f;
            while (!condition())
            {
                if (elapsed > timeoutSeconds)
                {
                    Assert.Fail($"タイムアウト: {message}");
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AllUnitsCleared_EndsMicroGameImmediately_AndScoresComboCorrectly()
        {
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new[] { DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear });

            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 5f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f); // ★大きくして1本目で必ずEndSessionへ行かせる

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 0.5f);

            yield return WaitUntilOrFail(() => result.HasValue, 5f, "セッションが終了しなかった");

            // 1件目 combo=1→倍率1.0→100 / 2件目 combo=2→1.0→100 / 3件目 combo=3→1.2→120 = 320
            Assert.AreEqual(320, result.Value.Score, "コンボ倍率を含めたスコア計算が仕様(§5.1/§5.2)と一致しない");
            Assert.AreEqual(3, result.Value.ClearedUnits);
            Assert.AreEqual(0, result.Value.MissedUnits);
            Assert.AreEqual(3, result.Value.MaxCombo);
        }

        [UnityTest]
        public IEnumerator TimeUp_IsNotTreatedAsFailure_ComboAndScoreArePreserved()
        {
            var dummy = TestFactory.CreateDummyTemplate();
            // 2件だけ正解し、3件目に届かないまま時間切れにする
            dummy.ConfigureScript(new[] { DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear });

            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 0.15f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 0.5f);

            yield return WaitUntilOrFail(() => result.HasValue, 5f, "セッションが終了しなかった");

            // ★注意: dummy は Instantiate() 元のテンプレートであり、GameSessionController が実際に
            //   操作するのは Instantiate() で複製されたクローン。テンプレート側のフィールドは
            //   [SerializeField] を付けない限りクローンへ引き継がれず、逆にクローン側の実行結果は
            //   テンプレートへ反映されない。そのため状態の観測は controller 経由で行う。
            Assert.AreEqual(MicroGameFinishReason.TimeUp, _controller.LastMicroGameFinishReason,
                "時間切れなのに Finish の理由が TimeUp になっていない");
            Assert.AreEqual(2, result.Value.ClearedUnits);
            Assert.AreEqual(0, result.Value.MissedUnits, "時間切れは誤答として数えてはならない(共通仕様 §3.4)");
            Assert.AreEqual(2, result.Value.MaxCombo, "時間切れでコンボが失われている(継続するのが仕様)");
            // 100 + 100 = 200
            Assert.AreEqual(200, result.Value.Score);
        }

        [UnityTest]
        public IEnumerator Missed_ResetsCombo_ButDoesNotReduceScoreOrClearedUnits()
        {
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new[]
            {
                DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear, // combo=2
                DummyMicroGame.Step.Miss,                              // combo=0にリセット
            });

            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 0.15f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 0.5f);

            yield return WaitUntilOrFail(() => result.HasValue, 5f, "セッションが終了しなかった");

            Assert.AreEqual(2, result.Value.ClearedUnits);
            Assert.AreEqual(1, result.Value.MissedUnits);
            Assert.AreEqual(2, result.Value.MaxCombo, "誤答までに達した最大コンボ(2)が記録されていない");
            Assert.AreEqual(200, result.Value.Score, "誤答はスコアを減らしても増やしてもならない");
        }

        [UnityTest]
        public IEnumerator ComboMultiplierTiers_MatchGameBalanceSpec()
        {
            // 90秒モードの前提を借りつつ、1ミニゲームで常に3件クリアするダミーを連続で走らせ、
            // 蓄積コンボ(ミニゲームをまたいで継続する。共通仕様 §5.3)に対する加点が
            // 共通仕様 §5.2 の表(0-2連=1.0/3-5連=1.2/6-9連=1.5/10連〜=2.0)と一致するかを検証する。
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new[] { DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear });

            var balance = TestFactory.CreateBalance(promptDuration: 0.01f, playDuration: 5f, judgeEffectDuration: 0.01f,
                countdownNormal: 0.01f, minRemainingTimeToStartNewMicroGame: 0.05f);

            var gains = new List<int>();
            _controller.UnitClearedGained += g => gains.Add(g);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            // 12件(=4ミニゲーム分)クリアできるだけの十分なセッション長を与える。
            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 3.0f);

            yield return WaitUntilOrFail(() => result.HasValue || gains.Count >= 12, 10f, "十分な件数が成立しなかった");

            Assert.GreaterOrEqual(gains.Count, 10, "コンボ倍率の全段(10連〜)を検証するには最低10件必要");

            int[] expected =
            {
                100, 100, 120, 120, 120, 150, 150, 150, 150, 200,
            };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], gains[i], $"{i + 1}件目の加点がコンボ倍率表と一致しない");
            }
        }

        [UnityTest]
        public IEnumerator M5StyleGrouping_RequiresNoSpecialCaseInGameSessionController()
        {
            // M5(ラッピング)は「3問正解=1件100点」に内部で丸める(共通仕様 §4.5)。
            // GameSessionController 側はそれを一切知らないことを、
            // 3問ぶんの生タップを内部でグルーピングするダミーで検証する。
            var groupedGo = new GameObject("~DummyGrouped", typeof(RectTransform));
            groupedGo.SetActive(false);
            var grouped = groupedGo.AddComponent<DummyGroupedMicroGame>();

            // 箱1で誤答 → ループ1は0件 / ループ2・3は成立 → 200点(2件)になるはず(共通仕様 §4.5 の具体例)。
            grouped.ConfigureRawTaps(new[]
            {
                false, true, true, // ループ1: 誤答を含む → 0件
                true, true, true,  // ループ2: 全問正解 → 1件
                true, true, true,  // ループ3: 全問正解 → 1件
            });

            var balance = TestFactory.CreateBalance(promptDuration: 0.01f, playDuration: 5f, judgeEffectDuration: 0.01f,
                countdownNormal: 0.01f, minRemainingTimeToStartNewMicroGame: 100f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(grouped, balance, sessionDuration: 0.5f);

            yield return WaitUntilOrFail(() => result.HasValue, 5f, "セッションが終了しなかった");

            Assert.AreEqual(2, result.Value.ClearedUnits, "M5相当のグルーピングで期待される成立件数(2件)と一致しない");
            Assert.AreEqual(1, result.Value.MissedUnits, "誤答を含む1ループぶんの Missed が計上されていない");
            // 1件目 combo=1→100 / 2件目 combo=2→100 = 200
            Assert.AreEqual(200, result.Value.Score, "M5相当のグルーピングでもスコア式は通常どおり適用されるべき");
        }

        [UnityTest]
        public IEnumerator SessionEndingDuringPrompt_MicroGameIsExcludedFromSummary()
        {
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new[] { DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear });

            // promptDuration より短いセッション長にして、Promptの最中にT1が尽きる状況を作る。
            var balance = TestFactory.CreateBalance(promptDuration: 1.0f, playDuration: 5f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 0.01f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 0.3f);

            yield return WaitUntilOrFail(() => result.HasValue, 5f, "セッションが終了しなかった");

            Assert.IsNull(_controller.LastMicroGameFinishReason,
                "Promptの最中にT1が尽きた場合、そのミニゲームには Finish を呼んではならない(共通仕様 §3.5/§4.4)");
            Assert.AreEqual(0, result.Value.ClearedUnits);
            Assert.AreEqual(0, result.Value.Score);
        }

        [UnityTest]
        public IEnumerator SessionEndingDuringPlay_FinishesWithSessionEndedReason_NotAFailure()
        {
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]); // 何もクリアしない(時間切れ相当の挙動)

            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 5f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 0.01f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            // Prompt(0.02) を終えて Play に入った直後にT1が尽きるよう調整。
            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 0.1f);

            yield return WaitUntilOrFail(() => result.HasValue, 5f, "セッションが終了しなかった");

            Assert.AreEqual(MicroGameFinishReason.SessionEnded, _controller.LastMicroGameFinishReason);
            Assert.AreEqual(0, result.Value.ClearedUnits);
            Assert.AreEqual(0, result.Value.MissedUnits, "セッション終了による中断は誤答ではない(共通仕様 §3.4)");
        }

        [Test]
        public void StartSession_ResetsPausedFlag_EvenIfPreviousSessionWasLeftPaused()
        {
            // 再発防止テスト: 「はじめから」(Overlay_Pause → StartSession(retry:true)) は
            // ポーズ中(_paused == true)のまま呼ばれうる。GameSessionController は常設サービスなので
            // (§13-10-G1)、前回の一時停止状態を新セッション開始時にリセットしないと、
            // 新セッションのT1が最初から一切進まなくなる(実際にこの不具合を作り込みかけて発見した)。
            //
            // ★注意: 最初はセッション完走を待つ [UnityTest] として書いたが、Intro表示処理
            // (ISaveManager未登録時は毎回「初出」扱いになり SetPaused(true)→SetPaused(false) が
            // 自動的に走る)が偶然この不具合を覆い隠してしまい、意図せず「常に成功する」テストに
            // なっていた(実際に不具合を作り込んだ状態でこのテストが誤って成功することを確認済み)。
            // そのため private フィールドを直接検査する形に書き直し、副作用の影響を受けないようにした。
            var dummy = TestFactory.CreateDummyTemplate();
            var balance = TestFactory.CreateBalance();

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);
            _controller.SetPaused(true);
            Assert.IsTrue(ReflectionTestUtil.GetPrivateField<bool>(_controller, "_paused"));

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            Assert.IsFalse(ReflectionTestUtil.GetPrivateField<bool>(_controller, "_paused"),
                "StartSession が前回の一時停止状態をリセットしていない(T1が最初から進まなくなる重大な不具合)");
        }

        [UnityTest]
        public IEnumerator FocusLossAutoPause_CanBeRecoveredWithASinglePauseButtonPress_AndSessionCompletes()
        {
            // 実プレイで再現した不具合の再現テスト:
            // Editor上でGame view以外(Inspector等)をクリックすると OnApplicationFocus(false) が飛び、
            // GameSessionController が自動的にポーズする(共通仕様 §2.4)。
            // 90秒間の実プレイ中にディレクターが何かをクリックする可能性は極めて高く、
            // 「テストは成功しているのに実プレイでは必ず詰まる」典型例だった。
            //
            // 以前の PauseButtonController はローカルに _paused を持っていたため、
            // 自動ポーズ後に「ポーズボタンを1回押す」という自然な操作をしても
            // ローカル状態とズレて再開しなかった。修正後は IGameSessionService.IsPaused を
            // 唯一の真実として読む。
            // ★2026-09-17: Overlay_Pause実装に伴い、実際のPauseButtonControllerは
            // RequestPause()(ポーズに入るだけ)しか呼ばなくなり、再開はOverlay_Pauseの
            // 「つづける」(ResumeFromPause)からのみ行われるようになった。
            // このテストはその変更以前からある「状態機械としての正しさ」の下位テストとして、
            // 低レベルのSetPaused直接トグルでも自動ポーズから復帰できることを引き続き確認する
            // (実際のボタンの配線は PauseOverlayControllerTests 等、別の検証範囲)。
            var dummy = TestFactory.CreateDummyTemplate();
            // ★2026-09-15 修正: 何も正解させない(空の台本)。以前は Begin() 直後に3件まとめて
            // 成立させていたが、即座に成立すると Judge→Teardown→[Finish](終了演出。§3.9)まで
            // 一瞬で進んでしまい、「Playフェーズで一時停止する」という本テストの前提が
            // [Finish] フェーズ(意図的にポーズしない。§13.6)にずれてテストが不安定になっていた。
            // 空の台本なら playDuration(2f) の TimeUp で終わるまで確実に [Play] のままになる
            // (★stepIntervalSecondsを使う代替案は、Instantiateされたクローンが非アクティブな
            // テンプレートの複製で inactive のままのため StartCoroutine が失敗し不採用)。
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);

            var balance = TestFactory.CreateBalance(promptDuration: 0.05f, playDuration: 2f, judgeEffectDuration: 0.05f,
                countdownNormal: 0.05f, minRemainingTimeToStartNewMicroGame: 100f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 1.0f);

            // Playフェーズに入るまで少し待つ(Countdown + Prompt ぶん)。
            yield return new WaitForSecondsRealtime(0.2f);

            // ★実際にUnityが呼ぶのと同じ非公開メソッドを呼び、「Game view からフォーカスが外れた」を再現する。
            InvokePrivate(_controller, "OnApplicationFocus", false);
            Assert.IsTrue(_controller.IsPaused, "OnApplicationFocus(false) で自動ポーズしていない");

            float remainingAtPauseTime = _controller.State.RemainingTime;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(remainingAtPauseTime, _controller.State.RemainingTime,
                "ポーズ中なのにT1が進んでしまっている");

            // ★修正後の PauseButtonController が実際に行うのと同じ1回の呼び出し。
            _controller.SetPaused(!_controller.IsPaused);
            Assert.IsFalse(_controller.IsPaused,
                "ポーズボタンを1回押しても再開しない(ローカル状態とのズレの再発)");

            yield return WaitUntilOrFail(() => result.HasValue, 5f,
                "自動ポーズからの復帰後、セッションが完走しなかった(実プレイで再現した不具合の再発)");
        }

        // ================= ★2026-09-17 追加: Overlay_Pause(31_Overlay_Pause.md)関連のテスト =================
        //
        // ★検証の限界: ここでは実際の Overlay_Pause.prefab / PauseOverlayController は使わず、
        // GameSessionController が正しいタイミングで ShowOverlay(Pause) / ResumeFromPause / StartSession /
        // QuitWithoutRecording を呼び分けること(状態機械としての正しさ)だけを検証する。
        // ボタンの実配線(PauseOverlayController → IGameSessionService)は
        // GamePlaySceneIntegrationTests.cs のような実シーン結合テストの範囲外にとどまっている
        // (取りまとめ役への報告事項)。

        [UnityTest]
        public IEnumerator AutoPause_DuringCountdown_ShowsPauseOverlay_AndResumeFromPauseCompletesSession()
        {
            var fakeFlow = new FakeScreenFlowService();
            ServiceLocator.Register<IScreenFlowService>(fakeFlow);
            // ★FakeScreenFlowService.ShowOverlayは常にtrueを返すため、ISaveManager未登録のままだと
            // 初出カード待ち(WaitUntil(_introDismissed))で永久にハングする。既読扱いにして回避する。
            ServiceLocator.Register<ISaveManager>(new FakeSaveManager());

            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);

            var balance = TestFactory.CreateBalance(promptDuration: 0.05f, playDuration: 3f, judgeEffectDuration: 0.05f,
                countdownNormal: 0.3f, countdownRetry: 0.05f, minRemainingTimeToStartNewMicroGame: 100f);
            ReflectionTestUtil.SetPrivateField(balance, "countdownNormalStepDuration", 0.1f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            yield return WaitUntilOrFail(() => _controller.Phase == GameSessionPhase.Countdown, 3f,
                "[Countdown] フェーズに入らなかった");

            InvokePrivate(_controller, "OnApplicationPause", true);

            Assert.IsTrue(_controller.IsPaused, "[Countdown]中の背景移行で自動ポーズしていない");
            CollectionAssert.Contains(fakeFlow.ShowOverlayCalls, OverlayId.Pause,
                "[Countdown]中の自動ポーズでOverlay_Pauseが表示されなかった");

            _controller.ResumeFromPause();

            yield return WaitUntilOrFail(() => !_controller.IsPaused, 3f,
                "「つづける」後もポーズが解除されなかった([Countdown]中の自動ポーズから復帰できない不具合の再発。" +
                "30_Overlay_Countdown.md §7-5)");
            CollectionAssert.Contains(fakeFlow.HideOverlayCalls, OverlayId.Pause,
                "「つづける」でOverlay_Pauseが閉じられなかった");

            yield return WaitUntilOrFail(() => result.HasValue, 5f,
                "[Countdown]中のポーズから復帰後、セッションが完走しなかった");
        }

        [UnityTest]
        public IEnumerator AutoPause_DuringPrompt_ShowsPauseOverlay_AndResumeRestartsPromptAndCompletesSession()
        {
            var fakeFlow = new FakeScreenFlowService();
            ServiceLocator.Register<IScreenFlowService>(fakeFlow);
            ServiceLocator.Register<ISaveManager>(new FakeSaveManager()); // ★初出カード待ちのハング回避(上記コメント参照)

            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);

            var balance = TestFactory.CreateBalance(promptDuration: 1.0f, playDuration: 3f, judgeEffectDuration: 0.05f,
                countdownNormal: 0.02f, countdownRetry: 0.05f, minRemainingTimeToStartNewMicroGame: 100f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            yield return WaitUntilOrFail(() => _controller.Phase == GameSessionPhase.Prompt, 3f,
                "[Prompt] フェーズに入らなかった");

            // 業務提示が少し進んだ状態でバックグラウンドへ移行する。
            yield return new WaitForSecondsRealtime(0.2f);
            InvokePrivate(_controller, "OnApplicationPause", true);

            Assert.IsTrue(_controller.IsPaused, "[Prompt]中の背景移行で自動ポーズしていない");
            CollectionAssert.Contains(fakeFlow.ShowOverlayCalls, OverlayId.Pause,
                "[Prompt]中の自動ポーズでOverlay_Pauseが表示されなかった");

            _controller.ResumeFromPause();

            yield return WaitUntilOrFail(() => !_controller.IsPaused, 3f,
                "「つづける」後もポーズが解除されなかった");

            // §12.7: 業務提示はt=0からやり直すため、[Prompt]をもう一度通ってから[Play]へ進むはず。
            yield return WaitUntilOrFail(() => _controller.Phase == GameSessionPhase.Play, 3f,
                "「つづける」後、業務提示が再開されず[Play]へ進まなかった(t=0からやり直す仕様。§12.7)");

            yield return WaitUntilOrFail(() => result.HasValue, 5f,
                "[Prompt]中のポーズから復帰後、セッションが完走しなかった");
        }

        [UnityTest]
        public IEnumerator AutoPause_DuringPlay_ShowsPauseOverlay_AndResumeContinuesSession()
        {
            var fakeFlow = new FakeScreenFlowService();
            ServiceLocator.Register<IScreenFlowService>(fakeFlow);
            ServiceLocator.Register<ISaveManager>(new FakeSaveManager()); // ★初出カード待ちのハング回避(上記コメント参照)

            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);

            var balance = TestFactory.CreateBalance(promptDuration: 0.05f, playDuration: 2f, judgeEffectDuration: 0.05f,
                countdownNormal: 0.02f, countdownRetry: 0.05f, minRemainingTimeToStartNewMicroGame: 100f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 1.0f);

            yield return WaitUntilOrFail(() => _controller.Phase == GameSessionPhase.Play, 3f,
                "[Play] フェーズに入らなかった");

            // ★手動ポーズ(HUDのPauseButtonController が呼ぶ)と同じ入口を使う。
            _controller.RequestPause();

            Assert.IsTrue(_controller.IsPaused, "RequestPause() でポーズしていない");
            CollectionAssert.Contains(fakeFlow.ShowOverlayCalls, OverlayId.Pause,
                "手動ポーズでOverlay_Pauseが表示されなかった");

            float remainingAtPauseTime = _controller.State.RemainingTime;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(remainingAtPauseTime, _controller.State.RemainingTime, "ポーズ中なのにT1が進んでしまっている");

            _controller.ResumeFromPause();

            yield return WaitUntilOrFail(() => !_controller.IsPaused, 3f, "「つづける」後もポーズが解除されなかった");

            yield return WaitUntilOrFail(() => result.HasValue, 5f,
                "[Play]中のポーズから復帰後、セッションが完走しなかった");
        }

        [Test]
        public void RequestPause_WhileAlreadyPauseOverlayIsVisible_DoesNotShowOverlayTwice()
        {
            // ★PA-6再発防止テスト: Overlay_Pause表示中に再度ポーズ要求が来ても二重に処理しない。
            var fakeFlow = new FakeScreenFlowService();
            ServiceLocator.Register<IScreenFlowService>(fakeFlow);

            var dummy = TestFactory.CreateDummyTemplate();
            var balance = TestFactory.CreateBalance();
            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            _controller.RequestPause();
            Assert.AreEqual(1, fakeFlow.ShowOverlayCalls.Count, "1回目のRequestPauseでOverlay_Pauseが表示されるべき");

            _controller.RequestPause();
            InvokePrivate(_controller, "OnApplicationPause", true);

            Assert.AreEqual(1, fakeFlow.ShowOverlayCalls.Count,
                "Overlay_Pause表示中に再度ポーズ要求が来ても、二重にShowOverlayが呼ばれてはならない(PA-6)");
        }

        [Test]
        public void RestartFromPause_ResetsSessionState()
        {
            // 31_Overlay_Pause.md §4.2「はじめから」: PauseOverlayController は
            // Overlay_Pauseを閉じてから session.StartSession(session.Mode, retry: true) を呼ぶ。
            // ここではその2手目(StartSessionの再初期化)がポーズ中に呼ばれても
            // セッション状態を正しく初期化することを検証する。
            var fakeFlow = new FakeScreenFlowService();
            ServiceLocator.Register<IScreenFlowService>(fakeFlow);

            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new[] { DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear });
            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 5f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f);

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            _controller.RequestPause();
            Assert.IsTrue(_controller.IsPaused);

            // 「はじめから」相当: PauseOverlayController が行う2手をそのまま再現する。
            if (ServiceLocator.TryGet<IScreenFlowService>(out var flow))
            {
                flow.HideOverlay(OverlayId.Pause);
            }
            _controller.StartSession(_controller.Mode, retry: true);

            Assert.IsFalse(_controller.IsPaused, "「はじめから」はポーズ状態を引き継いではならない(再発防止。§13-10-G1)");
            Assert.AreEqual(0, _controller.State.Score, "「はじめから」で前回のスコアがリセットされていない");
            Assert.AreEqual(0, _controller.State.ClearedUnits);
            Assert.AreEqual(GameSessionPhase.Countdown, _controller.Phase, "「はじめから」はCountdownフェーズからやり直すはず");
        }

        [Test]
        public void QuitFromPause_NavigatesToTitle_WithoutRecording()
        {
            var fakeFlow = new FakeScreenFlowService();
            ServiceLocator.Register<IScreenFlowService>(fakeFlow);
            var fakeSave = new FakeSaveManager();
            ServiceLocator.Register<ISaveManager>(fakeSave);

            var dummy = TestFactory.CreateDummyTemplate();
            var balance = TestFactory.CreateBalance();
            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            _controller.RequestPause();
            Assert.IsTrue(_controller.IsPaused);

            _controller.QuitWithoutRecording();

            Assert.AreEqual(GameSessionPhase.Idle, _controller.Phase, "「やめる」後はIdleフェーズに戻るべき");
            Assert.AreEqual(ScreenId.Title, fakeFlow.LastShownScreen, "「やめる」は Screen_Title へ遷移するべき(決定ログ §4-10)");
            Assert.IsFalse(fakeSave.CommitCalled, "「やめる」はスコア・統計・ハイスコアを一切記録してはならない(共通仕様 §2.4)");
        }

        [UnityTest]
        public IEnumerator Finish_DoesNotShowPauseOverlay_OnApplicationPause()
        {
            // 13.6: 終了演出中はOverlay_Pauseを一切出さない(結果は確定済みで、
            // 「やめる」を選べると確定したスコアを捨てられてしまうため)。
            var fakeFlow = new FakeScreenFlowService();
            ServiceLocator.Register<IScreenFlowService>(fakeFlow);
            ServiceLocator.Register<ISaveManager>(new FakeSaveManager()); // ★初出カード待ちのハング回避(上記コメント参照)

            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);

            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 0.05f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f, finishSequenceDuration: 1.0f);

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 0.1f);

            yield return WaitUntilOrFail(() => _controller.Phase == GameSessionPhase.Finish, 5f,
                "[Finish] フェーズに入らなかった");

            InvokePrivate(_controller, "OnApplicationPause", true);

            Assert.IsFalse(_controller.IsPaused, "終了演出中に自動ポーズしてしまっている(§13.6)");
            CollectionAssert.DoesNotContain(fakeFlow.ShowOverlayCalls, OverlayId.Pause,
                "終了演出中にOverlay_Pauseを表示してしまっている(§13.6)");
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, $"メソッドが見つかりません: {methodName}");
            method.Invoke(target, args);
        }

        // ================= ★2026-09-15 追加: 終了演出(Finishフェーズ)関連のテスト =================

        [UnityTest]
        public IEnumerator SessionEnd_PassesThroughFinishPhase_BeforeSessionEndedFires()
        {
            // 共通仕様 §3.9 / `10_Screen_GamePlay.md` §13: Result へ行く前に必ず [Finish] を通る。
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]); // 何も正解しない

            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 0.05f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f, finishSequenceDuration: 0.3f);

            var phases = new List<GameSessionPhase>();
            _controller.PhaseChanged += p => phases.Add(p);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 0.1f);

            yield return WaitUntilOrFail(() => result.HasValue, 5f, "セッションが終了しなかった");

            Assert.Contains(GameSessionPhase.Finish, phases, "[Finish] フェーズを一度も通っていない(共通仕様 §3.9)");
            int finishIndex = phases.LastIndexOf(GameSessionPhase.Finish);
            int endSessionIndex = phases.LastIndexOf(GameSessionPhase.EndSession);
            Assert.Less(finishIndex, endSessionIndex, "[Finish] が [EndSession] より後に来ている(順序が仕様と逆)");
        }

        [UnityTest]
        public IEnumerator Finish_TakesAtLeastConfiguredDuration()
        {
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);

            const float finishDuration = 0.3f;
            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 0.05f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f, finishSequenceDuration: finishDuration);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            float start = Time.realtimeSinceStartup;
            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 0.1f);

            yield return WaitUntilOrFail(() => result.HasValue, 5f, "セッションが終了しなかった");
            float elapsed = Time.realtimeSinceStartup - start;

            Assert.GreaterOrEqual(elapsed, finishDuration,
                "終了演出(finishSequenceDuration)ぶんの時間が経過する前に結果が確定している");
        }

        [UnityTest]
        public IEnumerator Finish_DoesNotEnterPause_EvenOnApplicationPauseCallback()
        {
            // 共通仕様 §3.9 / §13.6: 終了演出中はポーズに入らない。
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);

            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 0.05f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f, finishSequenceDuration: 1.0f);

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 0.1f);

            yield return WaitUntilOrFail(() => _controller.Phase == GameSessionPhase.Finish, 5f,
                "[Finish] フェーズに入らなかった");

            InvokePrivate(_controller, "OnApplicationPause", true);

            Assert.IsFalse(_controller.IsPaused,
                "終了演出中に OnApplicationPause(true) でポーズしてしまっている(§13.6の要件に反する)");
        }

        [UnityTest]
        public IEnumerator JudgePhase_DoesNotShortenWhilePaused()
        {
            // ★2026-09-15 修正(共通仕様 §10-G9)の再発防止テスト。
            // 以前は WaitRealtimeWhileT1Runs がポーズ中も残り時間を減らしており、
            // 業務提示・判定演出の待ち時間がポーズ中に勝手に減っていた。
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new[] { DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear });

            const float judgeDuration = 0.5f;
            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 5f, judgeEffectDuration: judgeDuration,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f);

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            yield return WaitUntilOrFail(() => _controller.Phase == GameSessionPhase.Judge, 5f,
                "[Judge] フェーズに入らなかった");

            // Judgeに入った直後にポーズし、judgeDurationより十分長く待ってから解除する。
            _controller.SetPaused(true);
            yield return new WaitForSecondsRealtime(judgeDuration + 0.3f);

            Assert.AreEqual(GameSessionPhase.Judge, _controller.Phase,
                "ポーズ中に [Judge] が終わってしまった(待ち時間がポーズ中に減っている)");

            _controller.SetPaused(false);
            yield return WaitUntilOrFail(() => _controller.Phase != GameSessionPhase.Judge, 3f,
                "ポーズ解除後も [Judge] が終わらない");
        }

        [UnityTest]
        public IEnumerator Prompt_PlaysMicroGameStartSeExactlyOnce_NotTwice()
        {
            // ★2026-09-15 修正(決定ログ §4-6 見-1)の再発防止テスト。
            // 以前は GamePlayScreenController.HandleMicroGamePrepared でも同じSEを鳴らしており、
            // [Prepare] と [Prompt] の2回、1.42秒の音が重なって再生されていた。
            var fakeAudio = new FakeAudioManager();
            ServiceLocator.Register<IAudioManager>(fakeAudio);

            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new[] { DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear });

            var balance = TestFactory.CreateBalance(promptDuration: 0.05f, playDuration: 3f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.02f, minRemainingTimeToStartNewMicroGame: 100f);

            SessionResult? result = null;
            _controller.SessionEnded += r => result = r;

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 1.0f);

            yield return WaitUntilOrFail(() => result.HasValue, 5f, "セッションが終了しなかった");

            int count = fakeAudio.SeCallCounts.TryGetValue(Santa.Core.AudioIds.Se.MicroGameStart, out var c) ? c : 0;
            Assert.AreEqual(1, count, "se_microgame_start が1ミニゲームにつき1回だけ再生されていない(二重再生の再発)");
        }

        // ================= ★2026-09-17 追加: カウントダウン(30_Overlay_Countdown.md)関連のテスト =================

        [UnityTest]
        public IEnumerator Countdown_Normal_StepsThroughFourStepsInOrder_WithStepCountExposed()
        {
            // countdownNormal(0.12) ÷ countdownNormalStepDuration(0.03) = 4ステップ(3→2→1→スタート!)。
            // 本番の3.0秒/0.75秒と同じ比率(4ステップ)を保ったまま高速化している。
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);

            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 5f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.12f, minRemainingTimeToStartNewMicroGame: 100f);
            ReflectionTestUtil.SetPrivateField(balance, "countdownNormalStepDuration", 0.03f);

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            var seenSteps = new List<int>();
            int? lastCount = null;
            yield return WaitUntilOrFail(() =>
            {
                if (_controller.Phase != GameSessionPhase.Countdown) return true; // Countdownを抜けたら終了
                int idx = _controller.CountdownStepIndex;
                if (idx >= 0 && (seenSteps.Count == 0 || seenSteps[seenSteps.Count - 1] != idx))
                {
                    seenSteps.Add(idx);
                    lastCount = _controller.CountdownStepCount;
                }
                return false;
            }, 5f, "Countdownフェーズが終了しなかった");

            Assert.AreEqual(4, lastCount, "CountdownStepCountが4(3→2→1→スタート!)になっていない");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, seenSteps, "カウントダウンのステップが0→1→2→3の順に進んでいない");
        }

        [UnityTest]
        public IEnumerator Countdown_DoesNotAdvance_WhilePaused()
        {
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);

            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 5f, judgeEffectDuration: 0.02f,
                countdownNormal: 1.0f, minRemainingTimeToStartNewMicroGame: 100f);
            ReflectionTestUtil.SetPrivateField(balance, "countdownNormalStepDuration", 0.5f);

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            yield return WaitUntilOrFail(() => _controller.Phase == GameSessionPhase.Countdown, 3f,
                "[Countdown] フェーズに入らなかった");

            _controller.SetPaused(true);
            float elapsedAtPause = _controller.CountdownStepElapsed;
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.AreEqual(GameSessionPhase.Countdown, _controller.Phase, "ポーズ中にCountdownが終わってしまった");
            Assert.AreEqual(elapsedAtPause, _controller.CountdownStepElapsed,
                "ポーズ中にカウントダウンのステップ経過時間が進んでしまっている(§4.3)");

            _controller.SetPaused(false);
            yield return WaitUntilOrFail(() => _controller.Phase != GameSessionPhase.Countdown, 3f,
                "ポーズ解除後もCountdownが終わらない");
        }

        [UnityTest]
        public IEnumerator RunCountdown_WaitsForMicroGameSlotBound_BeforeShowingOverlay()
        {
            // ★30_Overlay_Countdown.md §7-1 の再発防止テスト: MicroGameSlotが渡される前に
            // ShowOverlay(Countdown) を呼んでしまうと、直後の ShowScreen(GamePlay) でオーバーレイが
            // 即座に閉じられてしまう。MicroGameSlotが未バインドの間はオーバーレイを出さないこと。
            var fakeFlow = new FakeScreenFlowService();
            ServiceLocator.Register<IScreenFlowService>(fakeFlow);

            _controller.BindMicroGameSlot(null); // ★未バインド状態を再現する

            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new DummyMicroGame.Step[0]);
            var balance = TestFactory.CreateBalance(promptDuration: 0.02f, playDuration: 5f, judgeEffectDuration: 0.02f,
                countdownNormal: 0.2f, minRemainingTimeToStartNewMicroGame: 100f);
            ReflectionTestUtil.SetPrivateField(balance, "countdownNormalStepDuration", 0.1f);

            ConfigureBalanceAndForcedMicroGame(dummy, balance, sessionDuration: 5f);

            // 数フレーム待っても、MicroGameSlot未バインドの間はオーバーレイが出ないこと。
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(GameSessionPhase.Countdown, _controller.Phase);
            Assert.IsFalse(fakeFlow.ShowOverlayCalls.Contains(OverlayId.Countdown),
                "MicroGameSlot未バインドなのにOverlay_Countdownを表示してしまった(§7-1)");
            Assert.AreEqual(-1, _controller.CountdownStepIndex, "スロット未バインドなのにカウントダウンのステップが進んでいる");

            // MicroGameSlotが渡されたら、そこから初めてオーバーレイを表示しカウントダウンを進める。
            _controller.BindMicroGameSlot(_slot);
            yield return WaitUntilOrFail(() => fakeFlow.ShowOverlayCalls.Contains(OverlayId.Countdown), 3f,
                "MicroGameSlotのバインド後もOverlay_Countdownが表示されなかった");
            yield return WaitUntilOrFail(() => _controller.CountdownStepIndex >= 0, 3f,
                "MicroGameSlotのバインド後もカウントダウンのステップが進まなかった");
        }

        private class FakeScreenFlowService : IScreenFlowService
        {
            public readonly List<OverlayId> ShowOverlayCalls = new List<OverlayId>();
            public readonly List<OverlayId> HideOverlayCalls = new List<OverlayId>();
            public ScreenId CurrentScreen { get; private set; }
            public ScreenId? LastShownScreen { get; private set; }
            public event System.Action<ScreenId> ScreenChanged;

            public void ShowScreen(ScreenId id)
            {
                CurrentScreen = id;
                LastShownScreen = id;
                ScreenChanged?.Invoke(id);
            }

            public bool ShowOverlay(OverlayId id)
            {
                ShowOverlayCalls.Add(id);
                return true;
            }

            public void HideOverlay(OverlayId id)
            {
                HideOverlayCalls.Add(id);
            }
        }

        /// <summary>テスト用の最小 ISaveManager 実装。`CommitSessionResult` が呼ばれたかだけを記録する。</summary>
        private class FakeSaveManager : Santa.Core.ISaveManager
        {
            public bool CommitCalled { get; private set; }

            public int SaveVersion => 1;
            public int HighScore => 0;
            public int BestRankIndex => 0;
            public int TotalUnits => 0;
            public int TotalPlays => 0;
            public bool FirstLaunchDone { get; set; }
            public float BgmVolume { get; set; } = 1f;
            public float SeVolume { get; set; } = 1f;
            public string Language { get; set; } = "ja";

            public (int cleared, int attempted) GetMicroGameStats(string microGameId) => (0, 0);
            public bool IsMicroIntroSeen(string microGameId) => true; // ★Introのテストと干渉しないよう常に既読扱い
            public void SetMicroIntroSeen(string microGameId, bool seen) { }
            public void SetRecentQuestions(string microGameId, string commaSeparatedIds) { }

            public bool CommitSessionResult(
                int score, int rankIndex, int clearedUnits,
                IReadOnlyDictionary<string, (int cleared, int missed)> perMicroGame)
            {
                CommitCalled = true;
                return false;
            }

            public void ResetAllData(Santa.MicroGames.MicroGameCatalog catalog) { }
        }

        /// <summary>テスト用の最小 IAudioManager 実装。呼び出し回数だけを記録する。</summary>
        private class FakeAudioManager : IAudioManager
        {
            public readonly Dictionary<string, int> SeCallCounts = new Dictionary<string, int>();

            public void PlayBgm(string bgmId) { }
            public void StopBgm() { }
            public void PlaySe(string seId)
            {
                SeCallCounts.TryGetValue(seId, out var c);
                SeCallCounts[seId] = c + 1;
            }
            public void StopSe(string seId) { }
            public void FadeOutBgm(float seconds) { }
            public void PlayLoopSe(string seId) { }
            public void StopLoopSe(string seId) { }
            public void StopAllLoopSe() { }
            public void PauseAll() { }
            public void ResumeAll() { }
            public void ApplyVolumeSettings(float bgmVolume, float seVolume) { }
        }
    }
}
