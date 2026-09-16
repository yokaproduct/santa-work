using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Santa.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Santa.Tests
{
    /// <summary>
    /// `AudioManager` の不具合修正(決定ログ §4-6 見-3/見-4、共通仕様 §11.3)を検証する。
    ///
    /// ★`AudioManager` は Awake() で `bgmClips`/`seClips`(private な入れ子struct `ClipEntry` のList)を
    /// 一度だけ辞書へコピーする実装のため、テストは「非アクティブなGameObjectへ Add した上で
    /// リフレクションでフィールドを設定してから Activate する」(このプロジェクトの
    /// `TestFactory.CreateDummyTemplate` と同じ手法)ことで Awake() 実行前に値を仕込む。
    /// </summary>
    public class AudioManagerTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            ServiceLocator.Clear();
        }

        private AudioManager CreateAudioManager(
            out AudioSource bgmSource, out AudioSource[] sePool, string bgmId, string seId)
        {
            _root = new GameObject("~AudioManagerTestRoot");
            _root.SetActive(false);

            var manager = _root.AddComponent<AudioManager>();
            bgmSource = _root.AddComponent<AudioSource>();
            var seGo1 = new GameObject("~Se1", typeof(AudioSource));
            seGo1.transform.SetParent(_root.transform);
            sePool = new[] { seGo1.GetComponent<AudioSource>() };

            var clip = AudioClip.Create("~TestClip", 4410, 1, 44100, false);

            SetPrivateField(manager, "bgmSource", bgmSource);
            SetPrivateField(manager, "sePool", sePool);
            SetPrivateField(manager, "seSameIdCooldown", 0f);

            AddClipEntry(manager, "bgmClips", bgmId, clip);
            AddClipEntry(manager, "seClips", seId, clip);

            _root.SetActive(true); // ここで Awake() が走り、上記フィールドを辞書へコピーする
            return manager;
        }

        [UnityTest]
        public IEnumerator PlayBgm_ToUnregisteredId_StopsThePreviousBgm()
        {
            var manager = CreateAudioManager(out var bgmSource, out _, "bgm_a", "se_a");
            IAudioManager audio = manager;

            audio.PlayBgm("bgm_a");
            yield return null;
            Assert.IsTrue(bgmSource.isPlaying, "登録済みBGMが再生されなかった(テスト前提が崩れている)");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*"));
            audio.PlayBgm("bgm_not_registered");

            Assert.IsFalse(bgmSource.isPlaying,
                "未登録のBGMへ切り替えたのに前のBGMが鳴り続けている(決定ログ §4-6 見-4の再発)");
        }

        [UnityTest]
        public IEnumerator PauseAll_PausesOneShotSe_AndResumeAllRestartsIt()
        {
            var manager = CreateAudioManager(out _, out var sePool, "bgm_a", "se_a");
            IAudioManager audio = manager;

            audio.PlaySe("se_a");
            yield return null;
            Assert.IsTrue(sePool[0].isPlaying, "単発SEが再生されなかった(テスト前提が崩れている)");

            audio.PauseAll();
            Assert.IsFalse(sePool[0].isPlaying,
                "PauseAll() が単発SEを止めていない(決定ログ §4-6 見-3の再発。1.42秒のse_microgame_start等がポーズ中も鳴り続ける)");

            audio.ResumeAll();
            Assert.IsTrue(sePool[0].isPlaying, "ResumeAll() で単発SEが再開しない");
        }

        [UnityTest]
        public IEnumerator StopSe_StopsCurrentlyPlayingLongSe()
        {
            var manager = CreateAudioManager(out _, out var sePool, "bgm_a", "se_a");
            IAudioManager audio = manager;

            audio.PlaySe("se_a");
            yield return null;
            Assert.IsTrue(sePool[0].isPlaying);

            audio.StopSe("se_a");
            Assert.IsFalse(sePool[0].isPlaying,
                "StopSe() で長いSE(se_microgame_start/se_finish相当)を止められない");
        }

        [UnityTest]
        public IEnumerator PlayBgm_AppliesMasterVolumeScale_OnTopOfBgmBaseVolumeScale()
        {
            // ★2026-09-16 追加(ディレクター指示: 全体音量を約7割に)。
            // 既定値 bgmBaseVolumeScale=0.6 / masterVolumeScale=0.7 のとき、
            // スライダー最大(1.0)でも実効音量は 0.6 * 0.7 = 0.42 になること。
            var manager = CreateAudioManager(out var bgmSource, out _, "bgm_a", "se_a");
            IAudioManager audio = manager;

            audio.ApplyVolumeSettings(1f, 1f);
            audio.PlayBgm("bgm_a");
            yield return null;

            Assert.AreEqual(0.42f, bgmSource.volume, 0.001f,
                "masterVolumeScale(既定0.7)がBGMに掛かっていない");
        }

        [UnityTest]
        public IEnumerator PlaySe_AppliesMasterVolumeScale()
        {
            var manager = CreateAudioManager(out _, out var sePool, "bgm_a", "se_a");
            IAudioManager audio = manager;

            audio.ApplyVolumeSettings(1f, 1f);
            audio.PlaySe("se_a");
            yield return null;

            Assert.AreEqual(0.7f, sePool[0].volume, 0.001f,
                "masterVolumeScale(既定0.7)が単発SEに掛かっていない");
        }

        [UnityTest]
        public IEnumerator FadeOutBgm_StopsBgmAfterGivenSeconds()
        {
            var manager = CreateAudioManager(out var bgmSource, out _, "bgm_a", "se_a");
            IAudioManager audio = manager;

            audio.PlayBgm("bgm_a");
            yield return null;
            Assert.IsTrue(bgmSource.isPlaying);

            audio.FadeOutBgm(0.1f);
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.IsFalse(bgmSource.isPlaying, "FadeOutBgm() の時間が経過してもBGMが停止しない");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"フィールドが見つかりません: {fieldName}");
            field.SetValue(target, value);
        }

        /// <summary>
        /// `AudioManager.ClipEntry`(private な入れ子struct)の `List&lt;ClipEntry&gt;` へ、
        /// リフレクションだけでエントリを1件追加する。
        /// </summary>
        private static void AddClipEntry(object target, string listFieldName, string id, AudioClip clip)
        {
            var listField = target.GetType().GetField(listFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(listField, $"フィールドが見つかりません: {listFieldName}");
            var list = listField.GetValue(target);
            var entryType = list.GetType().GetGenericArguments()[0];

            var entry = System.Activator.CreateInstance(entryType);
            entryType.GetField("id").SetValue(entry, id);
            entryType.GetField("clip").SetValue(entry, clip);

            var addMethod = list.GetType().GetMethod("Add");
            addMethod.Invoke(list, new[] { entry });
        }
    }
}
