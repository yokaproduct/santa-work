using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Santa.Game;
using Santa.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Santa.Tests
{
    /// <summary>
    /// `BootFadeController`(起動時フェードイン)の検証。
    /// ディレクター指示(2026-09-19): 「ゲーム起動時に、フェードインからスタートする仕様にしてください。
    /// 2秒くらいで設定してみて」。
    ///
    /// ★実時間の経過を伴う演出のため、`TitleScreenController` の0.3秒入力遅延と同じ方針で
    /// PlayModeでのみ検証する(EditModeではコルーチンが進行しないため)。
    /// </summary>
    public class BootFadeControllerTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
        }

        private BootFadeController Create(float bootFadeInSeconds, out CanvasGroup group)
        {
            _root = new GameObject("~BootFadeTestRoot", typeof(RectTransform));
            _root.SetActive(false);

            group = _root.AddComponent<CanvasGroup>();
            _root.AddComponent<Image>();

            var controller = _root.AddComponent<BootFadeController>();
            var balance = ScriptableObject.CreateInstance<GameBalanceSettings>();
            ReflectionTestUtil.SetPrivateField(balance, "bootFadeInSeconds", bootFadeInSeconds);
            ReflectionTestUtil.SetPrivateField(controller, "balance", balance);

            _root.SetActive(true); // ここで実際の Awake()/Start() が走る
            return controller;
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
        public IEnumerator OnStart_BeginsFullyOpaque_AndBlocksRaycasts()
        {
            var controller = Create(0.2f, out var group);
            yield return null; // Awake/Start直後の最初のフレーム

            Assert.AreEqual(1f, group.alpha, 0.001f, "起動直後は不透明(alpha=1)から始まるはず");
            Assert.IsTrue(group.blocksRaycasts, "フェード中は入力を遮断するはず");
            Assert.IsTrue(controller.gameObject.activeSelf, "フェード中はオブジェクトが有効のはず");
        }

        [UnityTest]
        public IEnumerator AfterConfiguredDuration_BecomesFullyTransparent_AndStopsBlockingInput()
        {
            var controller = Create(0.2f, out var group);

            yield return WaitUntilOrFail(() => !controller.gameObject.activeSelf, 5f,
                "設定秒数が経過してもフェードが終了しなかった");

            Assert.AreEqual(0f, group.alpha, 0.001f, "終了後はalpha=0のはず");
            Assert.IsFalse(group.blocksRaycasts, "終了後は入力を遮断してはいけない");
        }

        [UnityTest]
        public IEnumerator ZeroDuration_SkipsFade_ImmediatelyTransparentAndUnblocked()
        {
            var controller = Create(0f, out var group);
            yield return null; // Start()は最初のフレームで即座に完了しているはず

            Assert.AreEqual(0f, group.alpha, 0.001f, "0秒指定はフェードなしで即座にalpha=0のはず");
            Assert.IsFalse(group.blocksRaycasts, "0秒指定は入力遮断もしないはず");
            Assert.IsFalse(controller.gameObject.activeSelf, "0秒指定は即座に非活性化するはず");
        }

        [UnityTest]
        public IEnumerator CallingStartTwice_DoesNotReplayFade()
        {
            var controller = Create(0f, out var group);
            yield return null;

            Assert.IsFalse(controller.gameObject.activeSelf, "前提: 0秒指定は既に非活性化しているはず");

            Assert.DoesNotThrow(() => InvokePrivateStart(controller),
                "Start()を再度呼んでも例外を出してはならない(起動時1回だけのガード)");

            Assert.AreEqual(0f, group.alpha, 0.001f, "再呼び出しでalphaが戻ってはいけない(起動時1回だけ)");
            Assert.IsFalse(controller.gameObject.activeSelf, "再呼び出しで再度有効化されてはいけない");
        }

        private static void InvokePrivateStart(object target)
        {
            var method = target.GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "Start method not found");
            method.Invoke(target, null);
        }
    }
}
