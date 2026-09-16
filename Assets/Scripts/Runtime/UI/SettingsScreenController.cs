using Santa.Core;
using UnityEngine;

namespace Santa.UI
{
    /// <summary>
    /// `Screen_Settings` の仮置きロジック。`04_Screen_Settings.md` §4。
    ///
    /// ★2026-09-16 ディレクター指示による最小構成: S1(BGM音量)/ S2(SE音量)/ S3(言語切替・仮置き)のみ。
    /// もどるボタンは `ScreenNavButton` が単独で完結するため、ここでは扱わない。
    /// </summary>
    public class SettingsScreenController : ScreenControllerBase<SettingsScreenRefs>
    {
        /// <summary>
        /// ★2026-09-16: `se_tap` はまだ実クリップが登録されていない(Main.unityのAudioManagerに
        /// 素材未登録。素材待ち)。SE音量スライダーを離したときの確認音は、登録済みの `se_correct` で
        /// 代用する(取りまとめ役への報告どおり)。素材が揃ったら `AudioIds.Se.Tap` に差し替えること。
        /// </summary>
        private const string SeVolumeConfirmSeId = AudioIds.Se.Correct;

        private float _bgmVolume;
        private float _seVolume;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (ServiceLocator.TryGet<ISaveManager>(out var save))
            {
                _bgmVolume = save.BgmVolume;
                _seVolume = save.SeVolume;
            }
            else
            {
                _bgmVolume = 1f;
                _seVolume = 1f;
            }

            // ★スライダーの初期化はリスナー登録前に行い、SetValueWithoutNotifyで
            //   「読み込んだだけなのに保存し直す」という不要な書き込みを避ける。
            Refs.BgmRow.Slider.SetValueWithoutNotify(_bgmVolume);
            Refs.SeRow.Slider.SetValueWithoutNotify(_seVolume);
            RefreshValueTexts();
            ApplyVolumesToAudioManager();

            Refs.BgmRow.Slider.onValueChanged.AddListener(OnBgmSliderChanged);
            Refs.SeRow.Slider.onValueChanged.AddListener(OnSeSliderChanged);
            if (Refs.BgmRow.ReleaseNotifier != null) Refs.BgmRow.ReleaseNotifier.Released += OnBgmSliderReleased;
            if (Refs.SeRow.ReleaseNotifier != null) Refs.SeRow.ReleaseNotifier.Released += OnSeSliderReleased;

            if (Refs.LanguageRow != null && Refs.LanguageRow.Button != null)
            {
                Refs.LanguageRow.Button.onClick.AddListener(OnLanguageButtonTapped);
            }
            RefreshLanguageText();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Refs.BgmRow.Slider.onValueChanged.RemoveListener(OnBgmSliderChanged);
            Refs.SeRow.Slider.onValueChanged.RemoveListener(OnSeSliderChanged);
            if (Refs.BgmRow.ReleaseNotifier != null) Refs.BgmRow.ReleaseNotifier.Released -= OnBgmSliderReleased;
            if (Refs.SeRow.ReleaseNotifier != null) Refs.SeRow.ReleaseNotifier.Released -= OnSeSliderReleased;

            if (Refs.LanguageRow != null && Refs.LanguageRow.Button != null)
            {
                Refs.LanguageRow.Button.onClick.RemoveListener(OnLanguageButtonTapped);
            }
        }

        private void OnBgmSliderChanged(float value)
        {
            _bgmVolume = value;
            ApplyVolumesToAudioManager();
            RefreshValueTexts();
        }

        private void OnSeSliderChanged(float value)
        {
            _seVolume = value;
            ApplyVolumesToAudioManager();
            RefreshValueTexts();
        }

        /// <summary>§4.2 S1: 離した時点で1回だけ `SaveManager` へ保存する。</summary>
        private void OnBgmSliderReleased()
        {
            if (ServiceLocator.TryGet<ISaveManager>(out var save))
            {
                save.BgmVolume = _bgmVolume;
            }
        }

        /// <summary>§4.2 S2: 離した時点で保存し、音量を聴かせる確認音を鳴らす。</summary>
        private void OnSeSliderReleased()
        {
            if (ServiceLocator.TryGet<ISaveManager>(out var save))
            {
                save.SeVolume = _seVolume;
            }
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(SeVolumeConfirmSeId);
            }
        }

        private void ApplyVolumesToAudioManager()
        {
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.ApplyVolumeSettings(_bgmVolume, _seVolume);
            }
        }

        private void RefreshValueTexts()
        {
            if (Refs.BgmRow.ValueText != null)
            {
                Refs.BgmRow.ValueText.text = Mathf.RoundToInt(_bgmVolume * 100f).ToString();
            }
            if (Refs.SeRow.ValueText != null)
            {
                Refs.SeRow.ValueText.text = Mathf.RoundToInt(_seVolume * 100f).ToString();
            }
        }

        /// <summary>
        /// ★2026-09-16: `LocalizationService` は日本語/英語の切替に対応済みなので実際に切り替える(§4.2 S3)。
        /// `LocalizedText` は `ILocalizationService.LanguageChanged` を購読しているため、
        /// 切り替えれば画面全体の文言が自動で差し替わる。
        /// </summary>
        private void OnLanguageButtonTapped()
        {
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Button);
            }

            if (!ServiceLocator.TryGet<ILocalizationService>(out var loc))
            {
                Debug.LogWarning("[SettingsScreenController] ILocalizationService が見つからず言語を切り替えられません。");
                return;
            }

            string next = loc.CurrentLanguage == "ja" ? "en" : "ja";
            loc.SetLanguage(next);
            RefreshLanguageText();
        }

        private void RefreshLanguageText()
        {
            if (Refs.LanguageRow == null || Refs.LanguageRow.ValueText == null) return;

            if (ServiceLocator.TryGet<ILocalizationService>(out var loc))
            {
                Refs.LanguageRow.ValueText.text = loc.CurrentLanguage == "ja" ? "日本語" : "English";
            }
        }
    }
}
