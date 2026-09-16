using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Santa.MicroGames;
using Santa.MicroGames.Debugging;
using UnityEngine;
using UnityEngine.TestTools;

namespace Santa.Tests
{
    /// <summary>
    /// `MicroGameDebugHarness` 自体が仕事をしているかを検証する。
    /// 「飾りにしない」の要求に対応: 実際に契約違反を仕込んだダミーで違反が検出されること、
    /// 正しく振る舞うダミーでは違反が0件であることの両方を確認する。
    /// </summary>
    public class MicroGameDebugHarnessTests
    {
        private GameObject _root;
        private RectTransform _slot;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("~HarnessTestRoot");
            var canvasGo = new GameObject("~Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(_root.transform);
            _slot = (RectTransform)canvasGo.transform;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
        }

        [UnityTest]
        public IEnumerator WellBehavedMicroGame_ReportsNoContractViolations()
        {
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new[] { DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear });
            var def = TestFactory.CreateDefinition("dummy_ok", dummy);

            var harnessGo = new GameObject("~Harness");
            harnessGo.transform.SetParent(_root.transform);
            var harness = harnessGo.AddComponent<MicroGameDebugHarness>();
            harness.Configure(_slot, def, loopCount: 3, useFixedSeed: true, seed: 1, playDuration: 1f, requiredUnits: 3);

            var reports = new List<MicroGameDebugHarness.LoopReport>();
            harness.LoopCompleted += reports.Add;

            bool allDone = false;
            harness.AllLoopsCompleted += () => allDone = true;

            harness.StartLoops();

            float elapsed = 0f;
            while (!allDone && elapsed < 10f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(allDone, "ハーネスが完了しなかった");
            Assert.AreEqual(3, reports.Count);
            foreach (var r in reports)
            {
                Assert.AreEqual(0, r.ContractViolations.Count, "正しく振る舞うミニゲームで違反が検出されてしまった(誤検出)");
                Assert.AreEqual(MicroGameFinishReason.AllUnitsCleared, r.Reason);
                Assert.AreEqual(3, r.Summary.UnitsCleared);
            }
        }

        [UnityTest]
        public IEnumerator MisbehavingMicroGame_ContractViolationIsDetected()
        {
            var dummy = TestFactory.CreateDummyTemplate();
            dummy.ConfigureScript(new[] { DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear, DummyMicroGame.Step.Clear });
            dummy.ViolateContractAfterFinish = true; // ★意図的にFinish後にイベントを発火させる
            var def = TestFactory.CreateDefinition("dummy_bad", dummy);

            var harnessGo = new GameObject("~Harness");
            harnessGo.transform.SetParent(_root.transform);
            var harness = harnessGo.AddComponent<MicroGameDebugHarness>();
            harness.Configure(_slot, def, loopCount: 1, useFixedSeed: true, seed: 1, playDuration: 1f, requiredUnits: 3);

            MicroGameDebugHarness.LoopReport report = null;
            harness.LoopCompleted += r => report = r;

            harness.StartLoops();

            float elapsed = 0f;
            while (report == null && elapsed < 10f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(report, "ハーネスが完了しなかった");
            Assert.Greater(report.ContractViolations.Count, 0,
                "Finish後のイベント発火という契約違反を、ハーネスが検出できなかった(検証手段として機能していない)");
        }
    }
}
