using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.Core
{
    /// <summary>
    /// <see cref="IAudioManager"/> の実装。共通仕様 00_共通仕様.md §11。
    ///
    /// クリップの実体はまだ用意されていない(素材待ち)ため、Inspectorで
    /// id → AudioClip の対応表を設定する方式にしてある。素材が揃うまでは
    /// 未登録IDを再生しようとしても警告を出すだけで例外にはしない。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class AudioManager : MonoBehaviour, IAudioManager
    {
        [Serializable]
        private struct ClipEntry
        {
            public string id;
            public AudioClip clip;
        }

        [Header("BGM")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private List<ClipEntry> bgmClips = new List<ClipEntry>();

        [Header("SE")]
        [Tooltip("SEの同時多重再生に備えたAudioSourceプール。M5は1.33秒おきに再生されうるため複数用意する(共通仕様 §11.3 / §13-13)。")]
        [SerializeField] private AudioSource[] sePool;
        [SerializeField] private List<ClipEntry> seClips = new List<ClipEntry>();

        [Tooltip("同一SE IDの最短再生間隔(秒)。連打による音割れの簡易対策。0で無効。")]
        [SerializeField] private float seSameIdCooldown = 0.05f;

        [Tooltip("★2026-09-15 追加(決定ログ §4-6 暫-2)。BGMの基準音量の一段下げ。" +
                 "3素材ともピーク-1dBに正規化されておりBGMとSEが同じ大きさに聞こえるため、" +
                 "設定画面の音量スライダー(bgmVolume)とは別に、BGMだけ一律でこの倍率をかける。暫定値。")]
        [SerializeField, Range(0f, 1f)] private float bgmBaseVolumeScale = 0.6f;

        [Tooltip("★2026-09-16 追加(ディレクター指示)。全体の音量が大きすぎるという指摘への対応で、" +
                 "BGM・単発SE・ループSEすべてに掛かる全体倍率。既定0.7。bgmBaseVolumeScale とは独立して" +
                 "掛け合わされる(BGM実効音量 = bgmVolume × bgmBaseVolumeScale × masterVolumeScale)。")]
        [SerializeField, Range(0f, 1f)] private float masterVolumeScale = 0.7f;

        private readonly Dictionary<string, AudioClip> _bgmMap = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> _seMap = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, float> _lastSePlayTime = new Dictionary<string, float>();

        /// <summary>
        /// ★2026-09-15 追加。<see cref="StopSe"/> / <see cref="PauseAll"/> のために、
        /// 直近にどの seId をどの AudioSource で再生したかを覚えておく(共通仕様 §11.3)。
        /// `sePool` は使い回しのプールなので、同じ AudioSource が別の id を再生していたら
        /// 「もう別の音に上書きされている」とみなして何もしない。
        /// </summary>
        private readonly Dictionary<string, AudioSource> _lastSeSource = new Dictionary<string, AudioSource>();
        private readonly Dictionary<AudioSource, string> _seSourceOwner = new Dictionary<AudioSource, string>();

        /// <summary>PauseAll() で一時停止した単発SEの AudioSource(ResumeAll() で戻すため)。</summary>
        private readonly HashSet<AudioSource> _pausedSeSources = new HashSet<AudioSource>();

        private Coroutine _bgmFadeRoutine;

        /// <summary>
        /// ループSE(例: `se_belt`)専用のAudioSource。IDごとに1つ、初回再生時に動的生成する
        /// (共通仕様は単発SEの `sePool` とBGM単一チャンネルしか用意していなかったため、
        /// ループSEチャンネルはこのクラス内で追加した。Prefab/シーンの変更は不要)。
        /// </summary>
        private readonly Dictionary<string, AudioSource> _loopSeSources = new Dictionary<string, AudioSource>();

        /// <summary>PauseAll() で一時停止したループSEのID。ResumeAll() で「元から止まっていたもの」まで
        /// 再生してしまわないよう、実際に止めたものだけを覚えておく。</summary>
        private readonly HashSet<string> _pausedLoopSeIds = new HashSet<string>();

        private string _currentBgmId;
        private int _seCursor;
        private float _bgmVolume = 1f;
        private float _seVolume = 1f;

        private float EffectiveBgmVolume => _bgmVolume * bgmBaseVolumeScale * masterVolumeScale;
        private float EffectiveSeVolume => _seVolume * masterVolumeScale;

        private void Awake()
        {
            foreach (var e in bgmClips)
            {
                if (!string.IsNullOrEmpty(e.id)) _bgmMap[e.id] = e.clip;
            }
            foreach (var e in seClips)
            {
                if (!string.IsNullOrEmpty(e.id)) _seMap[e.id] = e.clip;
            }

            ServiceLocator.Register<IAudioManager>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IAudioManager>();
        }

        public void PlayBgm(string bgmId)
        {
            StopBgmFade();

            if (_currentBgmId == bgmId) return;

            if (!_bgmMap.TryGetValue(bgmId, out var clip) || clip == null)
            {
                // ★2026-09-15 修正(共通仕様 §11.1 見-4 / §10-G10): 未登録のIDへ切り替えるときは
                // 前のBGMを止めて無音にする。以前は _currentBgmId だけ更新して bgmSource は
                // 鳴らしっぱなしにしていたため、bgm_title だけ登録された状態だと
                // ゲーム中も結果画面も bgm_title が流れ続けてしまっていた。
                Debug.LogWarning($"[AudioManager] BGM未登録または未割り当て: {bgmId}(素材待ちの可能性)。前のBGMを停止します。");
                bgmSource.Stop();
                _currentBgmId = null;
                return;
            }

            bgmSource.clip = clip;
            bgmSource.volume = EffectiveBgmVolume;
            bgmSource.loop = true;
            bgmSource.Play();
            _currentBgmId = bgmId;
        }

        public void StopBgm()
        {
            StopBgmFade();
            bgmSource.Stop();
            _currentBgmId = null;
        }

        public void FadeOutBgm(float seconds)
        {
            StopBgmFade();
            if (!bgmSource.isPlaying)
            {
                _currentBgmId = null;
                return;
            }
            _bgmFadeRoutine = StartCoroutine(FadeOutBgmRoutine(Mathf.Max(0f, seconds)));
        }

        private void StopBgmFade()
        {
            if (_bgmFadeRoutine != null)
            {
                StopCoroutine(_bgmFadeRoutine);
                _bgmFadeRoutine = null;
            }
            bgmSource.volume = EffectiveBgmVolume;
        }

        private System.Collections.IEnumerator FadeOutBgmRoutine(float seconds)
        {
            float startVolume = bgmSource.volume;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float ratio = seconds > 0f ? Mathf.Clamp01(t / seconds) : 1f;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, ratio);
                yield return null;
            }
            bgmSource.Stop();
            bgmSource.volume = EffectiveBgmVolume;
            _currentBgmId = null;
            _bgmFadeRoutine = null;
        }

        public void PlaySe(string seId)
        {
            if (seSameIdCooldown > 0f && _lastSePlayTime.TryGetValue(seId, out var last))
            {
                if (Time.unscaledTime - last < seSameIdCooldown) return;
            }

            if (!_seMap.TryGetValue(seId, out var clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] SE未登録または未割り当て: {seId}(素材待ちの可能性)。");
                return;
            }

            if (sePool == null || sePool.Length == 0)
            {
                Debug.LogWarning("[AudioManager] sePool が空です。Inspectorで設定してください。");
                return;
            }

            var source = sePool[_seCursor];
            _seCursor = (_seCursor + 1) % sePool.Length;
            source.volume = EffectiveSeVolume;
            source.Stop(); // ★このソースが直前に再生していた別のSE(あれば)を止めてから使う。
            source.clip = clip;
            source.Play();

            _lastSePlayTime[seId] = Time.unscaledTime;
            _lastSeSource[seId] = source;
            _seSourceOwner[source] = seId;
        }

        public void StopSe(string seId)
        {
            // ★2026-09-15 追加(共通仕様 §11.3)。se_microgame_start(1.42秒)/ se_finish(0.75秒)のような
            // 長いSEを、ポーズ・中断・バックグラウンド移行の際に途中で止めるための手段。
            // sePool は使い回しのプールのため、そのAudioSourceが今も同じidを再生中の場合だけ止める
            // (再利用されて既に別のSEに上書きされていたら何もしない)。
            if (_lastSeSource.TryGetValue(seId, out var source) && source != null &&
                _seSourceOwner.TryGetValue(source, out var owner) && owner == seId)
            {
                source.Stop();
            }
        }

        public void PlayLoopSe(string seId)
        {
            if (_loopSeSources.TryGetValue(seId, out var existing) && existing != null && existing.isPlaying)
            {
                return; // 既に再生中なら頭出しし直さない(PlayBgmと同じ考え方)。
            }

            if (!_seMap.TryGetValue(seId, out var clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] ループSE未登録または未割り当て: {seId}(素材待ちの可能性)。");
                return;
            }

            var source = GetOrCreateLoopSeSource(seId);
            source.clip = clip;
            source.volume = EffectiveSeVolume;
            source.loop = true;
            source.Play();
            _pausedLoopSeIds.Remove(seId);
        }

        public void StopLoopSe(string seId)
        {
            _pausedLoopSeIds.Remove(seId);
            if (_loopSeSources.TryGetValue(seId, out var source) && source != null)
            {
                source.Stop();
            }
        }

        public void StopAllLoopSe()
        {
            _pausedLoopSeIds.Clear();
            foreach (var source in _loopSeSources.Values)
            {
                if (source != null) source.Stop();
            }
        }

        private AudioSource GetOrCreateLoopSeSource(string seId)
        {
            if (_loopSeSources.TryGetValue(seId, out var existing) && existing != null)
            {
                return existing;
            }

            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            _loopSeSources[seId] = source;
            return source;
        }

        public void PauseAll()
        {
            if (bgmSource.isPlaying) bgmSource.Pause();

            foreach (var kv in _loopSeSources)
            {
                if (kv.Value != null && kv.Value.isPlaying)
                {
                    kv.Value.Pause();
                    _pausedLoopSeIds.Add(kv.Key);
                }
            }

            // ★2026-09-15 修正(共通仕様 §11.3 見-3): 以前は単発SE(sePool。PlayOneShotの名残)を
            // 一切止めていなかったため、1.42秒の se_microgame_start がポーズ画面の上で鳴り続けていた。
            // 再生中の sePool ソースも同様に一時停止し、ResumeAll() で復帰させる。
            if (sePool != null)
            {
                foreach (var source in sePool)
                {
                    if (source != null && source.isPlaying)
                    {
                        source.Pause();
                        _pausedSeSources.Add(source);
                    }
                }
            }
        }

        public void ResumeAll()
        {
            if (_currentBgmId != null && !bgmSource.isPlaying) bgmSource.UnPause();

            foreach (var seId in _pausedLoopSeIds)
            {
                if (_loopSeSources.TryGetValue(seId, out var source) && source != null) source.UnPause();
            }
            _pausedLoopSeIds.Clear();

            foreach (var source in _pausedSeSources)
            {
                if (source != null) source.UnPause();
            }
            _pausedSeSources.Clear();
        }

        public void ApplyVolumeSettings(float bgmVolume, float seVolume)
        {
            _bgmVolume = Mathf.Clamp01(bgmVolume);
            _seVolume = Mathf.Clamp01(seVolume);
            bgmSource.volume = EffectiveBgmVolume;

            foreach (var source in _loopSeSources.Values)
            {
                if (source != null) source.volume = EffectiveSeVolume;
            }
        }
    }
}
