using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Santa.MicroGames.Address
{
    /// <summary>
    /// 世界地図(`WorldMap.png`)への色キー判定タップ入力。取りまとめ役の実装依頼(2026-09-10)。
    /// `21_MicroGame_Address.md` §3.3「輪郭の内側だけがタップできる」を、矩形ではなく
    /// 実素材の塗り色そのもので実現する(開発チーム技術判断 M2-1 の決定)。
    ///
    /// ★この当たり判定を成立させる <see cref="RegionLookupTable"/> は自分でビルドしない。
    /// `AddressMicroGame.Prepare()` が `AddressSessionState`(セッション永続)経由で1回だけビルドし、
    /// <see cref="SetLookupTable"/> で注入する。理由: `MicroGame_Address` のPrefabはM2が終わるたびに
    /// 破棄されるため、このコンポーネント自身に持たせると毎回フルサイズテクスチャを読み直してしまう。
    /// </summary>
    public class WorldMapHitTester : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("地図の表示矩形。この矩形に対する相対位置で色を判定する。")]
        [SerializeField] private RectTransform mapRect;

        [Tooltip("判定に使う元テクスチャ(WorldMap.png)。Read/Write Enabled が必要。")]
        [SerializeField] private Texture2D sourceTexture;

        [Tooltip("6地域ぶんの代表色(塗り)。WorldMap.png から実際にサンプリングした値。")]
        [SerializeField] private RegionColorEntry[] regionColors;

        [Tooltip("代表色との距離がこの値以下のときだけ地域として判定する(RGB各0〜255のユークリッド距離)。" +
                 "大きすぎると海や輪郭を誤判定し、小さすぎると塗りの内部でも取りこぼす。")]
        [SerializeField] private float matchThreshold = 16f;

        [Tooltip("判定表(座標→地域ID)の解像度。間引くほどメモリは小さくなるが判定が粗くなる。")]
        [SerializeField] private int lookupWidth = 480;

        [SerializeField] private int lookupHeight = 270;

        /// <summary>地域がタップされた(regionId, タップ位置の地図ローカル座標)。
        /// ★C#の `event` ではなく素のフィールドにしているのは、EditModeテストから直接
        /// 発火してAddressMicroGame側のハンドラを駆動できるようにするため(ポインタ入力の
        /// 実配線を経由しなくても契約テストが書けるようにする、取りまとめ役への報告事項)。</summary>
        public Action<string, Vector2> RegionTapped;

        /// <summary>タップを受け付けるか。`Button.interactable` の代わり(共通仕様の当たり判定ON/OFFに相当)。</summary>
        public bool InputEnabled { get; set; }

        private RegionLookupTable _table;

        public RectTransform MapRect => mapRect;
        public Texture2D SourceTexture => sourceTexture;
        public IReadOnlyList<RegionColorEntry> RegionColors => regionColors;
        public float MatchThreshold => matchThreshold;
        public int LookupWidth => lookupWidth;
        public int LookupHeight => lookupHeight;
        public RegionLookupTable Table => _table;

        /// <summary>セッション側でビルド済みの対応表を注入する(§4.7 と同じ「一度だけビルドして使い回す」方式)。</summary>
        public void SetLookupTable(RegionLookupTable table)
        {
            _table = table;
        }

        /// <summary>地図ローカル座標(mapRect の rect 基準)から地域IDを引く。海・輪郭は false。</summary>
        public bool TryGetRegionIdAtLocalPoint(Vector2 localPoint, out string regionId)
        {
            regionId = null;
            if (_table == null || mapRect == null) return false;

            Rect rect = mapRect.rect;
            if (rect.width <= 0f || rect.height <= 0f) return false;

            float u = (localPoint.x - rect.xMin) / rect.width;
            float v = (localPoint.y - rect.yMin) / rect.height;
            return _table.TryGetRegionId(new Vector2(u, v), out regionId);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!InputEnabled || mapRect == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    mapRect, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                return;
            }

            if (TryGetRegionIdAtLocalPoint(localPoint, out var regionId))
            {
                RegionTapped?.Invoke(regionId, localPoint);
            }
            // ★海・輪郭上のタップは無反応(21_MicroGame_Address.md §3.3)。SEも鳴らさない・判定もしない。
        }
    }
}
