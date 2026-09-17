using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Santa.Core;
using Santa.Game;
using Santa.MicroGames.Letter;
using Santa.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Santa.Tests
{
    /// <summary>
    /// `Main.unity` を実際にロードし、
    /// **Title(統合後のホーム画面)の開始ボタン → GamePlay → Result → もう一回** の
    /// 一連の流れを、本物のPrefab(Screen_Title 等)を使って検証する結合テスト。
    ///
    /// ★2026-09-17: `Screen_ModeSelect` は `Screen_Title` に統合され廃止された(02_Screen_Title.md §0.3)。
    /// 旧来の「Title → ModeSelect → GamePlay」の経路は無くなり、Titleの `PlayButton_Time90` を押すと
    /// 直接 `Screen_GamePlay` へ進むようになった。あわせて `Overlay_Countdown` が
    /// `ShowScreen(GamePlay)` で即座に閉じられずに実際に表示されること(30_Overlay_Countdown.md §7-1の
    /// 再発防止)も検証する。
    ///
    /// GameSessionControllerTests.cs がロジック単体のテストであるのに対し、こちらは
    /// 「実際にシーンをロードして、実際のPrefabのボタンを押したら本当に動くか」を確認する。
    /// ディレクターのテストプレイの直前に、少なくとも1回はこの経路が壊れていないことを
    /// 自動で確認しておく目的。
    ///
    /// ★入力はEventSystem経由の実タップではなく、Button.onClick.Invoke() で代替している
    /// (バッチモードでのポインタ入力シミュレーションは複雑なため)。これは「クリックハンドラの
    /// 配線が正しいか」までは検証できるが、「実機でタップ判定が正しく効くか」までは検証していない。
    /// </summary>
    public class GamePlaySceneIntegrationTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null; // Awake/Start が一巡するのを待つ
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ServiceLocator.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullLoop_TitleToGamePlayToResultToRetry_CompletesWithoutErrors()
        {
            // --- Title(統合後のホーム画面) ---
            var screenLayer = GameObject.Find("UICanvas/ScreenLayer");
            Assert.IsNotNull(screenLayer, "ScreenLayer が見つからない(Main.unityの構成が変わった?)");
            var overlayLayer = GameObject.Find("UICanvas/OverlayLayer");
            Assert.IsNotNull(overlayLayer, "OverlayLayer が見つからない");

            yield return WaitForChild(screenLayer.transform, "Screen_Title(Clone)", 3f);
            var titleScreen = FindChild(screenLayer.transform, "Screen_Title(Clone)");
            Assert.IsNotNull(titleScreen, "起動時に Screen_Title が表示されなかった");

            var titleRefs = titleScreen.GetComponent<TitleScreenRefs>();
            Assert.IsNotNull(titleRefs, "TitleScreenRefs が見つからない(統合作業が未反映?)");
            Assert.IsNotNull(titleRefs.PlayButtonTime90, "PlayButton_Time90 が見つからない");

            // テストを高速化するため、常設サービスの GameSessionController にデバッグ用の
            // セッション短縮を仕込む(実行時のみの変更。アセットやシーンには保存しない)。
            // ★カウントダウン自体(GameBalanceSettings.countdownNormal 3.0秒)は意図的に上書きしない。
            //   Overlay_Countdown が実際に表示され続けることを検証したいため。
            var gameSession = ServiceLocator.Get<IGameSessionService>();
            SetPrivateField(gameSession, "debugSessionDurationOverride", 5.0f);
            SetPrivateField(gameSession, "debugPlayDurationOverride", 3.0f);
#if UNITY_EDITOR
            // M1(手紙)は今回作り込んだ唯一の本実装であり、必ず検証したいので出題を強制する。
            var letterDefinition = UnityEditor.AssetDatabase.LoadAssetAtPath<Santa.MicroGames.MicroGameDefinition>(
                "Assets/Settings/MicroGames/MicroGameDefinition_Letter.asset");
            SetPrivateField(gameSession, "debugForceMicroGame", letterDefinition);
#endif

            titleRefs.PlayButtonTime90.onClick.Invoke();
            yield return null;

            // --- GamePlay(ModeSelectを経由せず直行する) ---
            yield return WaitForChild(screenLayer.transform, "Screen_GamePlay(Clone)", 3f);
            var gamePlayScreen = FindChild(screenLayer.transform, "Screen_GamePlay(Clone)");
            Assert.IsNotNull(gamePlayScreen, "「90びょうモード」のタップで Screen_GamePlay へ直接遷移しなかった");

            var refs = gamePlayScreen.GetComponent<GamePlayScreenRefs>();
            Assert.IsNotNull(refs, "GamePlayScreenRefs が見つからない");

            // ★30_Overlay_Countdown.md §7-1 の再発防止: ShowScreen(GamePlay) の直後に
            // Overlay_Countdown が即座に閉じられず、実際に表示され続けること。
            yield return WaitForChild(overlayLayer.transform, "Overlay_Countdown(Clone)", 2f);
            Assert.IsNotNull(FindChild(overlayLayer.transform, "Overlay_Countdown(Clone)"),
                "Screen_GamePlay表示後にOverlay_Countdownが表示されなかった(ShowScreenで即座に閉じられている疑い)");
            Assert.AreEqual(0, refs.MicroGameSlot.childCount,
                "カウントダウン表示中なのにミニゲームが先に生成されてしまっている");
            Assert.AreEqual("", refs.Hud.MicroGameNameText.text,
                "カウントダウン中はミニゲーム名を空欄にするはず(仮文字が見えたままになっていないか)");

            // カウントダウン(既定3.0秒)が終わるまで待つ。終わったら Overlay_Countdown は閉じる。
            yield return WaitUntilOrFail(
                () => FindChild(overlayLayer.transform, "Overlay_Countdown(Clone)") == null,
                6f, "Overlay_Countdown が既定の長さで終了しなかった");

            // ミニゲームが実際にInstantiateされる(=Countdown完了後の初回Prepare)のを待つ。
            yield return WaitUntilOrFail(() => refs.MicroGameSlot.childCount > 0, 5f,
                "MicroGameSlot にミニゲームがInstantiateされなかった");

            // 業務提示フェーズ(Prompt)ではContentRoot全面を覆う白背景(PromptBackdrop)が
            // 表示され、ミニゲームの絵と業務提示テキストが重ならないことを検証する
            // (2026-09-09 ディレクター指摘への対応)。
            yield return WaitUntilOrFail(() => refs.PromptBackdrop.gameObject.activeInHierarchy, 3f,
                "業務提示フェーズになっても PromptBackdrop が表示されなかった");
            Assert.IsNotNull(refs.PromptBackdrop.sprite,
                "PromptBackdrop に Sprite が設定されていない。" +
                "Image.type=Simple でも Sprite が null だと意図しない描画になる恐れがある。");

            // M1(手紙)を強制出題しているので、実際にカードをタップして正誤判定が機能するか検証する。
            var letterGame = refs.MicroGameSlot.GetComponentInChildren<LetterMicroGame>();
            Assert.IsNotNull(letterGame, "debugForceMicroGame で letter を強制したのに MicroGame_Letter が出なかった");

            var letterRefs = refs.MicroGameSlot.GetComponentInChildren<LetterMicroGameRefs>();
            Assert.IsNotNull(letterRefs);
            Assert.AreEqual(4, letterRefs.StockCards.Length, "在庫カードが4枚ではない");
            Assert.IsFalse(string.IsNullOrEmpty(letterRefs.LetterText.text), "手紙の本文が表示されていない");

            // 業務提示(★2026-09-15: 0.8→1.5秒)が終わり Begin() でカードが活性化するのを待つ。
            yield return WaitUntilOrFail(() => letterRefs.StockCards[0].Button.interactable, 3f,
                "業務提示後もカードが非活性のまま(Begin()が反映されていない)");

            // 操作フェーズ(Play)に入ったら PromptBackdrop は非表示に戻り、
            // ミニゲームの操作を隠さないことを検証する。
            Assert.IsFalse(refs.PromptBackdrop.gameObject.activeInHierarchy,
                "操作フェーズになっても PromptBackdrop が表示されたまま(ミニゲームを隠している)");

            // ②「時間バーが2本とも見えない」問題の切り分け: Prefab側の Image.type / fillMethod は
            // 正しいことを確認済み(手作業でYAMLを検査)なので、ここでは
            // 「フォーカス喪失等が無い通常の実行経路で、実際に fillAmount が実時間とともに変化するか」を
            // 直接検証する。ここが正常なら②は①(自動ポーズが解除されない)の副作用だったと判断できる。
            float t1Before = refs.Hud.SessionTimeBarFill.fillAmount;
            float t2Before = refs.MicroGameTimerBar.Fill.fillAmount;
            Assert.Greater(refs.Hud.SessionTimeBarFill.gameObject.activeInHierarchy ? 1 : 0, 0,
                "SessionTimeBarFill が非アクティブ");
            Assert.AreEqual((int)UnityEngine.UI.Image.Type.Filled, (int)refs.Hud.SessionTimeBarFill.type,
                "SessionTimeBarFill の Image.type が Filled になっていない");
            Assert.AreEqual((int)UnityEngine.UI.Image.Type.Filled, (int)refs.MicroGameTimerBar.Fill.type,
                "MicroGameTimerBar の Image.type が Filled になっていない");

            // ★過去の不具合の再発防止: Image.OnPopulateMesh は activeSprite == null の場合、
            // type/fillMethod/fillAmount を一切参照せず常に矩形全面を描画して早期リターンする。
            // そのため fillAmount の値が正しく変化していても、Sprite が未設定だと画面上は
            // 常に満タンに見えるバグが発生する(fillAmountの値検証だけではこの穴を検出できない)。
            Assert.IsNotNull(refs.Hud.SessionTimeBarFill.sprite,
                "SessionTimeBarFill に Sprite が設定されていない。" +
                "Image.type=Filled でも Sprite が null だと OnPopulateMesh が early return し、" +
                "fillAmount の値がいくら変化しても画面上は常に矩形全面(満タン)のまま描画される。");
            Assert.IsNotNull(refs.MicroGameTimerBar.Fill.sprite,
                "MicroGameTimerBar の Fill に Sprite が設定されていない。" +
                "Image.type=Filled でも Sprite が null だと OnPopulateMesh が early return し、" +
                "fillAmount の値がいくら変化しても画面上は常に矩形全面(満タン)のまま描画される。");

            yield return new WaitForSecondsRealtime(0.6f);

            float t1After = refs.Hud.SessionTimeBarFill.fillAmount;
            float t2After = refs.MicroGameTimerBar.Fill.fillAmount;
            Assert.Less(t1After, t1Before, "90秒バー(T1)のfillAmountが実時間経過後も減っていない");
            Assert.Less(t2After, t2Before, "12秒バー(T2)のfillAmountが実時間経過後も減っていない");

            // ★State.MissedUnits は Judge フェーズで GetSummary() から一括加算される設計であり、
            // OnMissed のたびに即座には増えない(共通仕様 §4.3 / GameSessionController.HandleMissed参照)。
            // タップ直後に確認できる信頼できる信号は ClearedUnits(即時加算)と、
            // ミニゲーム自身の GetSummary()(参考値として覗き見る。本来は共通側がFinish後にしか
            // 読まない契約だが、テストからの観測はこの契約を破らない)。
            int clearedBefore = gameSession.State.ClearedUnits;
            int questionsAnsweredBefore = letterGame.GetSummary().QuestionsAnswered;
            letterRefs.StockCards[0].Button.onClick.Invoke(); // 正誤どちらでもよい。配線が動くことの確認
            yield return null;

            Assert.IsTrue(
                gameSession.State.ClearedUnits > clearedBefore ||
                letterGame.GetSummary().QuestionsAnswered > questionsAnsweredBefore,
                "カードをタップしても OnUnitCleared / OnMissed のどちらも反映されなかった");

            // --- Result(短縮したセッション長ぶんだけ待つ) ---
            // ★2026-09-15: 業務提示 0.8→1.5秒 + 終了演出1.5秒の新設ぶん、旧タイムアウト(10秒)では
            // バッチモードの実行が遅い場合に不安定になりうるため延長した。
            yield return WaitForChild(screenLayer.transform, "Screen_Result(Clone)", 20f);
            var resultScreen = FindChild(screenLayer.transform, "Screen_Result(Clone)");
            Assert.IsNotNull(resultScreen, "セッション終了後に Screen_Result へ遷移しなかった");

            var summaryText = resultScreen.GetComponentInChildren<TMPro.TMP_Text>(includeInactive: true);
            Assert.IsNotNull(summaryText);
            Assert.IsFalse(string.IsNullOrEmpty(summaryText.text), "結果画面にスコアが表示されていない");

            // --- もう一回(Titleを経由せずGamePlayへ直行する。カウントダウンはShort=1.5秒) ---
            var retryButton = resultScreen.GetComponentInChildren<StartSessionAndNavigateButton>(true)?.GetComponent<Button>();
            Assert.IsNotNull(retryButton, "RetryButton が見つからない");
            retryButton.onClick.Invoke();
            yield return null;

            yield return WaitForChild(screenLayer.transform, "Screen_GamePlay(Clone)", 3f);
            var retryGamePlayScreen = FindChild(screenLayer.transform, "Screen_GamePlay(Clone)");
            Assert.IsNotNull(retryGamePlayScreen, "「もう一回」で Screen_GamePlay に戻らなかった");

            // 2回目のセッションでスコアが正しく初期化されているか(リセット漏れの再確認)。
            Assert.AreEqual(0, gameSession.State.Score, "「もう一回」で前回のスコアがリセットされていない");
            Assert.AreEqual(0, gameSession.State.ClearedUnits);

            // ★2026-09-17: 「はじめから」相当の再初期化(HUD初期状態への復帰)の確認。
            // ここでは新規Instantiateされた画面のOnEnableでの初期化を確認する形になるが、
            // ミニゲーム名が仮文字のまま残っていないことは変わらず保証されるべき値なので検証する。
            var retryRefs = retryGamePlayScreen.GetComponent<GamePlayScreenRefs>();
            Assert.AreEqual("", retryRefs.Hud.MicroGameNameText.text,
                "「もう一回」直後、カウントダウン中にミニゲーム名が空欄になっていない");
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
            }
            return null;
        }

        private static IEnumerator WaitForChild(Transform parent, string name, float timeoutSeconds)
        {
            float elapsed = 0f;
            while (FindChild(parent, name) == null)
            {
                if (elapsed > timeoutSeconds)
                {
                    Assert.Fail($"タイムアウト: '{name}' が {timeoutSeconds}秒以内に現れなかった");
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static IEnumerator WaitUntilOrFail(System.Func<bool> condition, float timeoutSeconds, string message)
        {
            float elapsed = 0f;
            while (!condition())
            {
                if (elapsed > timeoutSeconds) Assert.Fail($"タイムアウト: {message}");
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
