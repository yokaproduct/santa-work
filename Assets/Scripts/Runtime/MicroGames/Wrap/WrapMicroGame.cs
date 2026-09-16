using System.Collections;
using System.Collections.Generic;
using Santa.Core;
using UnityEngine;

namespace Santa.MicroGames.Wrap
{
    /// <summary>
    /// M5「ラッピングしわけ」。`23_MicroGame_Wrap.md` の実装。
    ///
    /// ★M5は他の3種目と数え方が違う(同文書 §2)。3問×3ループ = 最大9問。
    /// 3問連続(1ループぶん)すべて正解して初めて 1件(Unit)。1ループに誤答が1つでもあれば
    /// そのループは0件になるが、残り2問はそのまま処理を続けさせる(コンボは切れるが処理は止めない)。
    ///
    /// ベルトの流れ方(§4.2、2026-09-07 ディレクター決定): 一定速度で流れ、箱を処理すると
    /// 後続の箱が加速して詰める。先頭の箱(判定ゾーン内で最も下)が常にアクティブになる。
    ///
    /// ★実装上の判断(取りまとめ役への報告参照): 「判定ゾーン内で最も下の箱」というジオメトリ判定ではなく、
    /// 「キューの先頭(index 0)」をアクティブ判定に使っている。既定値では
    /// `WrapGeneratorSettings.boxSpacing`(180px)と `JudgeZone` の高さ(180px)が一致するため
    /// 結果は同じだが、ジオメトリの境界一致に依存しないぶん、開発チームの技術判断事項 M5-2
    /// (「境界での取り違え」)を構造的に回避できる。
    /// </summary>
    public class WrapMicroGame : MicroGameBase
    {
        private const int InitialVisibleBoxCount = 3;
        private const float PositionEpsilon = 0.01f;
        private static readonly Vector3 ActiveBoxScale = new Vector3(1.1f, 1.1f, 1f);
        private static readonly Color CorrectFlashColor = new Color(0.55f, 0.85f, 0.55f, 1f);
        private static readonly Color WrongFlashColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        [SerializeField] private WrapMicroGameRefs refs;

        private WrapGeneratorSettings _settings;
        private WrapPreset _preset;
        private System.Random _rng;

        private readonly List<WrapBoxRuntime> _boxes = new List<WrapBoxRuntime>();
        private readonly List<GameObject> _pendingFeedback = new List<GameObject>();

        private int _loopSize;      // questionsPerUnit。M5は3。
        private int _totalLoops;    // preset.sequence.Length / _loopSize。M5は3。
        private int _sequenceCursor;
        private int _processedInCurrentLoop;
        private int _currentLoopIndex; // 0始まり
        private bool _currentLoopFailed;

        private int _unitsCleared;
        private int _unitsMissedLoops;
        private int _questionsAnswered;

        private float _stopPositionY;

        private bool _began;
        private bool _finished;
        private bool _paused;

        public override void Prepare(MicroGameContext context)
        {
            _settings = context.Definition.QuestionSource as WrapGeneratorSettings;
            _rng = context.Rng;

            _began = false;
            _finished = false;
            _paused = false;
            _unitsCleared = 0;
            _unitsMissedLoops = 0;
            _questionsAnswered = 0;
            _processedInCurrentLoop = 0;
            _currentLoopIndex = 0;
            _currentLoopFailed = false;
            _sequenceCursor = 0;
            _boxes.Clear();
            _pendingFeedback.Clear();

            if (_settings == null || _settings.Presets == null || _settings.Presets.Count == 0)
            {
                Debug.LogError("[WrapMicroGame] questionSource が WrapGeneratorSettings ではないか、presets が空です。");
                return;
            }

            // ★2026-09-13: 形カテゴリ(R04〜R06)をディレクター決定でいったん出題対象から外した。
            //   データは削除せず、WrapPreset.enabled == false のものを ActivePresets が除外する。
            var activePresets = _settings.ActivePresets;
            if (activePresets == null || activePresets.Count == 0)
            {
                Debug.LogError("[WrapMicroGame] 有効なプリセット(enabled == true)が1件もありません。");
                return;
            }

            // ★共通仕様 §5.2 条件2「カテゴリは1ミニゲーム内では固定(9問を通して変えない)」に従い、
            //   プリセット1件(=カテゴリ1つ)をこのミニゲームの間ずっと使う。ループごとにカテゴリを
            //   切り替える仕組みはデータスキーマ上も存在しない(WrapPreset.category は1件につき1つ)。
            var sessionState = context.GetOrCreateState(() =>
                new WrapSessionState { Pool = new ShuffledPoolCursor<WrapPreset>(activePresets, context.Rng) });
            _preset = sessionState.Pool.Next();

            _loopSize = Mathf.Max(1, context.Definition.QuestionsPerUnit);
            _totalLoops = Mathf.Max(1, _preset.sequence.Length / _loopSize);
            _stopPositionY = GetBoxHalfHeight();

            SetupChutesAndSkip();
            SpawnInitialBoxes();
            UpdateLoopLabel();
            ResetLoopDots();
            SetInteractable(false);
        }

