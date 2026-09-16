using System.Collections;
using System.Collections.Generic;
using Santa.MicroGames;
using UnityEngine;

namespace Santa.Tests
{
    /// <summary>
    /// テスト用の最小ミニゲーム実装。`MicroGameBase` の契約(5メソッド+3イベント)だけで動く
    /// 「一番単純な種目」を用意し、GameSessionController / MicroGameDebugHarness の検証に使う。
    ///
    /// 台本(Steps)をあらかじめ与えておくと、Begin() 呼び出し後に自動でイベントを発火する。
    /// 実際のUIも問題データも持たない、契約だけを再現する最小実装。
    /// </summary>
    public class DummyMicroGame : MicroGameBase
    {
        public enum Step
        {
            Clear,
            Miss,
        }

        // ★[SerializeField] が無い private フィールドは GameObject.Instantiate() のクローン時に
        //   引き継がれない(Unityの複製はシリアライズ経由のため)。テストで Instantiate(_definition.Prefab...)
        //   した際に台本が空になって全イベントが発火しない、という実際に踏んだ不具合の再発防止。
        [SerializeField] private List<Step> _script = new List<Step>();
        [SerializeField] private float _stepIntervalSeconds;
        private int _unitsCleared;
        private int _unitsMissed;
        private int _requiredUnits = 3;
        private bool _paused;
        private bool _finished;
        private Coroutine _routine;

        /// <summary>Finish() の直後に契約違反(Finish後のイベント発火)をわざと起こす。ハーネスの検出テスト用。</summary>
        public bool ViolateContractAfterFinish;

        public int PrepareCallCount { get; private set; }
        public int BeginCallCount { get; private set; }
        public int FinishCallCount { get; private set; }
        public MicroGameFinishReason? LastFinishReason { get; private set; }

        /// <summary>台本を設定する。stepIntervalSeconds が0なら Begin() 直後にまとめて処理する。</summary>
        public void ConfigureScript(IEnumerable<Step> steps, float stepIntervalSeconds = 0f)
        {
            _script = new List<Step>(steps);
            _stepIntervalSeconds = stepIntervalSeconds;
        }

        public override void Prepare(MicroGameContext context)
        {
            PrepareCallCount++;
            _unitsCleared = 0;
            _unitsMissed = 0;
            _finished = false;
            _requiredUnits = context.RequiredUnits;
        }

        public override void Begin()
        {
            BeginCallCount++;
            if (_stepIntervalSeconds <= 0f)
            {
                RunAllStepsImmediately();
            }
            else
            {
                _routine = StartCoroutine(RunStepsOverTime());
            }
        }

        public override void SetPaused(bool paused)
        {
            _paused = paused;
        }

        public override void Finish(MicroGameFinishReason reason)
        {
            FinishCallCount++;
            LastFinishReason = reason;
            _finished = true;
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (ViolateContractAfterFinish)
            {
                // ★契約違反: Finish後にイベントを発火してはならない(共通仕様 §4.3)。
                //   MicroGameDebugHarness がこれを検出できることをテストするための意図的な違反。
                RaiseUnitCleared();
            }
        }

        public override MicroGameSummary GetSummary()
        {
            return new MicroGameSummary(_unitsCleared, _unitsMissed, _unitsCleared + _unitsMissed);
        }

        private void RunAllStepsImmediately()
        {
            foreach (var step in _script)
            {
                if (_finished) break;
                ProcessStep(step);
                if (_unitsCleared >= _requiredUnits)
                {
                    RaiseAllUnitsCleared();
                    break;
                }
            }
        }

        private IEnumerator RunStepsOverTime()
        {
            foreach (var step in _script)
            {
                while (_paused) yield return null;
                yield return new WaitForSeconds(_stepIntervalSeconds);
                if (_finished) yield break;

                ProcessStep(step);
                if (_unitsCleared >= _requiredUnits)
                {
                    RaiseAllUnitsCleared();
                    yield break;
                }
            }
        }

        private void ProcessStep(Step step)
        {
            if (step == Step.Clear)
            {
                _unitsCleared++;
                RaiseUnitCleared();
            }
            else
            {
                _unitsMissed++;
                RaiseMissed();
            }
        }
    }
}
