#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.MicroGames.Debugging
{
    /// <summary>
    /// 特定のミニゲームだけを、Screen_GamePlay / GameSessionController を経由せず、
    /// MicroGameBase の契約(5メソッド + 3イベント)だけを使って直接・連続的に再生するハーネス。
    ///
    /// 【このツールが存在する理由】共通仕様 00_共通仕様.md §17.3(d) / §15.3-62。
    /// `Screen_Tutorial` の廃止により、MicroGameBase の契約を `Screen_GamePlay` 以外から
    /// 呼ぶ場所が無くなった。契約が本当に汎用か(12月の種目追加に耐えるか)は、
    /// 12月まで実地検証されないまま進むリスクがある。このハーネスが代替の検証手段になる。
    ///
    /// リリースビルドに含めない(#if UNITY_EDITOR || DEVELOPMENT_BUILD)。
    /// </summary>
    public class MicroGameDebugHarness : MonoBehaviour
    {
        public class LoopReport
        {
            public int LoopIndex;
            public MicroGameFinishReason Reason;
            public MicroGameSummary Summary;
            public readonly List<string> ContractViolations = new List<string>();
        }

        /// <summary>1ループ完了ごとに呼ばれる。EditorWindow側のログ表示に使う。</summary>
        public event Action<LoopReport> LoopCompleted;
        public event Action AllLoopsCompleted;

        private RectTransform _slot;
        private MicroGameDefinition _definition;
        private int _loopCount;
        private bool _useFixedSeed;
        private int _seed;
        private float _playDuration = 12f;
        private int _requiredUnits = 3;
        private Coroutine _routine;

        public bool IsRunning => _routine != null;

        public void Configure(
            RectTransform slot,
            MicroGameDefinition definition,
            int loopCount,
            bool useFixedSeed,
            int seed,
            float playDuration = 12f,
            int requiredUnits = 3)
        {
            _slot = slot;
            _definition = definition;
            _loopCount = Mathf.Max(1, loopCount);
            _useFixedSeed = useFixedSeed;
            _seed = seed;
            _playDuration = playDuration;
            _requiredUnits = requiredUnits;
        }

        public void StartLoops()
        {
            StopLoops();
            _routine = StartCoroutine(RunLoops());
        }

        public void StopLoops()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private IEnumerator RunLoops()
        {
            if (_definition == null || _definition.Prefab == null || _slot == null)
            {
                Debug.LogError("[MicroGameDebugHarness] Definition / Prefab / Slot のいずれかが未設定です。");
                yield break;
            }

            // ★System.Random と UnityEngine.Random が同名のため明示的に修飾する。
            var rng = new System.Random(_useFixedSeed ? _seed : Environment.TickCount);
            var stateStore = new Dictionary<string, IMicroGameSessionState>();

            for (int i = 0; i < _loopCount; i++)
            {
                var report = new LoopReport { LoopIndex = i };
                bool afterFinish = false;
                bool allClearedFired = false;

                var instance = Instantiate(_definition.Prefab.gameObject, _slot);
                var microGame = instance.GetComponent<MicroGameBase>();
                if (microGame == null)
                {
                    Debug.LogError("[MicroGameDebugHarness] Prefabのルートに MicroGameBase 継承コンポーネントがありません。");
                    Destroy(instance);
                    yield break;
                }

                void OnUnitCleared()
                {
                    if (afterFinish) report.ContractViolations.Add("Finish後にOnUnitClearedが発火した");
                }

                void OnMissed()
                {
                    if (afterFinish) report.ContractViolations.Add("Finish後にOnMissedが発火した");
                }

                void OnAllUnitsCleared() => allClearedFired = true;

                microGame.OnUnitCleared += OnUnitCleared;
                microGame.OnMissed += OnMissed;
                microGame.OnAllUnitsCleared += OnAllUnitsCleared;

                var context = new MicroGameContext(_definition, _requiredUnits, _playDuration, rng, i, stateStore);

                microGame.Prepare(context);
                yield return null; // Prepareが1フレームで完了しているかの簡易確認(重い処理があればここでフレーム落ちが見える)

                microGame.Begin();

                float t2 = _playDuration;
                while (t2 > 0f && !allClearedFired)
                {
                    yield return null;
                    t2 -= Time.unscaledDeltaTime;
                }

                var reason = allClearedFired ? MicroGameFinishReason.AllUnitsCleared : MicroGameFinishReason.TimeUp;
                // ★Finish()自身が(契約違反として)同期的にイベントを発火する可能性があるため、
                //   Finish()を呼ぶ「前」にフラグを立てる。呼んだ後に立てると、Finish内部からの
                //   同期発火を検出し損なう(実際にこの順序ミスで検出漏れを起こした)。
                afterFinish = true;
                microGame.Finish(reason);

                var summary = microGame.GetSummary();
                report.Reason = reason;
                report.Summary = summary;

                microGame.OnUnitCleared -= OnUnitCleared;
                microGame.OnMissed -= OnMissed;
                microGame.OnAllUnitsCleared -= OnAllUnitsCleared;

                Destroy(instance);
                yield return null; // Teardownが1フレームで完了しているかの簡易確認

                string violations = report.ContractViolations.Count > 0
                    ? " 違反=" + string.Join(", ", report.ContractViolations)
                    : "";
                Debug.Log($"[MicroGameDebugHarness] loop={i} reason={reason} " +
                          $"cleared={summary.UnitsCleared} missed={summary.UnitsMissed} " +
                          $"questions={summary.QuestionsAnswered}{violations}");

                LoopCompleted?.Invoke(report);
            }

            _routine = null;
            AllLoopsCompleted?.Invoke();
        }
    }
}
#endif
