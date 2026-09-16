using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames.Address
{
    /// <summary>`MicroGame_Address.prefab` の参照集約。`21_MicroGame_Address.md` §2。</summary>
    public class AddressMicroGameRefs : MonoBehaviour
    {
        [Tooltip("国旗。★プロトタイプは画像枠のみ(Sprite未設定)。ディレクターが後日 flagSprite を差し込む。")]
        [SerializeField] private Image flagImage;

        [SerializeField] private TMP_Text countryNameText;

        [Tooltip("常に6。アジア/ヨーロッパ/アフリカ/北アメリカ/南アメリカ/オセアニア(共通仕様 設計原則2)。")]
        [SerializeField] private RegionRefs[] regions;

        [Tooltip("世界地図への色キー判定タップ入力(2026-09-10 実素材投入に伴い追加)。")]
        [SerializeField] private WorldMapHitTester worldMapHitTester;

        [Tooltip("押した地点に一瞬だけ出す共通の演出マーカー。地域ごとに個別のImageを持たなくなったため" +
                 "(地図が1枚のスプライトになったため)、タップ座標そのものにフィードバックを出す方式に変更した。" +
                 "取りまとめ役への報告事項。")]
        [SerializeField] private Image tapFeedbackImage;

        public Image FlagImage => flagImage;
        public TMP_Text CountryNameText => countryNameText;
        public RegionRefs[] Regions => regions;
        public WorldMapHitTester WorldMapHitTester => worldMapHitTester;
        public Image TapFeedbackImage => tapFeedbackImage;
    }
}
