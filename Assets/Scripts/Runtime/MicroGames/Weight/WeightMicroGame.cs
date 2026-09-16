using System;
using System.Collections;
using System.Linq;
using Santa.Core;
using UnityEngine;

namespace Santa.MicroGames.Weight
{
    /// <summary>
    /// M3「重さでしわけろ」。`22_MicroGame_Weight.md` §4 の実装。
    ///
    /// ★この種目は「そり3台ぶんの問題をPrepareで一括生成し、以後はプールにアクセスしない」という
    /// 契約(共通仕様 §4.3-2)を、セッション状態を持たない形で満たす。M1(手紙)は複数プレイをまたいで
    /// 問題プールをシャッフルし続ける必要があるため `IMicroGameSessionState` を使うが、
    /// M3は `WeightQuestionGenerator` が毎回その場で有効な問題を作れるプロシージャル生成のため、
    /// セッションを跨いで持ち越す状態を持たない(取りまとめ役への報告に判断理由を記載)。
    /// </summary>
    public class WeightMicroGame : MicroGameBase
    {
        /// <summary>★2026-09-16 ディレクター決定: 選択できる箱は最大3個まで。</summary>
        private const int MaxSelectedBoxes = 3;

        [SerializeField] private WeightMicroGameRefs refs;

        private WeightGeneratorSettings _settings;
        private System.Random _rng;
        private int _requiredUnits;
        private int _unitsCleared;
        private bool _began;
        private bool _finished;
        private bool _paused;
        private bool _sliding;

        private WeightPreset[] _units;
        private int _unitIndex;
        private int[] _boxValues; // 現在のそりにおける箱スロットごとの数値(シャッフル済み)
        private bool[] _selected;
        private Vector2[] _boxHomePositions;
        private int _currentTarget;
        private int _currentMeterMax;
        private bool _overflow;

        private Coroutine _slideCoroutine;

        public override void Prepare(MicroGameContext context)
        {
            _settings = context.Definition.QuestionSource as WeightGeneratorSettings;
            _rng = context.Rng;
            _requiredUnits = context.RequiredUnits;
            _unitsCleared = 0;
            _began = false;
            _finished = false;
            _paused = false;
            _sliding = false;

            if (_settings == null)
            {
                Debug.LogError("[WeightMicroGame] questionSource が WeightGeneratorSettings ではありません。");
                return;
            }

            if (refs.Boxes == null || refs.Boxes.Length != _settings.BoxCount)
            {
                Debug.LogError($"[WeightMicroGame] 箱の数が boxCount({_settings.BoxCount})と一致しません。");
            }

            // 箱の元の位置(選択解除時に戻す基準)を最初の1回だけ記録する(§3.1)。
            _boxHomePositions = refs.Boxes.Select(b => b.RectTransform.anchoredPosition).ToArray();

            for (int i = 0; i < refs.Boxes.Length; i++)
            {
                int capturedIndex = i;
                var box = refs.Boxes[i];
                box.Button.onClick.RemoveAllListeners();
                box.Button.onClick.AddListener(() => OnBoxTapped(capturedIndex));
            }

            // ★3台ぶんをまとめて用意する(Begin以降は生成器へアクセスしない。共通仕様 §4.3 / §3.3)。
            _units = new WeightPreset[_requiredUnits];
            for (int i = 0; i < _requiredUnits; i++)
            {
                _units[i] = WeightQuestionGenerator.Generate(_settings, _rng);
            }

            _unitIndex = 0;
            SetupUnit(_units[0]);

            SetBoxesInteractable(false);
        }

        public override void Begin()
        {
            _began = true;
            if (!_sliding)
            {
                SetBoxesInteractable(true);
            }
        }

        public override void SetPaused(bool paused)
        {
            _paused = paused;
            // ★ポーズ中はスライド演出も止まる(SlideSledUnit 内で _paused を見て停止する。§4.5)。
            SetBoxesInteractable(!paused && _began && !_finished && !_sliding);
        }

        public override void Finish(MicroGameFinishReason reason)
        {
            _finished = true;
            SetBoxesInteractable(false);

            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
                _slideCoroutine = null;
            }
            _sliding = false;

            if (refs.SledUnit != null)
            {
                // 演出を即座に完了状態にする(共通仕様 §3.3 [Teardown] は1フレームで終える必要がある)。
                refs.SledUnit.anchoredPosition = Vector2.zero;
            }
            // ★OnUnitCleared / OnMissed をここで発火しない(§4.6)。途中まで積んでいたそりは無かったことになる。
        }