        public override void Begin()
        {
            _began = true;
            SetInteractable(true);

            // se_belt はベルト稼働中ずっとループ再生し、ポーズ・終了で止める(23_MicroGame_Wrap.md §7 / SE-13)。
            // ★ポーズ中の一時停止/再開は IAudioManager.PauseAll/ResumeAll が全ループSEに対して行うため、
            //   ここでは開始(PlayLoopSe)と終了(Finish内のStopLoopSe)だけを扱えばよい。
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlayLoopSe(AudioIds.Se.Belt);
            }
        }

        public override void SetPaused(bool paused)
        {
            _paused = paused;
            SetInteractable(!paused && _began && !_finished);
        }

        public override void Finish(MicroGameFinishReason reason)
        {
            if (_finished) return; // 多重呼び出し防止(共通仕様 §4.3 契約)
            _finished = true;

            SetInteractable(false);

            // se_belt を止める(SE-13: 「開始で鳴らし、ポーズ・終了で止める」)。
            // Judge演出(0.35秒)の間もベルトが鳴り続けないよう、Finish時点で即座に止める。
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.StopLoopSe(AudioIds.Se.Belt);
            }

            // 進行中のアイテム単位の演出を即座に完了状態にする(§4.7)。
            StopAllCoroutines();
            foreach (var go in _pendingFeedback)
            {
                SafeDestroy(go);
            }
            _pendingFeedback.Clear();

