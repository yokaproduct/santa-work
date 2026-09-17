using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Santa.Core;
using Santa.Game;
using Santa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// `TitleScreenController`(★2026-09-17 `Screen_ModeSelect` を統合したホーム画面)の検証。
    /// `02_Screen_Title.md` §3.1(記録表示)/§3.3(初回ヒントと告知の出し分け)/§4.3(二重遷移防止)。
    ///
    /// ★`SettingsScreenControllerTests` と同じ方針: GameObjectは非アクティブのまま組み立て、
    /// Awake/OnEnable をリフレクションで明示的に1回ずつ呼ぶ(EditModeではOnEnableが
    /// 自動発火しないため)。`StartCoroutine` を伴う演出(脈動・0.3秒の入力遅延)自体の
    /// 時間経過はPlayModeでないと検証できないため、ここでは同期的に判定できる範囲
    /// (記録表示・出し分け・クリックハンドラの副作用)だけを検証する。
    /// </summary>
    public class TitleScreenControllerTests
    {
        private GameObject _root;
        private TitleScreenRefs _refs;
        private TitleScreenController _controller;
        private FakeSaveManager _save;
        private RankTable _rankTable;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            if (_root != null) Object.DestroyImmediate(_root);
            _root = null;
            _refs = null;
            _controller = null;
            ServiceLocator.Clear();
        }

        private void Build(System.Action<FakeSaveManager> configureSave = null)
        {
            _save = new FakeSaveManager();
            configureSave?.Invoke(_save);
            ServiceLocator.Register<ISaveManager>(_save);

            _rankTable = ScriptableObject.CreateInstance<RankTable>(); // 既定5段(0番目 rank.trainee)

            var mode = ScriptableObject.CreateInstance<GameModeDefinition>();
            SetPrivateField(mode, "id", "time90");
            SetPrivateField(mode, "sessionDuration", 90f);
            SetPrivateField(mode, "rankTable", _rankTable);

            _root = new GameObject("~TitleScreenTestRoot", typeof(RectTransform));
            _root.SetActive(false);

            var safeAreaRoot = NewChild("SafeAreaRoot", _root.transform);
            var contentRoot = NewChild("ContentRoot", _root.transform);
            var bottomReserve = NewChild("BottomReserve", _root.transform);

            var playButtonGo = NewChild("PlayButton_Time90", contentRoot).gameObject;
            var playButton = playButtonGo.AddComponent<Button>();
            var startSessionButton = playButtonGo.AddComponent<StartSessionAndNavigateButton>();
            SetPrivateField(startSessionButton, "mode", mode);
            SetPrivateField(startSessionButton, "target", ScreenId.GamePlay);

            var settingsButton = NewChild("SettingsButton", _root.transform).gameObject.AddComponent<Button>();

            var highScoreValueText = NewChild("HighScoreValueText", contentRoot).gameObject.AddComponent<TextMeshProUGUI>();
            var bestRankValueText = NewChild("BestRankValueText", contentRoot).gameObject.AddComponent<TextMeshProUGUI>();
            var modeTitleText = NewChild("ModeTitleText", playButtonGo.transform).gameObject.AddComponent<TextMeshProUGUI>();
            var modeDescText = NewChild("ModeDescText", playButtonGo.transform).gameObject.AddComponent<TextMeshProUGUI>();

            var firstTimeHint = NewChild("FirstTimeHint", contentRoot).gameObject.AddComponent<CanvasGroup>();
            var comingSoonPanel = NewChild("ComingSoonPanel", contentRoot).gameObject.AddComponent<CanvasGroup>();
            var newContentBadge = NewChild("NewContentBadge", contentRoot).gameObject.AddComponent<CanvasGroup>();

            _refs = _root.AddComponent<TitleScreenRefs>();
            SetPrivateField(_refs, "safeAreaRoot", safeAreaRoot);
            SetPrivateField(_refs, "contentRoot", contentRoot);
            SetPrivateField(_refs, "bottomReserve", bottomReserve);
            SetPrivateField(_refs, "playButtonTime90", playButton);
            SetPrivateField(_refs, "settingsButton", settingsButton);
            SetPrivateField(_refs, "highScoreValueText", (TMP_Text)highScoreValueText);
            SetPrivateField(_refs, "bestRankValueText", (TMP_Text)bestRankValueText);
            SetPrivateField(_refs, "firstTimeHint", firstTimeHint);
            SetPrivateField(_refs, "comingSoonPanel", comingSoonPanel);
            SetPrivateField(_refs, "newContentBadge", newContentBadge);
            SetPrivateField(_refs, "modeTitleText", (TMP_Text)modeTitleText);
            SetPrivateField(_refs, "modeDescText", (TMP_Text)modeDescText);

            _controller = _root.AddComponent<TitleScreenController>();
            // ★0.3秒の入力遅延コルーチンはEditModeでは進行しないため0にしておく(本テストでは検証対象外)。
            var balance = ScriptableObject.CreateInstance<GameBalanceSettings>();
            SetPrivateField(balance, "titleInputDelaySeconds", 0f);
            SetPrivateField(_controller, "balance", balance);

            // ★TitleScreenController.OnEnableはStartCoroutine(脈動演出・0.3秒の入力遅延)を呼ぶが、
            //   GameObjectを非アクティブのまま組み立てる方針(SettingsScreenControllerTests参照)のため
            //   「inactiveなオブジェクトでCoroutineを開始しようとした」というErrorログが出る
            //   (コルーチン自体は例外を投げず、単に進行しないだけ)。このテストで検証したいのは
            //   OnEnable内の同期処理(記録表示・出し分け)だけなので、そのログだけを無視する。
            LogAssert.ignoreFailingMessages = true;

            InvokeLifecycleMethod(_controller, "Awake");
            InvokeLifecycleMethod(_controller, "OnEnable");
        }

        [Test]
        public void OnEnable_FirstLaunch_ShowsHint_HidesComingSoonAndBadge()
        {
            Build(s => s.FirstLaunchDone = false);

            Assert.AreEqual(1f, _refs.FirstTimeHint.alpha, "初回はFirstTimeHintを表示するはず(§3.3)");
            Assert.AreEqual(0f, _refs.ComingSoonPanel.alpha, "初回はComingSoonPanelを出してはいけない(§3.3)");
            Assert.AreEqual(0f, _refs.NewContentBadge.alpha, "NewContentBadgeはMVPでは常に非表示のはず");
            Assert.IsFalse(_refs.FirstTimeHint.blocksRaycasts, "FirstTimeHintはタップ不可のはず");
            Assert.IsFalse(_refs.ComingSoonPanel.blocksRaycasts, "ComingSoonPanelはタップ不可のはず(H6)");
        }

        [Test]
        public void OnEnable_NotFirstLaunch_HidesHint_ShowsComingSoon()
        {
            Build(s => s.FirstLaunchDone = true);

            Assert.AreEqual(0f, _refs.FirstTimeHint.alpha, "2回目以降はFirstTimeHintを出さないはず");
            Assert.AreEqual(1f, _refs.ComingSoonPanel.alpha, "2回目以降はComingSoonPanelを表示するはず(§3.3)");
        }

        [Test]
        public void OnEnable_NoPlaysYet_ShowsDashAndTraineeRank()
        {
            Build(s => s.TotalPlays = 0);

            Assert.AreEqual("—", _refs.HighScoreValueText.text, "未プレイなら「0」ではなく「—」のはず(§3.1)");
            StringAssert.Contains("rank.trainee", _refs.BestRankValueText.text,
                "未プレイなら0番目のランク(rank.trainee)を出すはず(ローカライズ未整備時はキーそのまま許容)");
        }

        [Test]
        public void OnEnable_HasPlays_ShowsFormattedHighScoreAndBestRank()
        {
            Build(s =>
            {
                s.TotalPlays = 5;
                s.HighScore = 3240;
                s.BestRankIndex = 2; // rank.veteran
            });

            Assert.AreEqual("3,240", _refs.HighScoreValueText.text, "ハイスコアが3桁区切りで表示されていない(§3.1)");
            StringAssert.Contains("rank.veteran", _refs.BestRankValueText.text,
                "BestRankIndexに対応するランクのキーが反映されていない");
        }

        [Test]
        public void OnEnable_BestRankIndexOutOfRange_FallsBackToIndexZero()
        {
            Build(s =>
            {
                s.TotalPlays = 3;
                s.BestRankIndex = 999; // 範囲外(データ破損等を想定)
            });

            StringAssert.Contains("rank.trainee", _refs.BestRankValueText.text,
                "範囲外のBestRankIndexは0番目にフォールバックするはず(§3.1)");
        }

        [Test]
        public void TappingPlayButton_SavesFirstLaunchDone_AndDisablesBothButtons()
        {
            Build(s => s.FirstLaunchDone = false);

            _refs.PlayButtonTime90.onClick.Invoke();

            Assert.IsTrue(_save.FirstLaunchDone, "PlayButton_Time90を押したらfirstLaunchDoneを保存するはず(§4.4)");
            Assert.IsFalse(_refs.PlayButtonTime90.interactable, "二重遷移防止のため開始ボタンは非活性になるはず(§4.6)");
            Assert.IsFalse(_refs.SettingsButton.interactable, "二重遷移防止のため設定ボタンも非活性になるはず(§4.6)");
        }

        [Test]
        public void TappingSettingsButton_DisablesBothButtons_WithoutTouchingFirstLaunchDone()
        {
            Build(s => s.FirstLaunchDone = false);

            _refs.SettingsButton.onClick.Invoke();

            Assert.IsFalse(_save.FirstLaunchDone, "設定ボタンではfirstLaunchDoneを立てないはず");
            Assert.IsFalse(_refs.PlayButtonTime90.interactable, "二重遷移防止のため開始ボタンも非活性になるはず(§4.6)");
            Assert.IsFalse(_refs.SettingsButton.interactable);
        }

        [Test]
        public void TappingPlayButtonTwice_DoesNotThrow_AndSavesOnlyConsistently()
        {
            Build();

            _refs.PlayButtonTime90.onClick.Invoke();
            Assert.DoesNotThrow(() => _refs.PlayButtonTime90.onClick.Invoke(),
                "二重タップ(マルチタッチ等)でも例外を出してはならない(§4.6)");
            Assert.IsTrue(_save.FirstLaunchDone);
        }

        private static RectTransform NewChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void InvokeLifecycleMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"lifecycle method not found: {target.GetType().Name}.{methodName}");
            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = null;
            for (var type = target.GetType(); type != null && field == null; type = type.BaseType)
            {
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }
            Assert.IsNotNull(field, $"field not found: {target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }

        private class FakeSaveManager : ISaveManager
        {
            public int SaveVersion => 1;
            public int HighScore { get; set; }
            public int BestRankIndex { get; set; }
            public int TotalUnits { get; set; }
            public int TotalPlays { get; set; }
            public bool FirstLaunchDone { get; set; }
            public float BgmVolume { get; set; }
            public float SeVolume { get; set; }
            public string Language { get; set; } = "";

            public (int cleared, int attempted) GetMicroGameStats(string microGameId) => (0, 0);
            public bool IsMicroIntroSeen(string microGameId) => false;
            public void SetMicroIntroSeen(string microGameId, bool seen) { }
            public void SetRecentQuestions(string microGameId, string commaSeparatedIds) { }

            public bool CommitSessionResult(
                int score, int rankIndex, int clearedUnits,
                IReadOnlyDictionary<string, (int cleared, int missed)> perMicroGame) => false;

            public void ResetAllData(Santa.MicroGames.MicroGameCatalog catalog) { }
        }
    }
}
