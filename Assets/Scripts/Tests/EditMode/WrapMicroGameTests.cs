using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Santa.MicroGames;
using Santa.MicroGames.Wrap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// M5「ラッピングしわけ」(`WrapMicroGame`)のテスト。`23_MicroGame_Wrap.md` の実装検証。
    ///
    /// `WrapMicroGame` / `WrapMicroGameRefs` / `WrapChuteRefs` / `WrapLoopDotRefs` / `WrapBoxItemRefs` は
    /// すべて private [SerializeField] で参照を持つため(共通仕様 §1.5 の規約どおり `transform.Find` を
    /// 使わない設計)、テスト側はシーン上に最小限のダミーUI階層を組み立てて、リフレクションで直接
    /// フィールドへ注入する。実際のPrefab資産(`MicroGame_Wrap.prefab` / `Part_WrapBoxItem.prefab`)には
    /// 依存しない(依存すると資産の見た目調整がテストを壊してしまうため)。
    /// </summary>
    public class WrapMicroGameTests
    {
        private readonly List<UnityEngine.Object> _spawned = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _spawned)
            {
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            }
            _spawned.Clear();
        }

        private T Track<T>(T obj) where T : UnityEngine.Object
        {
            _spawned.Add(obj);
            return obj;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, $"フィールドが見つかりません: {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        // ------------------------------------------------------------------
        // テスト用のダミーUI階層の組み立て
        // ------------------------------------------------------------------

        private GameObject CreateBoxItemTemplate()
        {
            var root = Track(new GameObject("TestBoxItem", typeof(RectTransform)));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(200, 200);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(rt, false);

            var pattern = new GameObject("Pattern", typeof(RectTransform), typeof(Image));
            pattern.transform.SetParent(rt, false);

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(rt, false);

            var trash = new GameObject("Trash", typeof(RectTransform));
            trash.transform.SetParent(rt, false);

            var refs = root.AddComponent<WrapBoxItemRefs>();
            SetPrivateField(refs, "background", bg.GetComponent<Image>());
            SetPrivateField(refs, "pattern", pattern.GetComponent<Image>());
            SetPrivateField(refs, "debugLabel", label.GetComponent<TextMeshProUGUI>());
            SetPrivateField(refs, "trashMark", trash);

            return root;
        }

        private WrapChuteRefs CreateChute(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var sampleGo = new GameObject("Sample", typeof(RectTransform), typeof(Image));
            sampleGo.transform.SetParent(go.transform, false);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);

            var refs = go.AddComponent<WrapChuteRefs>();
            SetPrivateField(refs, "button", button);
            SetPrivateField(refs, "sampleImage", sampleGo.GetComponent<Image>());
            SetPrivateField(refs, "sampleLabel", labelGo.GetComponent<TextMeshProUGUI>());
            return refs;
        }

        private WrapLoopDotRefs CreateLoopDot(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);

            var missGo = new GameObject("Miss", typeof(RectTransform));
            missGo.transform.SetParent(go.transform, false);

            var refs = go.AddComponent<WrapLoopDotRefs>();
            SetPrivateField(refs, "fillImage", fillGo.GetComponent<Image>());
            SetPrivateField(refs, "missMark", missGo);
            return refs;
        }

        private WrapMicroGame CreateMicroGame(out WrapMicroGameRefs refs)
        {
            var root = Track(new GameObject("WrapMicroGameUnderTest", typeof(RectTransform)));
            var rootRt = (RectTransform)root.transform;

            var boxContainerGo = new GameObject("BoxContainer", typeof(RectTransform));
            boxContainerGo.transform.SetParent(rootRt, false);

            var loopLabelGo = new GameObject("LoopLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            loopLabelGo.transform.SetParent(rootRt, false);

            var dotsParent = new GameObject("Dots", typeof(RectTransform));
            dotsParent.transform.SetParent(rootRt, false);
            var dots = new[]
            {
                CreateLoopDot(dotsParent.transform, "Dot0"),
                CreateLoopDot(dotsParent.transform, "Dot1"),
                CreateLoopDot(dotsParent.transform, "Dot2"),
            };

            var chutesParent = new GameObject("Chutes", typeof(RectTransform));
            chutesParent.transform.SetParent(rootRt, false);
            var chutes = new[]
            {
                CreateChute(chutesParent.transform, "Chute0"),
                CreateChute(chutesParent.transform, "Chute1"),
                CreateChute(chutesParent.transform, "Chute2"),
            };

            var skipGo = new GameObject("Skip", typeof(RectTransform), typeof(Image), typeof(Button));
            skipGo.transform.SetParent(rootRt, false);
            var skipButton = skipGo.GetComponent<Button>();
            skipButton.targetGraphic = skipGo.GetComponent<Image>();

            var boxItemTemplate = CreateBoxItemTemplate();

            var refsComp = root.AddComponent<WrapMicroGameRefs>();
            SetPrivateField(refsComp, "boxContainer", (RectTransform)boxContainerGo.transform);
            SetPrivateField(refsComp, "boxItemPrefab", boxItemTemplate);
            SetPrivateField(refsComp, "loopLabelText", loopLabelGo.GetComponent<TextMeshProUGUI>());
            SetPrivateField(refsComp, "loopDots", dots);
            SetPrivateField(refsComp, "chutes", chutes);
            SetPrivateField(refsComp, "skipButton", skipButton);

            var game = root.AddComponent<WrapMicroGame>();
            SetPrivateField(game, "refs", refsComp);

            refs = refsComp;
            return game;
        }

        private WrapGeneratorSettings CreateSettings(List<WrapPreset> presets, float boxSpacing = 180f, float beltSpeed = 135f)
        {
            var settings = Track(ScriptableObject.CreateInstance<WrapGeneratorSettings>());
            SetPrivateField(settings, "presets", presets);
            SetPrivateField(settings, "boxSpacing", boxSpacing);
            SetPrivateField(settings, "beltSpeed", beltSpeed);
            SetPrivateField(settings, "catchUpMultiplier", 3f);
            SetPrivateField(settings, "itemFeedbackDuration", 0.2f);
            SetPrivateField(settings, "boxCount", presets.Count > 0 ? presets[0].sequence.Length : 9);
            SetPrivateField(settings, "trashMin", 1);
            SetPrivateField(settings, "trashMax", 3);
            return settings;
        }

        private static WrapPreset BuildPreset(string id, WrapCategory category, (bool isTrash, int chuteIndex)[] sequence)
        {
            var boxDefs = new WrapBoxDef[sequence.Length];
            for (int i = 0; i < sequence.Length; i++)
            {
                boxDefs[i] = new WrapBoxDef { isTrash = sequence[i].isTrash, chuteIndex = sequence[i].chuteIndex };
            }

            return new WrapPreset
            {
                id = id,
                category = category,
                chuteSamples = new[]
                {
                    new WrapSample { color = Color.red, debugLabel = "A" },
                    new WrapSample { color = Color.blue, debugLabel = "B" },
                    new WrapSample { color = Color.green, debugLabel = "C" },
                },
                sequence = boxDefs,
            };
        }

        private MicroGameContext CreateContext(
            WrapGeneratorSettings settings,
            Dictionary<string, IMicroGameSessionState> stateStore,
            int questionsPerUnit = 3,
            int requiredUnits = 3)
        {
            var definition = Track(ScriptableObject.CreateInstance<MicroGameDefinition>());
            SetPrivateField(definition, "id", "wrap_test");
            SetPrivateField(definition, "questionSource", settings);
            SetPrivateField(definition, "questionsPerUnit", questionsPerUnit);

            return new MicroGameContext(definition, requiredUnits, 12f, new System.Random(1234), 0, stateStore);
        }

        private static readonly (bool isTrash, int chuteIndex)[] AllCorrectNineBoxes =
        {
            (false, 0), (false, 1), (false, 2),
            (false, 0), (false, 1), (false, 2),
            (false, 0), (false, 1), (false, 2),
        };

        // ------------------------------------------------------------------
        // 1) 3問=1件の丸め(最重要。全種目のスコアバランスの前提)
        // ------------------------------------------------------------------

        [Test]
        public void ThreeCorrectBoxesInARow_CompleteOneLoop_AndNineBoxesCompleteAllThreeLoops()
        {
            var preset = BuildPreset("T1", WrapCategory.Color, AllCorrectNineBoxes);
            var settings = CreateSettings(new List<WrapPreset> { preset });
            var game = CreateMicroGame(out var refs);

            int unitCleared = 0, allCleared = 0, missed = 0;
            game.OnUnitCleared += () => unitCleared++;
            game.OnAllUnitsCleared += () => allCleared++;
            game.OnMissed += () => missed++;

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            foreach (var (_, chuteIndex) in AllCorrectNineBoxes)
            {
                refs.Chutes[chuteIndex].Button.onClick.Invoke();
            }

            Assert.AreEqual(3, unitCleared, "3ループとも全問正解なので3件成立するはず(9問=最大3件)");
            Assert.AreEqual(0, missed);
            Assert.AreEqual(1, allCleared, "9問目の処理でOnAllUnitsClearedが1回だけ発火するはず");

            var summary = game.GetSummary();
            Assert.AreEqual(3, summary.UnitsCleared);
            Assert.AreEqual(0, summary.UnitsMissed);
            Assert.AreEqual(9, summary.QuestionsAnswered, "QuestionsAnsweredは件ではなく処理した箱の総数(最大9)");
        }

        [Test]
        public void OnUnitCleared_FiresBeforeOnAllUnitsCleared_OnFinalBox()
        {
            var preset = BuildPreset("T1b", WrapCategory.Color, AllCorrectNineBoxes);
            var settings = CreateSettings(new List<WrapPreset> { preset });
            var game = CreateMicroGame(out var refs);

            var log = new List<string>();
            game.OnUnitCleared += () => log.Add("unit");
            game.OnAllUnitsCleared += () => log.Add("all");

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            foreach (var (_, chuteIndex) in AllCorrectNineBoxes)
            {
                refs.Chutes[chuteIndex].Button.onClick.Invoke();
            }

            CollectionAssert.AreEqual(new[] { "unit", "unit", "unit", "all" }, log,
                "OnAllUnitsClearedは最後の(3件目の)OnUnitClearedの後に発火すること(共通仕様§4.3)");
        }

        // ------------------------------------------------------------------
        // 2) 誤答を含むループ・未処理のまま終わるループ
        // ------------------------------------------------------------------

        [Test]
        public void MissInsideALoop_MarksThatLoopAsMissed_ButDoesNotStopSubsequentBoxes()
        {
            var sequence = new (bool, int)[] { (false, 0), (false, 1), (false, 2) };
            var preset = BuildPreset("T2", WrapCategory.Color, sequence);
            var settings = CreateSettings(new List<WrapPreset> { preset });
            var game = CreateMicroGame(out var refs);

            int unitCleared = 0, missed = 0, allCleared = 0;
            game.OnUnitCleared += () => unitCleared++;
            game.OnMissed += () => missed++;
            game.OnAllUnitsCleared += () => allCleared++;

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            // 1問目(正解はChute0)をわざとスキップ口へ誤って入れる。
            refs.SkipButton.onClick.Invoke();
            Assert.AreEqual(1, missed, "誤答タップは即座にOnMissedを発火する");

            // 誤答してもベルトは止まらず、残り2問をそのまま処理できる(§2.1 帰結1)。
            Assert.DoesNotThrow(() => refs.Chutes[1].Button.onClick.Invoke());
            Assert.DoesNotThrow(() => refs.Chutes[2].Button.onClick.Invoke());

            Assert.AreEqual(0, unitCleared, "誤答を含むループは件にならない");
            Assert.AreEqual(1, allCleared, "3箱すべて処理し終えたのでOnAllUnitsClearedは発火する");

            var summary = game.GetSummary();
            Assert.AreEqual(0, summary.UnitsCleared);
            Assert.AreEqual(1, summary.UnitsMissed);
            Assert.AreEqual(3, summary.QuestionsAnswered);
        }

        [Test]
        public void UnprocessedBoxesAtFinish_DoNotRaiseMissed_AndAreNotCountedAsMissedLoop()
        {
            var sequence = new (bool, int)[] { (false, 0), (false, 1), (false, 2) };
            var preset = BuildPreset("T3", WrapCategory.Color, sequence);
            var settings = CreateSettings(new List<WrapPreset> { preset });
            var game = CreateMicroGame(out var refs);

            int missed = 0, unitCleared = 0, allCleared = 0;
            game.OnMissed += () => missed++;
            game.OnUnitCleared += () => unitCleared++;
            game.OnAllUnitsCleared += () => allCleared++;

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            refs.Chutes[0].Button.onClick.Invoke(); // 1問だけ正解しておく。残り2問は未処理のまま。

            Assert.DoesNotThrow(() => game.Finish(MicroGameFinishReason.TimeUp));

            Assert.AreEqual(0, missed, "箱が未処理のまま終わってもOnMissedは発火しない(仕様書§4.7)");
            Assert.AreEqual(0, unitCleared, "ループが完了していないので件にはならない");
            Assert.AreEqual(0, allCleared);

            var summary = game.GetSummary();
            Assert.AreEqual(0, summary.UnitsCleared);
            Assert.AreEqual(0, summary.UnitsMissed, "未完了ループはUnitsMissedにも数えない(仕様書§4.8)");
            Assert.AreEqual(1, summary.QuestionsAnswered, "処理済みの1問だけがQuestionsAnsweredに数えられる");
        }

        // ------------------------------------------------------------------
        // 3) 契約(イベント順序・多重呼び出し防止・ポーズ)
        // ------------------------------------------------------------------

        [Test]
        public void Finish_CalledTwice_DoesNotThrow_AndStopsAcceptingTapsAfterward()
        {
            var sequence = new (bool, int)[] { (false, 0), (false, 1), (false, 2) };
            var preset = BuildPreset("T4", WrapCategory.Color, sequence);
            var settings = CreateSettings(new List<WrapPreset> { preset });
            var game = CreateMicroGame(out var refs);

            int unitCleared = 0;
            game.OnUnitCleared += () => unitCleared++;

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            Assert.DoesNotThrow(() => game.Finish(MicroGameFinishReason.TimeUp));
            Assert.DoesNotThrow(() => game.Finish(MicroGameFinishReason.TimeUp)); // 多重呼び出し防止(共通仕様§4.3)

            Assert.DoesNotThrow(() => refs.Chutes[0].Button.onClick.Invoke());
            Assert.AreEqual(0, unitCleared, "Finish後はタップしても何も起きない");
        }

        [Test]
        public void TappingChute_WithNoActiveBoxLeft_DoesNothing()
        {
            var sequence = new (bool, int)[] { (false, 0) }; // 1問しかないプリセット
            var preset = BuildPreset("T10", WrapCategory.Color, sequence);
            var settings = CreateSettings(new List<WrapPreset> { preset });
            var game = CreateMicroGame(out var refs);

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            refs.Chutes[0].Button.onClick.Invoke(); // 唯一の箱を処理する

            // 判定ゾーンに箱が無いのでタップしても何も起きない(§4.1)。例外も出ない。
            Assert.DoesNotThrow(() => refs.Chutes[0].Button.onClick.Invoke());
            Assert.DoesNotThrow(() => refs.SkipButton.onClick.Invoke());
        }

        [Test]
        public void SetPaused_True_StopsBeltMovement()
        {
            var preset = BuildPreset("T5", WrapCategory.Color, AllCorrectNineBoxes);
            var settings = CreateSettings(new List<WrapPreset> { preset }, beltSpeed: 1000f); // 差を検知しやすい速度
            var game = CreateMicroGame(out var refs);

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            Assert.AreEqual(3, refs.BoxContainer.childCount,
                "Prepare時点で箱1〜3が初期配置されているはず(§4.2 必須要件)");

            var secondBox = (RectTransform)refs.BoxContainer.GetChild(1);
            float before = secondBox.anchoredPosition.y;

            game.SetPaused(true);
            game.Tick(1.0f); // 大きなdtを与えても

            float after = secondBox.anchoredPosition.y;
            Assert.AreEqual(before, after, 0.0001f, "ポーズ中はベルトが進まないこと(M5で最重要)");
        }

        [Test]
        public void Tick_BeforeAnyTap_DoesNotMoveBoxes_BecauseTheyAlreadyRestAtSpacingDistance()
        {
            // ★Prepare() 直後は箱1が停止位置、箱2・3がその後ろに「詰まった状態」で初期配置される
            //   (§4.2 必須要件)。まだ何も処理していない = 詰める必要が無いので、Tick を呼んでも
            //   座標は変化しない。これは実装のバグではなく設計どおりの静止状態。
            var preset = BuildPreset("T6a", WrapCategory.Color, AllCorrectNineBoxes);
            var settings = CreateSettings(new List<WrapPreset> { preset }, beltSpeed: 1000f);
            var game = CreateMicroGame(out var refs);

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            var positionsBefore = new float[3];
            for (int i = 0; i < 3; i++)
            {
                positionsBefore[i] = ((RectTransform)refs.BoxContainer.GetChild(i)).anchoredPosition.y;
            }

            game.Tick(0.05f);

            for (int i = 0; i < 3; i++)
            {
                float after = ((RectTransform)refs.BoxContainer.GetChild(i)).anchoredPosition.y;
                Assert.AreEqual(positionsBefore[i], after, 0.0001f,
                    $"箱{i}: 誰も処理していないのに動いてしまっている");
            }
        }

        [Test]
        public void Tick_AfterProcessingFrontBox_MovesNextBoxTowardStopLine_ByCatchingUp()
        {
            var preset = BuildPreset("T6b", WrapCategory.Color, AllCorrectNineBoxes);
            var settings = CreateSettings(new List<WrapPreset> { preset }, beltSpeed: 100f);
            var game = CreateMicroGame(out var refs);

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            // ★タップ前に「次に先頭になる箱」への参照を直接掴んでおく。
            //   タップ直後は、アイテム単位フィードバック中の旧・先頭の箱がまだ BoxContainer の
            //   子として残っている(0.2秒・非ブロッキングで消える。§6)ため、
            //   GetChild(0) のようなインデックス基準では新しい先頭を特定できない。
            var nextBox = (RectTransform)refs.BoxContainer.GetChild(1);
            float before = nextBox.anchoredPosition.y;
            Assert.Greater(before, 100f + 0.001f, "詰める前はまだ停止位置まで届いていないはず");

            refs.Chutes[0].Button.onClick.Invoke(); // 先頭(旧・箱0)を処理して取り除く

            game.Tick(0.05f);

            float after = nextBox.anchoredPosition.y;
            Assert.Less(after, before, "処理した直後は、後続の箱が停止位置へ向けて詰まっていくはず(§4.2)");
        }

        [Test]
        public void SetPaused_True_DisablesChuteAndSkipButtons()
        {
            var preset = BuildPreset("T5b", WrapCategory.Color, AllCorrectNineBoxes);
            var settings = CreateSettings(new List<WrapPreset> { preset });
            var game = CreateMicroGame(out var refs);

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();
            Assert.IsTrue(refs.Chutes[0].Button.interactable);
            Assert.IsTrue(refs.SkipButton.interactable);

            game.SetPaused(true);
            Assert.IsFalse(refs.Chutes[0].Button.interactable);
            Assert.IsFalse(refs.SkipButton.interactable);

            game.SetPaused(false);
            Assert.IsTrue(refs.Chutes[0].Button.interactable);
        }

        // ------------------------------------------------------------------
        // 4) カテゴリの固定(★仕様の矛盾点。取りまとめ役への報告参照)
        // ------------------------------------------------------------------

        /// <summary>
        /// ★依頼文の「ディレクター決定事項」には「ループごとに分類カテゴリが変わる」とあったが、
        /// これは `23_MicroGame_Wrap.md` §5.2 条件2(確定)「カテゴリは色/形のいずれか1つ。
        /// 1ミニゲーム内では固定(9問を通して変えない)」と矛盾する。データスキーマ
        /// (`WrapPreset.category` は1プリセットにつき1つ)もループごとの切り替えを表現できない。
        /// 本テストは矛盾を承知のうえで、確定仕様(および実データ資産)を正として書いている。
        /// 取りまとめ役への報告でこの矛盾を明記し、確認を仰ぐこと。
        /// </summary>
        [Test]
        public void Category_IsFixedForTheEntireMinigame_NotSwitchedPerLoop()
        {
            var preset = BuildPreset("T7", WrapCategory.Shape, AllCorrectNineBoxes);
            var settings = CreateSettings(new List<WrapPreset> { preset });
            var game = CreateMicroGame(out var refs);

            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));
            game.Begin();

            // 形カテゴリでは条件6により見本の色は白で固定される。9問を通して変化しないことを確認する。
            var initialColors = new[]
            {
                refs.Chutes[0].SampleImage.color,
                refs.Chutes[1].SampleImage.color,
                refs.Chutes[2].SampleImage.color,
            };

            foreach (var (_, chuteIndex) in AllCorrectNineBoxes)
            {
                refs.Chutes[chuteIndex].Button.onClick.Invoke();

                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(initialColors[i], refs.Chutes[i].SampleImage.color,
                        "カテゴリ(および見本表示)は1ミニゲーム内で変化しないはず");
                }
            }
        }

        // ------------------------------------------------------------------
        // 4b) 形カテゴリの一時停止(2026-09-13ディレクター決定)
        // ------------------------------------------------------------------

        /// <summary>
        /// 形カテゴリ(R04〜R06相当)をディレクター決定でいったん出題対象から外した。
        /// `WrapPreset.enabled = false` のプリセットが `Prepare()` で一切選ばれないことを確認する
        /// (データ自体は消さず、抽選対象から外すだけという実装方針の検証)。
        /// </summary>
        [Test]
        public void Prepare_NeverDrawsDisabledPresets()
        {
            var enabledPreset = BuildPreset("Enabled", WrapCategory.Color, AllCorrectNineBoxes);
            var disabledPreset = BuildPreset("Disabled", WrapCategory.Shape, AllCorrectNineBoxes);
            disabledPreset.enabled = false;

            var settings = CreateSettings(new List<WrapPreset> { enabledPreset, disabledPreset });

            // ★複数回 Prepare() し直しても(=毎回シャッフルし直しても)無効化したプリセットが選ばれないことを見る。
            for (int i = 0; i < 5; i++)
            {
                var game = CreateMicroGame(out var refs);
                game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));

                // 条件6(仕様書§5.2): 形カテゴリは見本の色が白で固定される。色カテゴリの Enabled プリセットが
                // 選ばれていれば、見本の色は白にならないはず。
                Assert.AreNotEqual(Color.white, refs.Chutes[0].SampleImage.color,
                    "無効化した形カテゴリのプリセットが選ばれてしまっている");
            }
        }

        // ------------------------------------------------------------------
        // 4c) 箱の完成画像方式(2026-09-13)
        // ------------------------------------------------------------------

        private Sprite CreateTestSprite()
        {
            var texture = Track(new Texture2D(2, 2));
            return Track(Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f)));
        }

        private static WrapPreset BuildColorPresetWithColorIds(string id, Sprite patternSprite, (bool isTrash, int chuteIndex)[] sequence)
        {
            var preset = BuildPreset(id, WrapCategory.Color, sequence);
            preset.chuteSamples[0].colorId = WrapColorId.Red;
            preset.chuteSamples[0].patternSprite = patternSprite;
            preset.chuteSamples[1].colorId = WrapColorId.Blue;
            preset.chuteSamples[2].colorId = WrapColorId.Green;
            return preset;
        }

        [Test]
        public void ApplyBoxVisual_CompletedBoxSpriteSet_SkipsColorMultiplyAndPattern()
        {
            var redSprite = CreateTestSprite();
            var patternSprite = CreateTestSprite();
            var preset = BuildColorPresetWithColorIds("T8", patternSprite, new[] { (false, 0) });

            var settings = CreateSettings(new List<WrapPreset> { preset });
            SetPrivateField(settings, "redBoxSprite", redSprite);

            var game = CreateMicroGame(out var refs);
            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));

            var boxRefs = refs.BoxContainer.GetChild(0).GetComponent<WrapBoxItemRefs>();
            Assert.AreEqual(redSprite, boxRefs.Background.sprite, "完成画像(赤)が設定されていれば箱にそのまま使われるはず");
            Assert.AreEqual(Color.white, boxRefs.Background.color, "完成画像方式では色を乗算しない(白のまま)");
            Assert.IsFalse(boxRefs.Pattern.enabled, "完成画像方式では模様を重ねない");
        }

        [Test]
        public void ApplyBoxVisual_CompletedBoxSpriteMissing_FallsBackToColorAndPattern()
        {
            var patternSprite = CreateTestSprite();
            var preset = BuildColorPresetWithColorIds("T9", patternSprite, new[] { (false, 0) });

            var settings = CreateSettings(new List<WrapPreset> { preset }); // redBoxSprite は未設定のまま

            var game = CreateMicroGame(out var refs);
            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));

            var boxRefs = refs.BoxContainer.GetChild(0).GetComponent<WrapBoxItemRefs>();
            Assert.AreEqual(Color.red, boxRefs.Background.color, "完成画像が未設定なら従来どおり色を乗算する");
            Assert.IsTrue(boxRefs.Pattern.enabled, "完成画像が未設定なら従来どおり模様を重ねる(色覚対策のフォールバック)");
            Assert.AreEqual(patternSprite, boxRefs.Pattern.sprite);
        }

        [Test]
        public void ApplyChuteSample_CompletedBoxSpriteSet_SampleShowsSameArtwork()
        {
            var redSprite = CreateTestSprite();
            var patternSprite = CreateTestSprite();
            var preset = BuildColorPresetWithColorIds("T10b", patternSprite, new[] { (false, 0) });

            var settings = CreateSettings(new List<WrapPreset> { preset });
            SetPrivateField(settings, "redBoxSprite", redSprite);

            var game = CreateMicroGame(out var refs);
            game.Prepare(CreateContext(settings, new Dictionary<string, IMicroGameSessionState>()));

            Assert.AreEqual(redSprite, refs.Chutes[0].SampleImage.sprite,
                "依頼どおり、シュートの見本にも箱と同じ完成画像を表示する");
            Assert.AreEqual(Color.white, refs.Chutes[0].SampleImage.color);
        }

        // ------------------------------------------------------------------
        // 5) 実データ資産のバリデーション(ゴミの数)
        // ------------------------------------------------------------------

        [Test]
        public void RealPresetData_TrashCount_IsWithinConfiguredRange()
        {
            const string assetPath = "Assets/Settings/Questions/WrapGeneratorSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<WrapGeneratorSettings>(assetPath);
            if (settings == null || settings.Presets == null || settings.Presets.Count == 0)
            {
                Assert.Ignore($"{assetPath} が見つからないか、presets が空です。");
                return;
            }

            foreach (var preset in settings.Presets)
            {
                int trashCount = 0;
                foreach (var box in preset.sequence)
                {
                    if (box.isTrash) trashCount++;
                }

                Assert.GreaterOrEqual(trashCount, settings.TrashMin,
                    $"{preset.id}: ゴミの数が最低{settings.TrashMin}個を下回っている");
                Assert.LessOrEqual(trashCount, settings.TrashMax,
                    $"{preset.id}: ゴミの数が最大{settings.TrashMax}個を超えている");
            }
        }
    }
}
