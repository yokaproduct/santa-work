using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.Core
{
    /// <summary>
    /// 画面遷移の常設サービス。Main.unity の Services 配下に置く(共通仕様 §1.2)。
    ///
    /// 【現状の実装方針】画面Prefabは都度 Instantiate / Destroy する(前の画面を破棄してから次を生成)。
    /// 全画面を常駐させるか都度生成するかは開発チーム判断(共通仕様 §13-6。基盤実装時に確定)。
    /// 常駐方式に変える場合はこのクラスの ShowScreen 実装だけを差し替えれば済むよう、
    /// 呼び出し側(ScreenNavButton 等)は <see cref="IScreenFlowService"/> のみに依存させている。
    ///
    /// [DefaultExecutionOrder(-100)] により、他のスクリプトの Awake より先に自分を登録する。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ScreenFlowManager : MonoBehaviour, IScreenFlowService
    {
        [Header("配置先(Main.unity の UICanvas 配下)")]
        [SerializeField] private Transform screenLayer;
        [SerializeField] private Transform overlayLayer;

        [Header("画面Prefab(Screen_*.prefab のルートに ScreenController が付いたもの)")]
        [SerializeField] private List<ScreenEntry> screenPrefabs = new List<ScreenEntry>();

        [Header("オーバーレイPrefab(Overlay_*.prefab)")]
        [SerializeField] private List<OverlayEntry> overlayPrefabs = new List<OverlayEntry>();

        private readonly Dictionary<ScreenId, GameObject> _screenPrefabMap = new Dictionary<ScreenId, GameObject>();
        private readonly Dictionary<OverlayId, GameObject> _overlayPrefabMap = new Dictionary<OverlayId, GameObject>();

        // 優先順位: Pause > MicroGameIntro > Countdown。Confirm はこの優先順位の外(共通仕様 §2.2)。
        private static readonly OverlayId[] PriorityOrder =
        {
            OverlayId.Pause, OverlayId.MicroGameIntro, OverlayId.Countdown,
        };

        // bgm_title を使う画面(共通仕様 §11.1 / 各screens/*.md)。GamePlay(bgm_gameplay)とResult(bgm_result)は
        // GameSessionController が担当するため、ここには含めない。
        // ★2026-09-17: ModeSelect は Screen_Title に統合され廃止(02_Screen_Title.md §0.3)。
        private static readonly HashSet<ScreenId> BgmTitleScreens = new HashSet<ScreenId>
        {
            ScreenId.Title, ScreenId.Settings, ScreenId.Credits,
        };

        private GameObject _currentScreenInstance;
        private GameObject _currentOverlayInstance;
        private OverlayId? _currentOverlayId;

        public ScreenId CurrentScreen { get; private set; }
        public event Action<ScreenId> ScreenChanged;

        private void Awake()
        {
            foreach (var entry in screenPrefabs)
            {
                if (entry.prefab != null) _screenPrefabMap[entry.id] = entry.prefab;
            }
            foreach (var entry in overlayPrefabs)
            {
                if (entry.prefab != null) _overlayPrefabMap[entry.id] = entry.prefab;
            }

            ServiceLocator.Register<IScreenFlowService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IScreenFlowService>();
        }

        public void ShowScreen(ScreenId id)
        {
            if (!_screenPrefabMap.TryGetValue(id, out var prefab) || prefab == null)
            {
                Debug.LogError($"[ScreenFlowManager] {id} のPrefabが未登録です。Inspectorで screenPrefabs を確認してください。");
                return;
            }

            // オーバーレイは画面をまたいで残らない(共通仕様 §2.2 の前提: 画面遷移で下の画面が破棄されるケースは
            // オーバーレイも一緒に閉じる)。
            CloseCurrentOverlay();

            // ★ループSE(se_belt等)の「画面遷移時に確実に止まる」ための安全弁。本来は各ミニゲームが
            // Finish()で自分のループSEを止める契約だが、StartSession経由の強制クリーンアップなど
            // Finish()を経由しない経路もあるため、画面が切り替わる瞬間に必ず全ループSEを止める。
            if (ServiceLocator.TryGet<IAudioManager>(out var loopAudio))
            {
                loopAudio.StopAllLoopSe();
            }

            if (_currentScreenInstance != null)
            {
                Destroy(_currentScreenInstance);
            }

            _currentScreenInstance = Instantiate(prefab, screenLayer);
            CurrentScreen = id;

            PlayScreenTransitionAudio(id);

            ScreenChanged?.Invoke(id);
        }

        /// <summary>
        /// 画面遷移に伴う音声(共通仕様 §11.1/§11.2)。`se_screen` は全画面遷移で共通、
        /// `bgm_title` は Title/ModeSelect/Settings/Credits の4画面で共通のため、
        /// 各画面のControllerに同じ呼び出しを重複して書かせず、ここに集約する。
        /// </summary>
        private void PlayScreenTransitionAudio(ScreenId id)
        {
            if (!ServiceLocator.TryGet<IAudioManager>(out var audio)) return;

            audio.PlaySe(AudioIds.Se.Screen);

            if (BgmTitleScreens.Contains(id))
            {
                audio.PlayBgm(AudioIds.Bgm.Title);
            }
            // GamePlay(bgm_gameplay)は GameSessionController.StartSession、
            // Result(bgm_result)は GameSessionController.EndSessionRoutine が切り替える(既存)。
        }

        public bool ShowOverlay(OverlayId id)
        {
            if (!_overlayPrefabMap.TryGetValue(id, out var prefab) || prefab == null)
            {
                // ★Screenと違いOverlayは「無くても進行できる」設計(GameSessionController側が
                // 未登録時は自動スキップする。共通仕様 §2.2)。開発初期はOverlay_*を意図的に
                // 後回しにできるよう、Errorではなく Warning にとどめる。
                // ★戻り値 false は必ず呼び出し側でハンドリングすること
                // (何も表示していないのに「閉じるまで待つ」ループへ入ると永久に返ってこない)。
                Debug.LogWarning($"[ScreenFlowManager] {id} のPrefabが未登録のため表示をスキップしました。" +
                                  "Inspectorで overlayPrefabs を確認してください。");
                return false;
            }

            // Confirm は優先順位の外(Pauseの上に重ねてよい唯一の例外。共通仕様 §2.2)。
            if (id != OverlayId.Confirm && _currentOverlayId.HasValue && _currentOverlayId.Value != id)
            {
                // 優先度の高いものが出るとき、低いものは閉じてから出す。
                // ★閉じたオーバーレイの復元は「再開する側」の責任(共通仕様 §2.2)。
                //   ここでは単に閉じるだけで、復元ロジックは持たない。
                if (GetPriorityRank(id) < GetPriorityRank(_currentOverlayId.Value))
                {
                    CloseCurrentOverlay();
                }
                else
                {
                    Debug.LogWarning($"[ScreenFlowManager] {_currentOverlayId} 表示中のため {id} の表示要求を無視しました。");
                    return false;
                }
            }

            if (_currentOverlayInstance != null && _currentOverlayId != OverlayId.Confirm)
            {
                Destroy(_currentOverlayInstance);
            }

            _currentOverlayInstance = Instantiate(prefab, overlayLayer);
            _currentOverlayId = id;
            return true;
        }

        public void HideOverlay(OverlayId id)
        {
            if (_currentOverlayId == id)
            {
                CloseCurrentOverlay();
            }
        }

        private void CloseCurrentOverlay()
        {
            if (_currentOverlayInstance != null)
            {
                Destroy(_currentOverlayInstance);
            }
            _currentOverlayInstance = null;
            _currentOverlayId = null;
        }

        private static int GetPriorityRank(OverlayId id)
        {
            for (int i = 0; i < PriorityOrder.Length; i++)
            {
                if (PriorityOrder[i] == id) return i;
            }
            return int.MaxValue; // Confirm 等、優先順位の外
        }

        [Serializable]
        private struct ScreenEntry
        {
            public ScreenId id;
            public GameObject prefab;
        }

        [Serializable]
        private struct OverlayEntry
        {
            public OverlayId id;
            public GameObject prefab;
        }
    }
}
