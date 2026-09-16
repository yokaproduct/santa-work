using TMPro;
using UnityEngine;

namespace Santa.MicroGames.Address
{
    /// <summary>
    /// 世界地図の地域1つぶんの参照集約(`RegionMarker_Asia` 等、常に6つ)。
    /// `21_MicroGame_Address.md` §2 / §3.1。
    ///
    /// ★2026-09-10: 実素材(色分けされた `WorldMap.png`)の投入に伴い、当たり判定は
    /// 矩形(Button)ではなく <see cref="WorldMapHitTester"/> による色キー判定に置き換わった
    /// (取りまとめ役への報告事項)。このクラスが持つ役割は「地域ID」と「名前・カウンタ表示」に
    /// 縮小されている。旧仕様にあった `button` / `regionImage` フィールドは廃止した
    /// (矩形の当たり判定・矩形色の塗り分けという役目そのものが実素材に置き換わったため)。
    ///
    /// このGameObjectのRectTransformは、対応する色領域の重心位置に配置される
    /// (`WorldMapRegionSampler` による自動計算を初期値とし、以後はディレクターが
    /// Inspector 上で直接 anchoredPosition を手調整できる。CLAUDE.md「UIレイアウトの取り扱い」)。
    /// </summary>
    public class RegionRefs : MonoBehaviour
    {
        [Tooltip("asia / europe / africa / north_america / south_america / oceania のいずれか。" +
                 "Country.regionId および WorldMapHitTester.regionColors の regionId と一致させること。")]
        [SerializeField] private string regionId;

        [SerializeField] private TMP_Text regionNameText;
        [SerializeField] private TMP_Text regionCounterText;

        public string RegionId => regionId;
        public TMP_Text RegionNameText => regionNameText;
        public TMP_Text RegionCounterText => regionCounterText;
    }
}
