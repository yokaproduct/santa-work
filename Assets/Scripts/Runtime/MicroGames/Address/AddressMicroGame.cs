using System.Collections;
using System.Collections.Generic;
using Santa.Core;
using UnityEngine;

namespace Santa.MicroGames.Address
{
    /// <summary>
    /// M2「住所でしわけろ」。`21_MicroGame_Address.md` §4 の実装。
    /// 構造は `LetterMicroGame`(M1)に合わせている(★お手本。独自の流儀を持ち込まない)。
    ///
    /// ★2026-09-10: 実素材(6地域を色分けした `WorldMap.png`)の投入に伴い、当たり判定を
    /// 矩形の `Button` × 6 から <see cref="WorldMapHitTester"/> による色キー判定へ置き換えた
    /// (取りまとめ役への実装依頼。開発チーム技術判断 M2-1 の決定)。
    /// </summary>
    public class AddressMicroGame : MicroGameBase
    {
        [SerializeField] private AddressMicroGameRefs refs;

        // ★演出のタイミング(仮値)。判定演出(共通仕様 §3.8。Part_JudgeEffect)とは別物で、
        // これはM2内部の「押した地域そのもの」への即時フィードバック(共通仕様 §4.5 のM5同様、
        // ミニゲーム内部の演出として閉じる)。
        private const float PressEffectDuration = 0.05f;
        private const float ResultEffectDuration = 0.2f;

        private CountryTable _table;
        private AddressSessionState _sessionState;
        private Country _currentCountry;
        private readonly List<Coroutine> _activeEffectCoroutines = new List<Coroutine>();

        private int _requiredUnits;
        private int _unitsCleared;
        private int _unitsMissed;
        private bool _began;
        private bool _finished;

        public override void Prepare(MicroGameContext context)
        {
            _table = context.Definition.QuestionSource as CountryTable;
            _requiredUnits = context.RequiredUnits;
            _unitsCleared = 0;
            _unitsMissed = 0;
            _began = false;
            _finished = false;
            _activeEffectCoroutines.Clear();

            if (_table == null || _table.Countries.Count == 0)
            {
                Debug.LogError("[AddressMicroGame] questionSource が CountryTable ではないか、空です。");
                return;
            }

            // 共通仕様 §4.7: セッションを通じて地図の累積カウンタと出題プールを持ち越す。
            _sessionState = context.GetOrCreateState(() => new AddressSessionState
            {
                Pool = new ShuffledPoolCursor<Country>(_table.Countries, context.Rng),
            });

            BindWorldMap();
            RefreshAllRegionCounters();
            LoadNextCountry();
            SetRegionsInteractable(false);
        }

        public override void Begin()
        {
            _began = true;
            SetRegionsInteractable(true);
        }

        public override void SetPaused(bool paused)
        {
            SetRegionsInteractable(!paused && _began && !_finished);
            if (paused)
            {
                StopAllRegionEffectsAndResetVisuals();
            }
        }

        public override void Finish(MicroGameFinishReason reason)
        {
            _finished = true;
            SetRegionsInteractable(false);
            StopAllRegionEffectsAndResetVisuals();
        }

        public override MicroGameSummary GetSummary()
        {
            return new MicroGameSummary(_unitsCleared, _unitsMissed, _unitsCleared + _unitsMissed);
        }

        /// <summary>
        /// 世界地図の「座標→地域ID」対応表をセッション状態から取得(無ければ1回だけビルド)し、
        /// `WorldMapHitTester` に注入する(§4.7 と同じ「一度だけビルドして使い回す」方式)。
        /// `WorldMap.png` のフル解像度ピクセルを毎回読み直さないための仕組み(取りまとめ役への報告事項)。
        /// </summary>
        private void BindWorldMap()
        {
            var hitTester = refs.WorldMapHitTester;
            if (hitTester == null) return;

            if (_sessionState.MapLookup == null && hitTester.SourceTexture != null && hitTester.RegionColors != null)
            {
                _sessionState.MapLookup = WorldMapRegionSampler.Build(
                    hitTester.SourceTexture,
                    hitTester.RegionColors,
                    hitTester.LookupWidth,
                    hitTester.LookupHeight,
                    hitTester.MatchThreshold);
            }

            hitTester.SetLookupTable(_sessionState.MapLookup);
            hitTester.RegionTapped -= OnRegionTapped; // ★二重購読の防止(通常はPrefab再生成のたびに新規インスタンスだが念のため)。
            hitTester.RegionTapped += OnRegionTapped;
        }

        private void LoadNextCountry()
        {
            _currentCountry = _sessionState.Pool.Next();

            if (refs.FlagImage != null)
            {
                // ★プロトタイプでは flagSprite が未設定(null)のことがある。その場合 Image は
                // 単色の矩形(画像枠)として表示される(CLAUDE.md: Type=Simple なら Sprite未設定でも描画される)。
                refs.FlagImage.sprite = _currentCountry.flagSprite;
            }

            if (refs.CountryNameText != null)
            {
                string lang = ServiceLocator.TryGet<ILocalizationService>(out var loc) ? loc.CurrentLanguage : "ja";
                refs.CountryNameText.text = lang == "en" ? _currentCountry.nameEn : _currentCountry.nameJa;
            }
        }

        private void OnRegionTapped(string regionId, Vector2 localPoint)
        {
            if (_finished || _currentCountry == null) return;

            var region = FindRegion(regionId);
            if (region == null) return; // ★対応表が返した地域IDに一致する RegionRefs が無い(設定ミス)。安全側に倒して無視する。

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Tap);
            }

