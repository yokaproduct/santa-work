using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Santa.MicroGames;
using Santa.MicroGames.Weight;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// M3(重さ)の本実装(`WeightMicroGame` / `WeightQuestionGenerator`)の検証。
    /// `22_MicroGame_Weight.md` §4 / §5.2。
    ///
    /// ★注意: EditModeテストにはUnityのフレーム更新(Player Loop)が無いため、
    /// `MonoBehaviour.StartCoroutine` で開始したコルーチン(そりのスライド演出)は自動的には進まない。
    /// そのため:
    /// - スライド演出の「ポーズ中は進まない」という契約は、コルーチンの実体(IEnumerator)を
    ///   直接 MoveNext() して検証する(<see cref="SlideAnimation_DoesNotProgress_WhilePaused"/>)。
    /// - 3件クリアの一連の流れは、スライド演出が終わった後に本来コルーチンが行う
    ///   「次のそりのセットアップ + 再入力可能化」を直接呼び出して代替し、
    ///   ビジネスロジック(イベント発火順序・件数カウント)を検証する
    ///   (<see cref="ClearingThreeUnits_FiresAllUnitsClearedOnlyOnce_AfterThirdUnit"/>)。
    /// </summary>
    public class WeightMicroGameTests
    {
        private GameObject _root;
        private WeightMicroGameRefs _refs;
        private WeightBoxRefs[] _boxes;
        private WeightGeneratorSettings _settings;
        private MicroGameDefinition _definition;
        private readonly List<UnityEngine.Object> _dynamicAssets = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            // ★NUnitの既定(LifeCycle.SingleInstance)ではテストフィクスチャのインスタンスは
            // クラス全体で1つを使い回す(テストメソッドごとに新規生成されない)。そのため、
            // ここで破棄した後に参照をnullへ戻しておかないと、次のテストが「まだ使える」と
            // 誤認してしまう。UnityEngine.Objectは破棄後も参照自体はnullにならない
            // (Unity独自の`==`オーバーロードでのみ破棄済みと判定できる、いわゆる「フェイクnull」)
            // ため、フィールドへの再代入を省略すると事故る。
            if (_root != null) Object.DestroyImmediate(_root);
            _root = null;
            _refs = null;
            _boxes = null;

            if (_settings != null) Object.DestroyImmediate(_settings);
            _settings = null;

            if (_definition != null) Object.DestroyImmediate(_definition);
            _definition = null;

            foreach (var asset in _dynamicAssets)
            {
                if (asset != null) Object.DestroyImmediate(asset);
            }
            _dynamicAssets.Clear();
        }

        private Sprite CreateTestSprite()
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            _dynamicAssets.Add(texture);
            _dynamicAssets.Add(sprite);
            return sprite;
        }

        // ------------------------------------------------------------
        // §5.2 生成器: 解の一意性 + 重さの重複が無いこと
        // ------------------------------------------------------------

        [Test]
        public void Generator_AlwaysProducesUniqueSolution_And_DistinctBoxValues()
        {
            var settings = BuildSettings();
            var rng = new System.Random(12345);

            for (int trial = 0; trial < 300; trial++)
            {
                var preset = WeightQuestionGenerator.Generate(settings, rng);

                Assert.AreEqual(6, preset.boxValues.Length, "箱の数は常に6個のはず");
                Assert.AreEqual(6, preset.boxValues.Distinct().Count(),
                    $"重さの重複がある: [{string.Join(",", preset.boxValues)}]");

                int matches = CountSubsetsSummingToIndependently(preset.boxValues, preset.target);
                Assert.AreEqual(1, matches,
                    $"目標値{preset.target}に一致する組み合わせが1通りでない(実測{matches})。" +
                    $"boxValues=[{string.Join(",", preset.boxValues)}]");
            }
        }

        [Test]
        public void Generator_FallsBackWhenValueRangeTooNarrowForBoxCount()
        {
            // valueMin/valueMaxの範囲がboxCount未満だと相異なる6個を選べない。無限ループにせずフォールバックすること。
            var settings = BuildSettings(valueMin: 1, valueMax: 3);
            var preset = WeightQuestionGenerator.Generate(settings, new System.Random(1));
            Assert.IsNotNull(preset);
            Assert.AreEqual(6, preset.boxValues.Length);
        }

        [Test]
        public void Generator_NeverExceedsMaxTargetValue_AndAnswerBoxCountIsAtMostThree()
        {
            // ★2026-09-16 ディレクター決定: テストプレイ用の暫定難易度として目標値の上限は20。
            //   valueMaxはデフォルトのまま(35)にしておき、おとりの箱には大きな値が混ざりうる状態でも
            //   targetそのものは常に上限以下になることを確認する。
            var settings = BuildSettings();
            var rng = new System.Random(777);

            for (int trial = 0; trial < 300; trial++)
            {
                var preset = WeightQuestionGenerator.Generate(settings, rng);
                Assert.LessOrEqual(preset.target, settings.MaxTargetValue,
                    $"目標値{preset.target}が上限({settings.MaxTargetValue})を超えている");
                Assert.LessOrEqual(preset.answerIndex.Length, 3, "答えの箱が3個を超えている");
            }
        }

        [Test]
        public void RealPresetAsset_AllTargetsAreAtMostTwenty()
        {
            // ★実データ(Assets/Settings/Questions/WeightGeneratorSettings.asset)そのものを検証する。
            //   `WeightPresetUniquenessTests` と同じ方針(テストコード内にデータを書き写さない)。
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<WeightGeneratorSettings>(
                "Assets/Settings/Questions/WeightGeneratorSettings.asset");
            if (settings == null)
            {
                Assert.Ignore("WeightGeneratorSettings.asset がまだ存在しません。");
            }

            foreach (var preset in settings.Presets)
            {
                Assert.LessOrEqual(preset.target, 20, $"{preset.id}: 目標値が20を超えている");
                Assert.LessOrEqual(preset.answerIndex.Length, 3, $"{preset.id}: 答えの箱が3個を超えている");
            }
        }

        private static int CountSubsetsSummingToIndependently(int[] values, int target)
        {
            // WeightQuestionGeneratorの内部実装から独立して再検証する(LetterQuestionDataTestsと同じ方針)。
            int n = values.Length;
            int count = 0;
            for (int mask = 1; mask < (1 << n); mask++)
            {
                int sum = 0;
                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) != 0) sum += values[i];
                }
                if (sum == target) count++;
            }
            return count;
        }

        // ------------------------------------------------------------
        // Prepare / 選択と取り消し
        // ------------------------------------------------------------

        [Test]
        public void Prepare_SetsUpFirstUnit_SixBoxesWithNumbers_AndMeterAtZero()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(3, new System.Random(1)));

            foreach (var box in _boxes)
            {
                Assert.IsTrue(int.TryParse(box.ValueText.text, out _),
                    $"BoxValueTextが数値になっていない: '{box.ValueText.text}'");
            }

            Assert.IsTrue(int.TryParse(_refs.TargetValueText.text, out int target));
            Assert.Greater(target, 0);
            Assert.AreEqual(0f, _refs.MeterFill.fillAmount, 0.0001f);

            foreach (var box in _boxes)
            {
                Assert.IsFalse(box.Button.interactable, "Begin前は非活性のはず(§4.1)");
            }
        }

        [Test]
        public void SelectingAndDeselectingBoxes_UpdatesMeterFillBySelectedSum()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(3, new System.Random(2)));
            game.Begin();

            var boxValues = (int[])GetPrivateField(game, "_boxValues");
            int meterMax = boxValues.Sum();

            InvokePrivate(game, "OnBoxTapped", 0);
            float expected = (float)boxValues[0] / meterMax;
            Assert.AreEqual(expected, _refs.MeterFill.fillAmount, 0.001f, "選択がメーターへ反映されていない");

            InvokePrivate(game, "OnBoxTapped", 0); // 取り消し
            Assert.AreEqual(0f, _refs.MeterFill.fillAmount, 0.001f, "取り消しがメーターへ反映されていない");
        }

        [Test]
        public void SelectingFourthBox_IsRejected_NoStateChange()
        {
            // ★2026-09-16 ディレクター決定: 選択できる箱は最大3個。
            var game = BuildGame();
            game.Prepare(BuildContext(3, new System.Random(20)));
            game.Begin();

            // targetをどのタップでも絶対に届かない大きな値にしておき、成立/オーバーの副作用
            // (ユニットクリア・スライド開始等)が選択上限の検証に混ざらないようにする。
            var boxValues = (int[])GetPrivateField(game, "_boxValues");
            SetPrivateField(game, "_currentTarget", boxValues.Sum() + 1000);

            InvokePrivate(game, "OnBoxTapped", 0);
            InvokePrivate(game, "OnBoxTapped", 1);
            InvokePrivate(game, "OnBoxTapped", 2);

            Assert.AreEqual(3, (int)InvokePrivate(game, "CountSelected"), "3個選択できているはず");

            var homePositions = (Vector2[])GetPrivateField(game, "_boxHomePositions");

            InvokePrivate(game, "OnBoxTapped", 3); // 4個目。受け付けられないはず

            var selected = (bool[])GetPrivateField(game, "_selected");
            Assert.IsFalse(selected[3], "4個目の選択は拒否されるはず");
            Assert.AreEqual(3, (int)InvokePrivate(game, "CountSelected"), "4個目のタップ後も選択数は3のまま");
            Assert.AreEqual(homePositions[3], _boxes[3].RectTransform.anchoredPosition,
                "拒否されたはずの箱の見た目(位置)が変化している");

            // 選択済みの箱の取り消しは、上限に達していても引き続きできるはず。
            InvokePrivate(game, "OnBoxTapped", 0);
            selected = (bool[])GetPrivateField(game, "_selected");
            Assert.IsFalse(selected[0], "選択済みの箱の取り消しはできるはず");
            Assert.AreEqual(2, (int)InvokePrivate(game, "CountSelected"));
        }

        [Test]
        public void SledVisual_TransitionsThroughSelectionStates_0_Normal1_Normal2_Angry2_Normal1()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(3, new System.Random(21)));
            game.Begin();

            var idleSprite = CreateTestSprite();
            var normalSprites = new[] { CreateTestSprite(), CreateTestSprite(), CreateTestSprite() };
            var angrySprites = new[] { CreateTestSprite(), CreateTestSprite(), CreateTestSprite() };
            var happySprites = new[] { CreateTestSprite(), CreateTestSprite(), CreateTestSprite() };
            SetPrivateField(_settings, "sledReindeerIdleSprite", idleSprite);
            SetPrivateField(_settings, "sledNormalSprites", normalSprites);
            SetPrivateField(_settings, "sledAngrySprites", angrySprites);
            SetPrivateField(_settings, "sledHappySprites", happySprites);

            // ★視覚状態の遷移だけを狙って検証したいので、生成された問題を上書きして値を完全に制御する。
            //   A=2, B=3 は合計してもtarget(10)未満。C=20はAと組み合わせるとtargetを超える(オーバー)。
            var preset = new WeightPreset
            {
                id = "visual-test",
                boxValues = new[] { 2, 3, 20, 25, 28, 31 },
                target = 10,
                answerIndex = new[] { 0, 1 },
            };
            InvokePrivate(game, "SetupUnit", preset);
            SetPrivateField(game, "_sliding", false);
            InvokePrivate(game, "SetBoxesInteractable", true);

            var boxValues = (int[])GetPrivateField(game, "_boxValues"); // SetupUnit内でシャッフルされた並び
            int idxA = System.Array.IndexOf(boxValues, 2);
            int idxB = System.Array.IndexOf(boxValues, 3);
            int idxC = System.Array.IndexOf(boxValues, 20);

            var sledImage = _refs.SledReindeerImage;
            Assert.AreEqual(idleSprite, sledImage.sprite, "0: 初期状態はidleのはず");

            InvokePrivate(game, "OnBoxTapped", idxA); // 選択数1、sum=2<10
            Assert.AreEqual(normalSprites[0], sledImage.sprite, "normal1のはず");

            InvokePrivate(game, "OnBoxTapped", idxB); // 選択数2、sum=5<10
            Assert.AreEqual(normalSprites[1], sledImage.sprite, "normal2のはず");

            InvokePrivate(game, "OnBoxTapped", idxB); // Bを取り消し(選択数1、途中経過。個別のアサートはしない)
            InvokePrivate(game, "OnBoxTapped", idxC); // 選択数2、sum=2+20=22>10 → オーバー
            Assert.AreEqual(angrySprites[1], sledImage.sprite, "angry2のはず");

            InvokePrivate(game, "OnBoxTapped", idxC); // 1個外す。選択数1、sum=2<10
            Assert.AreEqual(normalSprites[0], sledImage.sprite, "1個外した後はnormal1に戻るはず");
        }

        // ------------------------------------------------------------
        // 成立(合計==目標値) / オーバー(合計>目標値)
        // ------------------------------------------------------------

        [Test]
        public void ReachingExactTarget_ClearsUnit_FiresEventsInOrder_ForSingleRequiredUnit()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(1, new System.Random(3)));
            game.Begin();

            var boxValues = (int[])GetPrivateField(game, "_boxValues");
            int target = (int)GetPrivateField(game, "_currentTarget");
            var subset = FindSubsetIndices(boxValues, target);
            Assert.IsNotNull(subset, "targetに一致する組み合わせが見つからない(生成バリデーションが機能していない可能性)");

            var events = new List<string>();
            game.OnUnitCleared += () => events.Add("cleared");
            game.OnAllUnitsCleared += () => events.Add("all");
            game.OnMissed += () => events.Add("missed");

            foreach (var index in subset)
            {
                InvokePrivate(game, "OnBoxTapped", index);
            }

            CollectionAssert.AreEqual(new[] { "cleared", "all" }, events,
                "OnUnitCleared→OnAllUnitsClearedの順で発火し、OnMissedは発火しないはず");

            var summary = game.GetSummary();
            Assert.AreEqual(1, summary.UnitsCleared);
            Assert.AreEqual(0, summary.UnitsMissed, "M3のUnitsMissedは構造上つねに0(§4.7)");

            foreach (var box in _boxes)
            {
                Assert.IsFalse(box.Button.interactable, "3件目(=RequiredUnits)クリア後は非活性のはず");
            }
        }

        [Test]
        public void ExceedingTarget_DoesNotRaiseMissed_AndGameKeepsRunning()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(3, new System.Random(4)));
            game.Begin();

            bool missedRaised = false;
            game.OnMissed += () => missedRaised = true;

            var boxValues = (int[])GetPrivateField(game, "_boxValues");
            int target = (int)GetPrivateField(game, "_currentTarget");

            var sortedIndices = Enumerable.Range(0, boxValues.Length).OrderByDescending(i => boxValues[i]).ToArray();
            int sum = 0;
            foreach (var i in sortedIndices)
            {
                sum += boxValues[i];
                InvokePrivate(game, "OnBoxTapped", i);

                if (sum == target)
                {
                    Assert.Inconclusive("たまたま正解の組み合わせを踏んだため、この乱数種ではオーバーの検証ができない");
                }
                if (sum > target)
                {
                    break;
                }
            }

            Assert.Greater(sum, target, "用意した箱の組み合わせではtargetを超えられなかった(乱数種を見直すこと)");
            Assert.IsFalse(missedRaised, "M3のオーバーはOnMissedを発火してはいけない(§4.3 / §4.4)");

            foreach (var box in _boxes)
            {
                Assert.IsTrue(box.Button.interactable, "オーバーしてもゲームは止まらず操作を続けられるはず");
            }
        }

        // ------------------------------------------------------------
        // 3件クリアで1ミニゲームクリア
        // ------------------------------------------------------------

        [Test]
        public void ClearingThreeUnits_FiresAllUnitsClearedOnlyOnce_AfterThirdUnit()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(3, new System.Random(5)));
            game.Begin();

            int allClearedCount = 0;
            int unitClearedCount = 0;
            game.OnAllUnitsCleared += () => allClearedCount++;
            game.OnUnitCleared += () => unitClearedCount++;

            var units = (WeightPreset[])GetPrivateField(game, "_units");

            for (int unit = 0; unit < 3; unit++)
            {
                var boxValues = (int[])GetPrivateField(game, "_boxValues");
                int target = (int)GetPrivateField(game, "_currentTarget");
                var subset = FindSubsetIndices(boxValues, target);
                Assert.IsNotNull(subset);

                foreach (var index in subset)
                {
                    InvokePrivate(game, "OnBoxTapped", index);
                }

                Assert.AreEqual(unit + 1, unitClearedCount);

                if (unit < 2)
                {
                    Assert.AreEqual(0, allClearedCount, "3台そろう前にOnAllUnitsClearedが発火した");

                    // ★実機ではここでそりのスライド演出(コルーチン)が0.25秒かけて進むが、
                    // EditModeテストはUnityのフレーム更新を持たないため実駆動できない。
                    // コルーチンが完了後に行う処理(次のそりのセットアップ+再入力可能化)を直接呼び、
                    // ビジネスロジックの検証を続行する。
                    InvokePrivate(game, "SetupUnit", units[unit + 1]);
                    SetPrivateField(game, "_sliding", false);
                    InvokePrivate(game, "SetBoxesInteractable", true);
                }
            }

            Assert.AreEqual(1, allClearedCount, "OnAllUnitsClearedは1回だけ発火するはず");
            Assert.AreEqual(3, unitClearedCount);
            Assert.AreEqual(3, game.GetSummary().UnitsCleared);
        }

        // ------------------------------------------------------------
        // ★2026-09-16(3) 目標の数字の表示・差し替えタイミング(§3.3.1)
        // ------------------------------------------------------------

        [Test]
        public void ClearingUnit_KeepsTargetTextUntilNextUnitSetup_ThenReplacesIt()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(2, new System.Random(9)));
            game.Begin();

            var units = (WeightPreset[])GetPrivateField(game, "_units");
            string firstTargetText = _refs.TargetValueText.text;
            Assert.AreEqual(units[0].target.ToString(), firstTargetText);

            var boxValues = (int[])GetPrivateField(game, "_boxValues");
            int target = (int)GetPrivateField(game, "_currentTarget");
            var subset = FindSubsetIndices(boxValues, target);
            Assert.IsNotNull(subset);

            foreach (var index in subset)
            {
                InvokePrivate(game, "OnBoxTapped", index);
            }

            // ★成立した瞬間(そりが右へ抜け始める時点)では、まだ前の目標値のまま(§3.3.1)。
            Assert.AreEqual(firstTargetText, _refs.TargetValueText.text,
                "そりが抜けきる前に目標の数字が変わってしまった(§3.3.1違反)");

            // ★次のそりが左から入り始めるフレームで SetupUnit が呼ばれ、そこで初めて差し替わる
            //   (実装ではコルーチン SlideToNextUnit の中で呼ばれる。EditModeでは直接呼んで検証する)。
            InvokePrivate(game, "SetupUnit", units[1]);
            Assert.AreEqual(units[1].target.ToString(), _refs.TargetValueText.text,
                "次のそりの目標値に差し替わっていない");
        }

        [Test]
        public void Prefab_TargetValueText_IsNotChildOfSledUnit_AndTextIsBlack()
        {
            const string prefabPath = "Assets/Prefabs/UI/MicroGames/MicroGame_Weight.prefab";
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Assert.Ignore($"{prefabPath} が見つかりません。");
                return;
            }

            var refs = prefab.GetComponent<WeightMicroGameRefs>();
            Assert.IsNotNull(refs, "WeightMicroGameRefs が付いていない");
            Assert.IsNotNull(refs.TargetValueText, "targetValueText が未設定");
            Assert.IsNotNull(refs.SledUnit, "sledUnit が未設定");

            Assert.IsFalse(refs.TargetValueText.transform.IsChildOf(refs.SledUnit),
                "★2026-09-16(3) TargetValueText が SledUnit の子のまま" +
                "(そりのスライドに付いていってはいけない。§3.3.1)");

            Color c = refs.TargetValueText.color;
            Assert.AreEqual(0f, c.r, 0.01f, "目標の数字が黒になっていない(r)");
            Assert.AreEqual(0f, c.g, 0.01f, "目標の数字が黒になっていない(g)");
            Assert.AreEqual(0f, c.b, 0.01f, "目標の数字が黒になっていない(b)");
        }

        // ------------------------------------------------------------
        // 契約: Finish / SetPaused
        // ------------------------------------------------------------

        [Test]
        public void Finish_DisablesBoxes_AndIsIdempotent_NoEventsAfter()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(3, new System.Random(6)));
            game.Begin();

            game.Finish(MicroGameFinishReason.TimeUp);

            bool clearedAfterFinish = false;
            bool missedAfterFinish = false;
            game.OnUnitCleared += () => clearedAfterFinish = true;
            game.OnMissed += () => missedAfterFinish = true;

            foreach (var box in _boxes)
            {
                Assert.IsFalse(box.Button.interactable, "Finish後は非活性のはず");
            }

            Assert.DoesNotThrow(() => game.Finish(MicroGameFinishReason.TimeUp), "Finishの多重呼び出しで例外が出た");

            // Finish後の入力(ボタン自体はinteractable=falseで防がれるが、内部ガードも二重に確認する)
            InvokePrivate(game, "OnBoxTapped", 0);

            Assert.IsFalse(clearedAfterFinish, "Finish後にOnUnitClearedが発火した");
            Assert.IsFalse(missedAfterFinish, "Finish後にOnMissedが発火した");
        }

        [Test]
        public void SetPaused_True_DisablesBoxes_False_RestoresThem()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(3, new System.Random(7)));
            game.Begin();

            game.SetPaused(true);
            foreach (var box in _boxes)
            {
                Assert.IsFalse(box.Button.interactable, "ポーズ中は非活性のはず");
            }

            game.SetPaused(false);
            foreach (var box in _boxes)
            {
                Assert.IsTrue(box.Button.interactable, "ポーズ解除後は活性に戻るはず");
            }
        }

        [Test]
        public void SlideAnimation_DoesNotProgress_WhilePaused()
        {
            var game = BuildGame();
            game.Prepare(BuildContext(3, new System.Random(8)));
            game.Begin();

            SetPrivateField(game, "_paused", true);

            var from = Vector2.zero;
            var to = new Vector2(1200f, 0f);
            _refs.SledUnit.anchoredPosition = from;

            var enumerator = (IEnumerator)InvokePrivate(game, "SlideSledUnit", from, to, 0.25f);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
            }

            Assert.AreEqual(from, _refs.SledUnit.anchoredPosition,
                "SetPaused中なのにそりのスライド演出が進んでしまった(§4.5必須要件)");
        }

        // ------------------------------------------------------------
        // テストヘルパー
        // ------------------------------------------------------------

        private WeightGeneratorSettings BuildSettings(
            int valueMin = 1, int valueMax = 35, int answerMin = 2, int answerMax = 3, int maxRetry = 50)
        {
            var settings = ScriptableObject.CreateInstance<WeightGeneratorSettings>();
            SetPrivateField(settings, "boxCount", 6);
            SetPrivateField(settings, "valueMin", valueMin);
            SetPrivateField(settings, "valueMax", valueMax);
            SetPrivateField(settings, "answerBoxCountMin", answerMin);
            SetPrivateField(settings, "answerBoxCountMax", answerMax);
            SetPrivateField(settings, "maxRetry", maxRetry);
            SetPrivateField(settings, "sledSlideDuration", 0.25f);

            var fallback = new WeightPreset
            {
                id = "W01",
                boxValues = new[] { 4, 6, 7, 10, 15, 23 },
                target = 13,
                answerIndex = new[] { 1, 2 },
            };
            SetPrivateField(settings, "fallbackPreset", fallback);
            SetPrivateField(settings, "presets", new List<WeightPreset> { fallback });

            _settings = settings;
            return settings;
        }

        private MicroGameDefinition BuildDefinition(WeightGeneratorSettings settings)
        {
            var def = ScriptableObject.CreateInstance<MicroGameDefinition>();
            SetPrivateField(def, "id", "weight");
            SetPrivateField(def, "questionSource", settings);
            SetPrivateField(def, "questionsPerUnit", 1);
            SetPrivateField(def, "enabled", true);
            _definition = def;
            return def;
        }

        private MicroGameContext BuildContext(int requiredUnits, System.Random rng)
        {
            // ★UnityEngine.Objectの「フェイクnull」は `??`(null合体演算子)では正しく検出できない
            // (`??`はCLRレベルの素の参照比較になり、Unity独自の`==`オーバーロードを経由しないため)。
            // 明示的な `== null` 判定(Unity側のオーバーロードが効く)に置き換える。
            if (_settings == null)
            {
                _settings = BuildSettings();
            }
            if (_definition == null)
            {
                _definition = BuildDefinition(_settings);
            }
            return new MicroGameContext(_definition, requiredUnits, 12f, rng, 0, new Dictionary<string, IMicroGameSessionState>());
        }

        private WeightMicroGame BuildGame()
        {
            _root = new GameObject("~WeightMicroGameTestRoot", typeof(RectTransform));
            var rootRect = (RectTransform)_root.transform;

            var sledUnit = NewChild("SledUnit", rootRect);

            var targetTextGo = NewChild("TargetValueText", rootRect);
            var targetText = targetTextGo.gameObject.AddComponent<TextMeshProUGUI>();

            var reindeerGo = NewChild("SledReindeerImage", rootRect);
            var reindeer = reindeerGo.gameObject.AddComponent<Image>();

            var loadMeterRect = NewChild("LoadMeter", rootRect);
            loadMeterRect.sizeDelta = new Vector2(1000, 56);

            var meterFillGo = NewChild("MeterFill", loadMeterRect);
            var meterFill = meterFillGo.gameObject.AddComponent<Image>();
            meterFill.type = Image.Type.Filled;
            meterFill.fillMethod = Image.FillMethod.Horizontal;

            var meterTargetLine = NewChild("MeterTargetLine", loadMeterRect);

            var meterOverflowGo = NewChild("MeterOverflow", loadMeterRect);

            var boxes = new WeightBoxRefs[6];
            Vector2[] positions =
            {
                new Vector2(-350, 180), new Vector2(0, 180), new Vector2(350, 180),
                new Vector2(-350, -180), new Vector2(0, -180), new Vector2(350, -180),
            };

            for (int i = 0; i < 6; i++)
            {
                var boxRect = NewChild($"Box{i}", rootRect);
                boxRect.anchoredPosition = positions[i];

                var boxImageGo = NewChild("BoxImage", boxRect);
                var boxImage = boxImageGo.gameObject.AddComponent<Image>();

                var valueTextGo = NewChild("BoxValueText", boxRect);
                var valueText = valueTextGo.gameObject.AddComponent<TextMeshProUGUI>();

                var shadowGo = NewChild("BoxShadow", boxRect);
                shadowGo.gameObject.SetActive(false);

                var button = boxRect.gameObject.AddComponent<Button>();

                var boxRefs = boxRect.gameObject.AddComponent<WeightBoxRefs>();
                SetPrivateField(boxRefs, "rectTransform", boxRect);
                SetPrivateField(boxRefs, "boxImage", boxImage);
                SetPrivateField(boxRefs, "valueText", valueText);
                SetPrivateField(boxRefs, "shadow", shadowGo.gameObject);
                SetPrivateField(boxRefs, "button", button);

                boxes[i] = boxRefs;
            }
            _boxes = boxes;

            var refs = _root.AddComponent<WeightMicroGameRefs>();
            SetPrivateField(refs, "sledUnit", sledUnit);
            SetPrivateField(refs, "targetValueText", (TMP_Text)targetText);
            SetPrivateField(refs, "sledReindeerImage", reindeer);
            SetPrivateField(refs, "sledSlideDistance", 1200f);
            SetPrivateField(refs, "meterFill", meterFill);
            SetPrivateField(refs, "loadMeterRect", loadMeterRect);
            SetPrivateField(refs, "meterTargetLine", meterTargetLine);
            SetPrivateField(refs, "meterOverflow", meterOverflowGo.gameObject);
            SetPrivateField(refs, "boxSelectedLiftOffset", 24f);
            SetPrivateField(refs, "boxes", boxes);
            _refs = refs;

            var game = _root.AddComponent<WeightMicroGame>();
            SetPrivateField(game, "refs", refs);
            return game;
        }

        private static RectTransform NewChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static int[] FindSubsetIndices(int[] values, int target)
        {
            int n = values.Length;
            for (int mask = 1; mask < (1 << n); mask++)
            {
                int sum = 0;
                var idx = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        sum += values[i];
                        idx.Add(i);
                    }
                }
                if (sum == target)
                {
                    return idx.ToArray();
                }
            }
            return null;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"field not found: {target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"field not found: {target.GetType().Name}.{name}");
            return field.GetValue(target);
        }

        private static object InvokePrivate(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"method not found: {target.GetType().Name}.{name}");
            return method.Invoke(target, args);
        }
    }
}