        public override MicroGameSummary GetSummary()
        {
            // M3はオーバーを誤答扱いにしないため UnitsMissed は構造上つねに0(§4.7)。
            return new MicroGameSummary(_unitsCleared, 0, _unitsCleared);
        }

        private void SetupUnit(WeightPreset preset)
        {
            int[] shuffledValues = (int[])preset.boxValues.Clone();
            Shuffle(shuffledValues, _rng);
            _boxValues = shuffledValues;
            _selected = new bool[refs.Boxes.Length];
            _overflow = false;

            for (int i = 0; i < refs.Boxes.Length; i++)
            {
                refs.Boxes[i].ValueText.text = _boxValues[i].ToString();
                SetBoxSelected(i, false);
            }

            _currentTarget = preset.target;
            _currentMeterMax = preset.boxValues.Sum();

            if (refs.TargetValueText != null)
            {
                refs.TargetValueText.text = _currentTarget.ToString();
            }

            UpdateMeterTargetLine();
            UpdateMeterFill(0);
            SetOverflow(false);
            UpdateSledVisual(0, SledVisualState.Idle);
        }

        private void OnBoxTapped(int index)
        {
            if (_sliding || _finished)
            {
                return; // 演出中・終了後のタップは無視する(ボタンのinteractableで基本防いでいるが二重の安全策)
            }

            bool willSelect = !_selected[index];
            if (willSelect && CountSelected() >= MaxSelectedBoxes)
            {
                // ★2026-09-16 ディレクター決定: 3個選択中に未選択の箱をタップしても受け付けない
                //   (ペナルティなし・状態変化なし。SEも鳴らさない)。
                return;
            }

            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Tap);
            }

            _selected[index] = willSelect;
            SetBoxSelected(index, _selected[index]);

            int sum = SumSelected();
            int selectedCount = CountSelected();
            UpdateMeterFill(sum);

            if (sum == _currentTarget)
            {
                SetOverflow(false);
                UpdateSledVisual(selectedCount, SledVisualState.Happy);
                HandleUnitCleared();
            }
            else if (sum > _currentTarget)
            {
                bool wasOverflow = _overflow;
                SetOverflow(true);
                UpdateSledVisual(selectedCount, SledVisualState.Angry);
                if (!wasOverflow && ServiceLocator.TryGet<IAudioManager>(out var audio2))
                {
                    audio2.PlaySe(AudioIds.Se.Overload);
                }
            }
            else
            {
                SetOverflow(false);
                UpdateSledVisual(selectedCount, selectedCount == 0 ? SledVisualState.Idle : SledVisualState.Normal);
            }
        }

        private int CountSelected()
        {
            int count = 0;
            for (int i = 0; i < _selected.Length; i++)
            {
                if (_selected[i]) count++;
            }
            return count;
        }

        private void HandleUnitCleared()
        {
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Sled);
            }

            _unitsCleared++;
            SetBoxesInteractable(false);
            RaiseUnitCleared();

            if (_unitsCleared >= _requiredUnits)
            {
                // ★3台目はスライド演出なしで即終了(§4.4-4)。
                RaiseAllUnitsCleared();
                return;
            }

            _unitIndex++;
            _slideCoroutine = StartCoroutine(SlideToNextUnit());
        }

        private IEnumerator SlideToNextUnit()
        {
            _sliding = true;

            float distance = refs.SledSlideDistance;
            float halfDuration = Mathf.Max(0.01f, _settings.SledSlideDuration) * 0.5f;

            yield return SlideSledUnit(Vector2.zero, new Vector2(distance, 0f), halfDuration);

            // 中身を次の問題に差し替えてから、画面外の左側へテレポートする(★選択状態は全箱リセット済み。§4.4-5)。
            SetupUnit(_units[_unitIndex]);
            refs.SledUnit.anchoredPosition = new Vector2(-distance, 0f);

            yield return SlideSledUnit(new Vector2(-distance, 0f), Vector2.zero, halfDuration);

            _sliding = false;
            _slideCoroutine = null;

            if (!_paused && _began && !_finished)
            {
                SetBoxesInteractable(true);
            }
        }

        private IEnumerator SlideSledUnit(Vector2 from, Vector2 to, float duration)
        {
            if (duration <= 0f)
            {
                refs.SledUnit.anchoredPosition = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // ★SetPaused中はスライド演出も止める(§4.5)。経過時間を進めずに待つ。
                while (_paused)
                {
                    yield return null;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                refs.SledUnit.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }

            refs.SledUnit.anchoredPosition = to;
        }

        private int SumSelected()
        {
            int sum = 0;
            for (int i = 0; i < _selected.Length; i++)
            {
                if (_selected[i])
                {
                    sum += _boxValues[i];
                }
            }
            return sum;
        }

        private void SetBoxSelected(int index, bool selected)
        {
            var box = refs.Boxes[index];
            Vector2 pos = _boxHomePositions[index];
            if (selected)
            {
                pos += new Vector2(0f, refs.BoxSelectedLiftOffset);
            }
            box.RectTransform.anchoredPosition = pos;

            if (box.Shadow != null)
            {
                box.Shadow.SetActive(selected);
            }

            // ★選択中のハイライト枠(22_MicroGame_Weight.md §3.1「浮かせる+影+ハイライト枠」/ Q-3)。
            //   Sprite未設定のときは無効のまま(持ち上げ+影だけの現状の見た目を保つ。壊れない)。
            if (box.SelectionHighlight != null)
            {
                box.SelectionHighlight.enabled = selected && box.SelectionHighlight.sprite != null;
            }
        }

        private void UpdateMeterFill(int sum)
        {
            if (refs.MeterFill == null)
            {
                return;
            }
            refs.MeterFill.fillAmount = _currentMeterMax > 0 ? Mathf.Clamp01((float)sum / _currentMeterMax) : 0f;
        }

        private void UpdateMeterTargetLine()
        {
            if (refs.MeterTargetLine == null || refs.LoadMeterRect == null || _currentMeterMax <= 0)
            {
                return;
            }

            float width = refs.LoadMeterRect.rect.width;
            float x = (float)_currentTarget / _currentMeterMax * width;

            // ★x位置だけを実行時に計算して上書きする。高さ・太さ・色はディレクターの所有物なので触らない(§2.1)。
            Vector2 pos = refs.MeterTargetLine.anchoredPosition;
            pos.x = x;
            refs.MeterTargetLine.anchoredPosition = pos;
        }

        private void SetOverflow(bool overflow)
        {
            _overflow = overflow;
            if (refs.MeterOverflow != null)
            {
                refs.MeterOverflow.SetActive(overflow);
            }
        }

        /// <summary>
        /// そり+トナカイ統合画像(<see cref="WeightMicroGameRefs.SledReindeerImage"/>)の表示状態。
        /// ★2026-09-16 ディレクター決定: 選択数(0〜3)と正誤/オーバー状態の組み合わせで絵を切り替える。
        /// </summary>
        private enum SledVisualState
        {
            Idle,   // 選択数0(初期状態・新しい問題が出たときも常にこれ)
            Normal, // 選択数1〜3、目標値未満
            Happy,  // そり成立(正解)時点
            Angry,  // 重量オーバー時点(従来の「悲しい顔」に相当)
        }

        private void UpdateSledVisual(int selectedCount, SledVisualState state)
        {
            if (refs.SledReindeerImage == null)
            {
                return;
            }

            Sprite sprite = null;
            if (_settings != null)
            {
                switch (state)
                {
                    case SledVisualState.Idle:
                        sprite = _settings.SledReindeerIdleSprite;
                        break;
                    case SledVisualState.Normal:
                        sprite = _settings.GetSledNormalSprite(selectedCount);
                        break;
                    case SledVisualState.Happy:
                        sprite = _settings.GetSledHappySprite(selectedCount);
                        break;
                    case SledVisualState.Angry:
                        sprite = _settings.GetSledAngrySprite(selectedCount);
                        break;
                }
            }

            if (sprite != null)
            {
                refs.SledReindeerImage.sprite = sprite;
                refs.SledReindeerImage.color = Color.white;
            }
            else
            {
                // ★フォールバック: 該当状態のSpriteが未設定の間は壊れないよう、
                //   従来どおり色替え(Angryのときだけ赤み)で代用する(仮置き)。
                refs.SledReindeerImage.sprite = null;
                refs.SledReindeerImage.color = state == SledVisualState.Angry
                    ? new Color(0.85f, 0.45f, 0.45f, 1f)
                    : Color.white;
            }
        }

        private void SetBoxesInteractable(bool interactable)
        {
            if (refs.Boxes == null)
            {
                return;
            }
            foreach (var box in refs.Boxes)
            {
                box.Button.interactable = interactable;
            }
        }

        private static void Shuffle<T>(T[] array, System.Random rng)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