            SetRegionsInteractable(false); // 連打による二重回答の防止(共通仕様 §4.1)

            bool correct = region.RegionId == _currentCountry.regionId;
            if (correct)
            {
                _unitsCleared++;
                int delivered = IncrementDelivered(region.RegionId);
                UpdateRegionCounterText(region, delivered);
                PlayRegionFeedback(region, localPoint, correctHit: true);
                RaiseUnitCleared();
            }
            else
            {
                _unitsMissed++;
                PlayRegionFeedback(region, localPoint, correctHit: false);
                RaiseMissed();
            }

            if (_unitsCleared >= _requiredUnits)
            {
                RaiseAllUnitsCleared();
                return; // 共通側が Finish() を呼ぶまで、これ以上操作させない(LetterMicroGameと同じ設計)
            }

            LoadNextCountry();
            SetRegionsInteractable(true);
        }

        private RegionRefs FindRegion(string regionId)
        {
            if (refs.Regions == null) return null;
            foreach (var region in refs.Regions)
            {
                if (region != null && region.RegionId == regionId) return region;
            }
            return null;
        }

        private int IncrementDelivered(string regionId)
        {
            _sessionState.DeliveredPerRegion.TryGetValue(regionId, out int count);
            count++;
            _sessionState.DeliveredPerRegion[regionId] = count;
            return count;
        }

        private void RefreshAllRegionCounters()
        {
            if (refs.Regions == null) return;

            foreach (var region in refs.Regions)
            {
                if (region == null) continue;
                _sessionState.DeliveredPerRegion.TryGetValue(region.RegionId, out int count);
                UpdateRegionCounterText(region, count);
            }
        }

        private static void UpdateRegionCounterText(RegionRefs region, int count)
        {
            if (region.RegionCounterText != null)
            {
                region.RegionCounterText.text = count.ToString();
            }
        }

        private void SetRegionsInteractable(bool interactable)
        {
            if (refs.WorldMapHitTester != null)
            {
                refs.WorldMapHitTester.InputEnabled = interactable;
            }
        }

        /// <summary>
        /// 押した地域そのものへの即時フィードバック(共通仕様 §3.1「押下」→「正解」/「不正解」)。
        /// 正解・不正解ともにまず同じ「押下」演出を挟み、そのあとで分岐する。
        /// ★正解を示す演出(光る)はこの地域自身にだけ起きる。他地域や正解の地域を示す演出は行わない
        /// (`21_MicroGame_Address.md` §1.1「正解は一切示さない」)。
        ///
        /// ★2026-09-10: 地図が1枚のスプライトになり地域ごとのImageが無くなったため、
        /// 「押した座標そのものに出す共通マーカー」+「地域名・カウンタのパンチスケール」に変更した
        /// (旧実装は地域のImage.colorを直接塗り替えていた。取りまとめ役への報告事項)。
        /// </summary>
        private void PlayRegionFeedback(RegionRefs region, Vector2 localPoint, bool correctHit)
        {
            var coroutine = StartCoroutine(RegionFeedbackRoutine(region, localPoint, correctHit));
            _activeEffectCoroutines.Add(coroutine);
        }

        private IEnumerator RegionFeedbackRoutine(RegionRefs region, Vector2 localPoint, bool correctHit)
        {
            var marker = refs.TapFeedbackImage;
            if (marker != null)
            {
                marker.gameObject.SetActive(true);
                marker.rectTransform.anchoredPosition = localPoint;
                marker.rectTransform.localScale = Vector3.one * 0.6f;
                marker.color = Color.white;
            }

            region.transform.localScale = Vector3.one * 0.95f;
            yield return new WaitForSeconds(PressEffectDuration);

            // 正解/不正解で分岐。正解=光る(明るいまま少し膨らむ)。不正解=赤み方向へ沈む。
            if (marker != null)
            {
                marker.color = correctHit ? new Color(0.55f, 1f, 0.6f, 0.9f) : new Color(1f, 0.4f, 0.4f, 0.9f);
                marker.rectTransform.localScale = Vector3.one * (correctHit ? 1.4f : 1.1f);
            }
            region.transform.localScale = correctHit ? Vector3.one * 1.03f : Vector3.one * 0.92f;

            if (correctHit && region.RegionCounterText != null)
            {
                StartCoroutine(PunchScale(region.RegionCounterText.transform));
            }

            yield return new WaitForSeconds(ResultEffectDuration);

            region.transform.localScale = Vector3.one;
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
            }
        }

        private static IEnumerator PunchScale(Transform target)
        {
            target.localScale = Vector3.one * 1.3f;
            yield return new WaitForSeconds(0.15f);
            target.localScale = Vector3.one;
        }

        private void StopAllRegionEffectsAndResetVisuals()
        {
            foreach (var coroutine in _activeEffectCoroutines)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            _activeEffectCoroutines.Clear();

            if (refs.TapFeedbackImage != null)
            {
                refs.TapFeedbackImage.gameObject.SetActive(false);
            }

            if (refs.Regions == null) return;

            foreach (var region in refs.Regions)
            {
                if (region == null) continue;

                region.transform.localScale = Vector3.one;
                if (region.RegionCounterText != null)
                {
                    region.RegionCounterText.transform.localScale = Vector3.one;
                }
            }
        }
    }
}