            // ★時間切れ等で途中までしか処理できなかったループは、
            //   OnUnitCleared / OnMissed のどちらも発火しない(§4.7 / 共通仕様 §3.4)。
            //   ここでは何もしない(既に処理済みのループぶんは ResolveActiveBox 側で発火済み)。
        }

        public override MicroGameSummary GetSummary()
        {
            return new MicroGameSummary(_unitsCleared, _unitsMissedLoops, _questionsAnswered);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// ベルトの1フレームぶんの進行。`Update()` から呼ばれるが、EditModeテストからも
        /// 決定的な dt を渡して直接呼べるよう public にしてある。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_began || _finished || _paused || _settings == null) return;

            float normalSpeed = _settings.BeltSpeed;
            float catchUpSpeed = normalSpeed * _settings.CatchUpMultiplier;

            for (int i = 0; i < _boxes.Count; i++)
            {
                var box = _boxes[i];
                float floor = i == 0 ? _stopPositionY : _boxes[i - 1].CurrentY + _settings.BoxSpacing;

                if (box.CurrentY > floor + PositionEpsilon)
                {
                    float speed = box.IsCatchingUp ? catchUpSpeed : normalSpeed;
                    box.CurrentY = Mathf.Max(floor, box.CurrentY - speed * deltaTime);
                }

                if (box.CurrentY <= floor + PositionEpsilon)
                {
                    box.CurrentY = floor;
                    box.IsCatchingUp = false;
                }

                if (box.RectTransform != null)
                {
                    var pos = box.RectTransform.anchoredPosition;
                    pos.y = box.CurrentY;
                    box.RectTransform.anchoredPosition = pos;
                    box.RectTransform.localScale = i == 0 ? ActiveBoxScale : Vector3.one;
                }

                // ★判定ゾーン先頭(アクティブ)の箱に光る輪郭を付ける(23_MicroGame_Wrap.md §4.1 / Q-3)。
                //   Sprite未設定のときは無効のまま(拡大表示だけの現状の見た目を保つ。壊れない)。
                if (box.Refs != null && box.Refs.ActiveOutline != null)
                {
                    box.Refs.ActiveOutline.enabled = i == 0 && box.Refs.ActiveOutline.sprite != null;
                }
            }
        }

        // ------------------------------------------------------------------
        // 準備
        // ------------------------------------------------------------------

        private void SetupChutesAndSkip()
        {
            for (int i = 0; i < refs.Chutes.Length; i++)
            {
                var chute = refs.Chutes[i];
                if (i < _preset.chuteSamples.Length)
                {
                    ApplyChuteSample(chute, _preset.chuteSamples[i]);
                }

                int captured = i;
                chute.Button.onClick.RemoveAllListeners();
                chute.Button.onClick.AddListener(() => OnChuteTapped(captured));
            }

            refs.SkipButton.onClick.RemoveAllListeners();
            refs.SkipButton.onClick.AddListener(OnSkipTapped);
        }

        private void ApplyChuteSample(WrapChuteRefs chute, WrapSample sample)
        {
            if (_preset.category == WrapCategory.Color)
            {
                // ★2026-09-13: 完成画像方式(依頼B)。settings に色ごとの完成箱画像があれば、
                //   色の乗算も模様の重ねもせず、見本にもその絵をそのまま出す(箱と見本を同じ絵にする既定案)。
                var boxSprite = _settings != null ? _settings.GetCompletedBoxSprite(sample.colorId) : null;
                if (boxSprite != null)
                {
                    chute.SampleImage.sprite = boxSprite;
                    chute.SampleImage.color = Color.white;
                    if (chute.SamplePattern != null) chute.SamplePattern.enabled = false;
                }
                else
                {
                    // ★完成画像が未設定の間の従来どおりのフォールバック(色 + 模様重ね)。
                    chute.SampleImage.color = sample.color;

                    // ★色覚対策の模様(水玉/ストライプ/チェック)を見本にも重ねる(Q-3: 「見本の模様表示」)。
                    //   箱側(ApplyBoxVisual)は既に patternSprite を反映しているが、見本側には
                    //   表示する要素が無かったため追加した。素材が無い間は非表示のまま(見た目は変わらない)。
                    if (chute.SamplePattern != null)
                    {
                        chute.SamplePattern.sprite = sample.patternSprite;
                        chute.SamplePattern.enabled = sample.patternSprite != null;
                    }
                }
            }
            else
            {
                // ★形カテゴリの見本スプライトは未着手(素材が無い)。素材投入まではニュートラルな色にし、
                //   テキストラベルで形を示す(仕様書§5.3の指示どおり、素材投入前提のプレースホルダ)。
                chute.SampleImage.color = Color.white;
                chute.SampleImage.sprite = sample.shapeSprite; // null のままなら Simple 描画で白の矩形になる
                if (chute.SamplePattern != null) chute.SamplePattern.enabled = false;
            }

            if (chute.SampleLabel != null)
            {
                chute.SampleLabel.text = sample.debugLabel ?? string.Empty;
            }
        }

        private void SpawnInitialBoxes()
        {
            int count = Mathf.Min(InitialVisibleBoxCount, _preset.sequence.Length);
            for (int i = 0; i < count; i++)
            {
                SpawnBox(_stopPositionY + i * _settings.BoxSpacing, isCatchingUp: false);
            }
        }

        private float GetBoxHalfHeight()
        {
            if (refs.BoxItemPrefab == null) return 100f;
            var rt = refs.BoxItemPrefab.GetComponent<RectTransform>();
            return rt != null ? rt.sizeDelta.y * 0.5f : 100f;
        }

        private void SpawnBox(float y, bool isCatchingUp)
        {
            if (_sequenceCursor >= _preset.sequence.Length) return;
            var def = _preset.sequence[_sequenceCursor++];

            var go = Instantiate(refs.BoxItemPrefab, refs.BoxContainer);
            var rt = go.GetComponent<RectTransform>();
            var pos = rt.anchoredPosition;
            pos.y = y;
            rt.anchoredPosition = pos;

            var boxRefs = go.GetComponent<WrapBoxItemRefs>();
            ApplyBoxVisual(boxRefs, def);

            _boxes.Add(new WrapBoxRuntime
            {
                Definition = def,
                GameObject = go,
                RectTransform = rt,
                Refs = boxRefs,
                CurrentY = y,
                IsCatchingUp = isCatchingUp,
            });
        }

        private void ApplyBoxVisual(WrapBoxItemRefs box, WrapBoxDef def)
        {
            if (box == null) return;

            if (def.isTrash)
            {
                box.Background.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                if (box.DebugLabel != null) box.DebugLabel.text = "ごみ";
                if (box.TrashMark != null) box.TrashMark.SetActive(true);
                if (box.Pattern != null) box.Pattern.enabled = false;

                // ★ゴミの絵(つぶれた箱)。素材が無い間は無効のままにして、
                //   従来どおり Background の灰色 + TrashMark の「×」だけで伝わる見た目にフォールバックする
                //   (承認済みの仮案。差し替え方法D → Sprite未設定時は現状の見た目を保つ)。
                if (box.TrashImage != null)
                {
                    var trashSprite = _settings != null ? _settings.TrashSprite : null;
                    box.TrashImage.sprite = trashSprite;
                    box.TrashImage.enabled = trashSprite != null;
                }
                return;
            }

            if (box.TrashMark != null) box.TrashMark.SetActive(false);
            if (box.TrashImage != null) box.TrashImage.enabled = false;

            var sample = _preset.chuteSamples[def.chuteIndex];
            if (_preset.category == WrapCategory.Color)
            {
                // ★2026-09-13: 完成画像方式(依頼B)。settings に色ごとの完成箱画像があれば、
                //   Background の色乗算も Pattern の模様重ねもせず、絵をそのまま出す。
                var boxSprite = _settings != null ? _settings.GetCompletedBoxSprite(sample.colorId) : null;
                if (boxSprite != null)
                {
                    box.Background.sprite = boxSprite;
                    box.Background.color = Color.white;
                    if (box.Pattern != null) box.Pattern.enabled = false;
                }
                else
                {
                    // ★完成画像が未設定の間の従来どおりのフォールバック(色乗算 + 模様重ね)。
                    box.Background.color = sample.color;
                    if (box.Pattern != null)
                    {
                        box.Pattern.sprite = sample.patternSprite; // ★色覚対策。素材が無い間は非表示のまま
                        box.Pattern.enabled = sample.patternSprite != null;
                    }
                }
            }
            else
            {
                // 条件6(§5.2確定): 形カテゴリのときは箱の色をすべて同じにする。
                // ★形の絵は「見本(シュート)」だけでなく箱本体にも必要(Q-3)。既存の Pattern スロットを
                //   色覚対策の模様と共用し、形カテゴリのときだけ shapeSprite(白いシルエット)を表示する。
                box.Background.color = Color.white;
                if (box.Pattern != null)
                {
                    box.Pattern.sprite = sample.shapeSprite;
                    box.Pattern.enabled = sample.shapeSprite != null;
                }
            }

            if (box.DebugLabel != null) box.DebugLabel.text = sample.debugLabel ?? string.Empty;
        }

        // ------------------------------------------------------------------
        // タップ処理(§4.5)
        // ------------------------------------------------------------------

        private void OnChuteTapped(int chuteIndex)
        {
            if (!TryGetActiveBox(out var box)) return;

            PlaySe(AudioIds.Se.Tap);
            bool correct = !box.Definition.isTrash && box.Definition.chuteIndex == chuteIndex;
            ResolveActiveBox(correct);
        }

        private void OnSkipTapped()
        {
            if (!TryGetActiveBox(out var box)) return;

            PlaySe(AudioIds.Se.Tap);
            bool correct = box.Definition.isTrash;
            ResolveActiveBox(correct);
        }

        private bool TryGetActiveBox(out WrapBoxRuntime box)
        {
            box = null;
            if (_finished || !_began || _paused) return false; // ポーズ中・終了後は反応しない
            if (_boxes.Count == 0) return false; // 判定ゾーンに箱が無ければ何もしない(SEも鳴らさない。§4.1)
            box = _boxes[0];
            return true;
        }

        private void ResolveActiveBox(bool correct)
        {
            var box = _boxes[0];
            _boxes.RemoveAt(0);
            _questionsAnswered++;

            PlayItemFeedback(box, correct);

            if (correct)
            {
                PlaySe(box.Definition.isTrash ? AudioIds.Se.Skip : AudioIds.Se.ItemOk);
                SetDotState(_processedInCurrentLoop, DotState.Correct);
            }
            else
            {
                PlaySe(AudioIds.Se.Wrong);
                SetDotState(_processedInCurrentLoop, DotState.Miss);
                _currentLoopFailed = true;
                RaiseMissed(); // ★箱が未処理のまま流れてもここは通らない。誤答タップのときだけ発火する。
            }

            _processedInCurrentLoop++;

            // 後続を詰める(§4.2)。実装の簡略化として、残っている箱すべてに加速フラグを立てる。
            // 既に floor に到達している箱は Tick() 内で即座にフラグが解除されるだけなので副作用は無い。
            foreach (var remaining in _boxes)
            {
                remaining.IsCatchingUp = true;
            }

            if (_sequenceCursor < _preset.sequence.Length)
            {
                float spawnY = (_boxes.Count > 0 ? _boxes[_boxes.Count - 1].CurrentY : _stopPositionY) + _settings.BoxSpacing;
                SpawnBox(spawnY, isCatchingUp: false);
            }

            bool loopFinished = _processedInCurrentLoop >= _loopSize;
            bool wasLastLoop = false;

            if (loopFinished)
            {
                if (_currentLoopFailed)
                {
                    _unitsMissedLoops++;
                }
                else
                {
                    _unitsCleared++;
                    RaiseUnitCleared();
                }

                _currentLoopIndex++;
                wasLastLoop = _currentLoopIndex >= _totalLoops;

                if (!wasLastLoop)
                {
                    _processedInCurrentLoop = 0;
                    _currentLoopFailed = false;
                    UpdateLoopLabel();
                    ResetLoopDots();
                }
            }

            // ★§4.5 手順8: 「3ループ目を終えたら」は9問すべて処理し終えたことを指す。
            //   3ループ目自体が0件(誤答を含む)だった場合でも発火する(=「全件クリア」ではなく
            //   「全問を出題し終えた」の意味。仕様書の文言どおりの実装)。
            if (wasLastLoop)
            {
                RaiseAllUnitsCleared();
            }
        }

        private void PlayItemFeedback(WrapBoxRuntime box, bool correct)
        {
            if (box.GameObject == null) return;

            if (box.Refs != null && box.Refs.Background != null)
            {
                box.Refs.Background.color = correct ? CorrectFlashColor : WrongFlashColor;
            }

            _pendingFeedback.Add(box.GameObject);
            StartCoroutine(ItemFeedbackRoutine(box.GameObject, _settings.ItemFeedbackDuration));
        }

        private IEnumerator ItemFeedbackRoutine(GameObject go, float duration)
        {
            var rt = go.transform as RectTransform;
            var startScale = rt != null ? rt.localScale : Vector3.one;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                if (rt != null)
                {
                    float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                    rt.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
                }
                yield return null;
            }

            _pendingFeedback.Remove(go);
            SafeDestroy(go);
        }

        /// <summary>
        /// EditModeテスト(Unity Test Runner・非Play Mode)から呼ばれた場合に
        /// 「Destroy may not be called from edit mode」エラーにならないための分岐。
        /// 実行時(Play Mode/ビルド後)は通常どおり <see cref="Destroy(Object)"/> を使う。
        /// </summary>
        private void SafeDestroy(GameObject go)
        {
            if (go == null) return;

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        // ------------------------------------------------------------------
        // ループ進捗表示
        // ------------------------------------------------------------------

        private enum DotState
        {
            Unprocessed,
            Correct,
            Miss,
        }

        private void UpdateLoopLabel()
        {
            if (refs.LoopLabelText != null)
            {
                refs.LoopLabelText.text = $"ループ {_currentLoopIndex + 1} / {_totalLoops}";
            }
        }

        private void ResetLoopDots()
        {
            for (int i = 0; i < refs.LoopDots.Length; i++)
            {
                SetDotState(i, DotState.Unprocessed);
            }
        }

        private void SetDotState(int index, DotState state)
        {
            if (index < 0 || index >= refs.LoopDots.Length) return;
            var dot = refs.LoopDots[index];
            if (dot == null) return;

            switch (state)
            {
                case DotState.Unprocessed:
                    if (dot.FillImage != null) dot.FillImage.color = new Color(1f, 1f, 1f, 0.35f);
                    if (dot.MissMark != null) dot.MissMark.SetActive(false);
                    break;
                case DotState.Correct:
                    if (dot.FillImage != null) dot.FillImage.color = new Color(0.3f, 0.75f, 0.35f, 1f);
                    if (dot.MissMark != null) dot.MissMark.SetActive(false);
                    break;
                case DotState.Miss:
                    if (dot.FillImage != null) dot.FillImage.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                    if (dot.MissMark != null) dot.MissMark.SetActive(true);
                    break;
            }
        }

        // ------------------------------------------------------------------
        // 雑用
        // ------------------------------------------------------------------

        private void SetInteractable(bool interactable)
        {
            if (refs == null) return;

            foreach (var chute in refs.Chutes)
            {
                if (chute != null && chute.Button != null) chute.Button.interactable = interactable;
            }

            if (refs.SkipButton != null) refs.SkipButton.interactable = interactable;
        }

        private void PlaySe(string seId)
        {
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(seId);
            }
        }
    }
}
