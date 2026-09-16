using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Santa.Core;
using Santa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.Tests.EditMode
{
    /// <summary>
    /// `SettingsScreenController` の検証(2026-09-16 ディレクター指示の仮置き設定画面)。
    /// `04_Screen_Settings.md` §4.2 の S1/S2(音量スライダー)・S3(言語切替)を扱う。
    ///
    /// ★実際の `Row_SettingsSlider.prefab` / `Row_SettingsButton.prefab` はPrefabInstanceとして
    /// `Screen_Settings.prefab` に組み込まれているが、EditModeテストでは
    /// `WeightMicroGameTests` と同じ方針で、必要な部品(Slider/Button/TMP_Text等)を直接生成し
    /// リフレクションで `SettingsSliderRowRefs` 等へ注入する。
    /// </summary>
    public class SettingsScreenControllerTests
    {
        private GameObject _root;
        private SettingsScreenRefs _refs;
        private FakeSaveManager _save;
        private FakeAudioManager _audio;
        private FakeLocalizationService _loc;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            _root = null;
            _refs = null;
            ServiceLocator.Clear();
        }

        private void Build(float initialBgm = 0.6f, float initialSe = 0.8f)
        {
            _save = new FakeSaveManager { BgmVolume = initialBgm, SeVolume = initialSe };
            _audio = new FakeAudioManager();
            _loc = new FakeLocalizationService();
            ServiceLocator.Register<ISaveManager>(_save);
            ServiceLocator.Register<IAudioManager>(_audio);
            ServiceLocator.Register<ILocalizationService>(_loc);

            _root = new GameObject("~SettingsScreenTestRoot", typeof(RectTransform));
            // ★2026-09-16 判明: EditModeテストでは、GameObjectをactive化しても(AddComponent時・
            //   SetActive(true)時のいずれでも)通常のMonoBehaviourのOnEnableをUnityが自動では
            //   呼ばない(Awakeは呼ばれる場合があるが、OnEnableは呼ばれない/呼ばれるタイミングが
            //   保証されない。PlayModeテストや実機Play時は通常どおりAwake→OnEnableの順で
            //   確実に呼ばれるため、これはテスト実行環境固有の制約であり実装の不具合ではない)。
            //   このためGameObjectは非アクティブのまま組み立て、Awake/OnEnableをリフレクションで
            //   明示的に1回ずつ呼ぶ(自動発火に一切頼らない。二重発火も防げる)。
            _root.SetActive(false);

            var safeAreaRoot = NewChild("SafeAreaRoot", _root.transform);
            var contentRoot = NewChild("ContentRoot", _root.transform);
            var bottomReserve = NewChild("BottomReserve", _root.transform);

            var bgmRow = BuildSliderRow("BgmRow", _root.transform);
            var seRow = BuildSliderRow("SeRow", _root.transform);
            var languageRow = BuildButtonRow("LanguageRow", _root.transform);

            _refs = _root.AddComponent<SettingsScreenRefs>();
            SetPrivateField(_refs, "safeAreaRoot", safeAreaRoot);
            SetPrivateField(_refs, "contentRoot", contentRoot);
            SetPrivateField(_refs, "bottomReserve", bottomReserve);
            SetPrivateField(_refs, "bgmRow", bgmRow);
            SetPrivateField(_refs, "seRow", seRow);
            SetPrivateField(_refs, "languageRow", languageRow);

            var controller = _root.AddComponent<SettingsScreenController>();

            // ★Unityが実行時に呼ぶ順序(Awake→OnEnable)のとおりに明示的に呼び出す。
            InvokeLifecycleMethod(controller, "Awake");
            InvokeLifecycleMethod(controller, "OnEnable");
        }

        [Test]
        public void OnEnable_LoadsSavedVolumes_AndAppliesToAudioManagerImmediately()
        {
            Build(initialBgm: 0.4f, initialSe: 0.9f);

            Assert.AreEqual(0.4f, _refs.BgmRow.Slider.value, 0.001f, "BGMスライダーに保存値が反映されていない");
            Assert.AreEqual(0.9f, _refs.SeRow.Slider.value, 0.001f, "SEスライダーに保存値が反映されていない");
            Assert.AreEqual(1, _audio.ApplyCallCount, "画面表示時にAudioManagerへ音量が反映されていない");
            Assert.AreEqual(0.4f, _audio.LastBgmVolume, 0.001f);
            Assert.AreEqual(0.9f, _audio.LastSeVolume, 0.001f);
        }

        [Test]
        public void DraggingBgmSlider_UpdatesAudioImmediately_ButSavesOnlyOnRelease()
        {
            Build(initialBgm: 0.5f, initialSe: 0.5f);
            float savedAtStart = _save.BgmVolume;

            _refs.BgmRow.Slider.value = 0.8f; // ドラッグ中に相当(onValueChanged)

            Assert.AreEqual(0.8f, _audio.LastBgmVolume, 0.001f,
                "ドラッグ中にAudioManagerへ即座に反映されていない(§4.2 S1)");
            Assert.AreEqual(savedAtStart, _save.BgmVolume, 0.001f,
                "指を離す前にSaveManagerへ保存されてしまった(§4.2 S1: 離した時点で1回だけ保存)");

            _refs.BgmRow.ReleaseNotifier.OnPointerUp(null);

            Assert.AreEqual(0.8f, _save.BgmVolume, 0.001f,
                "指を離した時点でSaveManagerへ保存されていない");
        }

        [Test]
        public void ReleasingSeSlider_SavesVolume_AndPlaysConfirmSe()
        {
            Build();

            _refs.SeRow.Slider.value = 0.3f;
            _refs.SeRow.ReleaseNotifier.OnPointerUp(null);

            Assert.AreEqual(0.3f, _save.SeVolume, 0.001f, "SE音量が保存されていない(§4.2 S2)");
            CollectionAssert.Contains(_audio.PlayedSe, AudioIds.Se.Correct,
                "SEスライダーを離したときの確認音が鳴っていない" +
                "(se_tapは素材未登録のためse_correctで代用する実装のはず)");
        }

        [Test]
        public void TappingLanguageButton_TogglesLanguage_AndUpdatesValueText()
        {
            Build();

            Assert.AreEqual("ja", _loc.CurrentLanguage, "既定言語はjaのはず");
            Assert.AreEqual("日本語", _refs.LanguageRow.ValueText.text, "初期表示が「日本語」になっていない");

            _refs.LanguageRow.Button.onClick.Invoke();

            Assert.AreEqual("en", _loc.CurrentLanguage, "言語がenへ切り替わっていない(§4.2 S3)");
            Assert.AreEqual("English", _refs.LanguageRow.ValueText.text, "表示が English に差し替わっていない");

            _refs.LanguageRow.Button.onClick.Invoke();

            Assert.AreEqual("ja", _loc.CurrentLanguage, "もう一度押すとjaへ戻るはず(トグル動作)");
        }

        [Test]
        public void Prefab_ScreenSettings_HasAllRequiredRowReferences()
        {
            const string prefabPath = "Assets/Prefabs/UI/Screens/Screen_Settings.prefab";
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Assert.Ignore($"{prefabPath} が見つかりません。");
                return;
            }

            var refs = prefab.GetComponent<SettingsScreenRefs>();
            Assert.IsNotNull(refs, "SettingsScreenRefs が付いていない");
            Assert.IsNotNull(refs.SafeAreaRoot, "safeAreaRoot が未設定");
            Assert.IsNotNull(refs.ContentRoot, "contentRoot が未設定");
            Assert.IsNotNull(refs.BottomReserve, "bottomReserve が未設定");
            Assert.IsNotNull(refs.BgmRow, "bgmRow(S1)が未設定");
            Assert.IsNotNull(refs.SeRow, "seRow(S2)が未設定");
            Assert.IsNotNull(refs.LanguageRow, "languageRow(S3・仮置き)が未設定");
            Assert.IsNotNull(refs.BgmRow.Slider, "BGM行のSliderが未配線");
            Assert.IsNotNull(refs.SeRow.Slider, "SE行のSliderが未配線");
            Assert.IsNotNull(refs.LanguageRow.Button, "言語行のButtonが未配線");

            Assert.IsNotNull(prefab.GetComponent<SettingsScreenController>(), "SettingsScreenController が付いていない");
        }

        private static RectTransform NewChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private SettingsSliderRowRefs BuildSliderRow(string name, Transform parent)
        {
            var rowRoot = NewChild(name, parent);

            var labelText = NewChild("LabelText", rowRoot).gameObject.AddComponent<TextMeshProUGUI>();
            var valueText = NewChild("ValueText", rowRoot).gameObject.AddComponent<TextMeshProUGUI>();
            var sliderRect = NewChild("Slider", rowRoot);
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            var releaseNotifier = sliderRect.gameObject.AddComponent<PointerUpNotifier>();

            var rowRefs = rowRoot.gameObject.AddComponent<SettingsSliderRowRefs>();
            SetPrivateField(rowRefs, "labelText", (TMP_Text)labelText);
            SetPrivateField(rowRefs, "slider", slider);
            SetPrivateField(rowRefs, "valueText", (TMP_Text)valueText);
            SetPrivateField(rowRefs, "releaseNotifier", releaseNotifier);
            return rowRefs;
        }

        private SettingsButtonRowRefs BuildButtonRow(string name, Transform parent)
        {
            var rowRoot = NewChild(name, parent);

            var labelText = NewChild("LabelText", rowRoot).gameObject.AddComponent<TextMeshProUGUI>();
            var valueText = NewChild("ValueText", rowRoot).gameObject.AddComponent<TextMeshProUGUI>();
            var button = rowRoot.gameObject.AddComponent<Button>();

            var rowRefs = rowRoot.gameObject.AddComponent<SettingsButtonRowRefs>();
            SetPrivateField(rowRefs, "labelText", (TMP_Text)labelText);
            SetPrivateField(rowRefs, "valueText", (TMP_Text)valueText);
            SetPrivateField(rowRefs, "button", button);
            return rowRefs;
        }

        /// <summary>
        /// `Awake`/`OnEnable` 等、Unityがメッセージとして自動で呼ぶ非public無引数メソッドを
        /// リフレクションで明示的に呼び出す(基底クラス宣言・オーバーライドの両方に対応)。
        /// </summary>
        private static void InvokeLifecycleMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"lifecycle method not found: {target.GetType().Name}.{methodName}");
            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            // private フィールドは派生型の GetField では見えないため、基底型(ScreenRefsBase 等)までさかのぼって探す。
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
            public int HighScore => 0;
            public int BestRankIndex => 0;
            public int TotalUnits => 0;
            public int TotalPlays => 0;
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

        private class FakeAudioManager : IAudioManager
        {
            public float LastBgmVolume;
            public float LastSeVolume;
            public int ApplyCallCount;
            public readonly List<string> PlayedSe = new List<string>();

            public void PlayBgm(string bgmId) { }
            public void StopBgm() { }
            public void PlaySe(string seId) => PlayedSe.Add(seId);
            public void StopSe(string seId) { }
            public void FadeOutBgm(float seconds) { }
            public void PlayLoopSe(string seId) { }
            public void StopLoopSe(string seId) { }
            public void StopAllLoopSe() { }
            public void PauseAll() { }
            public void ResumeAll() { }

            public void ApplyVolumeSettings(float bgmVolume, float seVolume)
            {
                LastBgmVolume = bgmVolume;
                LastSeVolume = seVolume;
                ApplyCallCount++;
            }
        }

        private class FakeLocalizationService : ILocalizationService
        {
            public string CurrentLanguage { get; private set; } = "ja";
            public event Action LanguageChanged;

            public string GetString(string key) => key;
            public string GetString(string key, params object[] args) => key;

            public void SetLanguage(string languageCode)
            {
                CurrentLanguage = languageCode;
                LanguageChanged?.Invoke();
            }
        }
    }
}
