using Santa.Core;
using Santa.MicroGames;
using Santa.MicroGames.Debugging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.EditorTools
{
    /// <summary>
    /// 特定のミニゲームだけを連続再生できる Editor デバッグ機能。
    /// `10_Screen_GamePlay.md` §7 / 共通仕様 §17.3(d) が要求する「代替検証手段」。
    ///
    /// Screen_GamePlay / GameSessionController を経由せず、MicroGameBase の契約
    /// (Prepare → Begin → …イベント… → Finish → GetSummary)だけで動かす。
    /// Play Mode中のみ動作する。生成する一時オブジェクトはすべて HideFlags.DontSave であり、
    /// シーン・Prefabアセットには一切保存されない。
    ///
    /// これは「継続利用するツール」であり、Scaffold(一度きり)ではないため
    /// Assets/Scripts/Editor/Tools/ に置く(共通仕様 §1.1)。
    /// </summary>
    public class MicroGameDebugWindow : EditorWindow
    {
        private MicroGameDefinition _definition;
        private int _loopCount = 10;
        private bool _useFixedSeed = true;
        private int _seed = 12345;
        private float _playDuration = 12f;
        private int _requiredUnits = 3;

        private GameObject _harnessRoot;
        private MicroGameDebugHarness _harness;
        private int _completedLoops;

        [MenuItem("Tools/デバッグ/特定ミニゲームを連続再生")]
        public static void Open()
        {
            GetWindow<MicroGameDebugWindow>("MicroGame連続再生");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "特定のミニゲームだけを、Screen_GamePlayを経由せず MicroGameBase の契約だけで連続再生します。\n" +
                "Play Mode中のみ実行できます。結果と契約違反の検出はConsoleに出力されます。\n" +
                "(共通仕様 00_共通仕様.md §17.3(d) の代替検証手段)",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _definition = (MicroGameDefinition)EditorGUILayout.ObjectField(
                "種目定義(MicroGameDefinition)", _definition, typeof(MicroGameDefinition), false);
            _loopCount = Mathf.Max(1, EditorGUILayout.IntField("連続再生回数", _loopCount));
            _playDuration = EditorGUILayout.FloatField("ミニゲーム時間(秒。12秒が既定)", _playDuration);
            _requiredUnits = EditorGUILayout.IntField("必要件数(3が既定)", _requiredUnits);
            _useFixedSeed = EditorGUILayout.Toggle("乱数シードを固定する", _useFixedSeed);
            using (new EditorGUI.DisabledScope(!_useFixedSeed))
            {
                _seed = EditorGUILayout.IntField("シード値", _seed);
            }
            EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode に入ってから実行してください。", MessageType.Warning);
                return;
            }

            bool running = _harness != null && _harness.IsRunning;

            using (new EditorGUI.DisabledScope(_definition == null || _definition.Prefab == null || running))
            {
                if (GUILayout.Button("連続再生を開始"))
                {
                    StartHarness();
                }
            }

            using (new EditorGUI.DisabledScope(!running))
            {
                if (GUILayout.Button("停止"))
                {
                    _harness.StopLoops();
                }
            }

            EditorGUILayout.LabelField("完了ループ数", _completedLoops.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("補助機能", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_definition == null))
            {
                if (GUILayout.Button("この種目の初出フラグをリセット(Overlay_MicroGameIntroの確認用)"))
                {
                    if (ServiceLocator.TryGet<ISaveManager>(out var save))
                    {
                        save.SetMicroIntroSeen(_definition.Id, false);
                        Debug.Log($"[MicroGameDebugWindow] '{_definition.Id}' の初出フラグをリセットしました。");
                    }
                    else
                    {
                        Debug.LogWarning("[MicroGameDebugWindow] ISaveManager が未登録です(SceneにServicesが無い可能性)。");
                    }
                }
            }
        }

        private void StartHarness()
        {
            TearDownHarness();

            _harnessRoot = new GameObject("~MicroGameDebugHarness(Editor専用・自動生成/保存されません)")
            {
                hideFlags = HideFlags.DontSave,
            };

            var canvasGo = new GameObject("~DebugCanvas") { hideFlags = HideFlags.DontSave };
            canvasGo.transform.SetParent(_harnessRoot.transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var slotGo = new GameObject("~Slot", typeof(RectTransform)) { hideFlags = HideFlags.DontSave };
            slotGo.transform.SetParent(canvasGo.transform, false);
            var slotRect = (RectTransform)slotGo.transform;
            slotRect.anchorMin = Vector2.zero;
            slotRect.anchorMax = Vector2.one;
            slotRect.offsetMin = Vector2.zero;
            slotRect.offsetMax = Vector2.zero;

            _harness = _harnessRoot.AddComponent<MicroGameDebugHarness>();
            _harness.Configure(slotRect, _definition, _loopCount, _useFixedSeed, _seed, _playDuration, _requiredUnits);

            _completedLoops = 0;
            _harness.LoopCompleted += _ => { _completedLoops++; Repaint(); };
            _harness.AllLoopsCompleted += () => Debug.Log("[MicroGameDebugWindow] 連続再生が完了しました。");

            _harness.StartLoops();
        }

        private void TearDownHarness()
        {
            if (_harnessRoot != null)
            {
                DestroyImmediate(_harnessRoot);
            }
            _harnessRoot = null;
            _harness = null;
        }

        private void OnDisable()
        {
            TearDownHarness();
        }

        private void Update()
        {
            // Play Mode 終了を検知したら後始末する(hideFlags.DontSaveでもドメインリロード等で残る事故を避ける)。
            if (!EditorApplication.isPlaying && _harnessRoot != null)
            {
                TearDownHarness();
            }
        }
    }
}
