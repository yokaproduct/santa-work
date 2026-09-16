using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Santa.MicroGames.Weight
{
    /// <summary>`MicroGame_Weight.prefab` の参照集約。`22_MicroGame_Weight.md` §2。</summary>
    public class WeightMicroGameRefs : MonoBehaviour
    {
        [Header("そり(SledUnit1個を使い回し、退場→内容差し替え→入場で表現する)")]
        [SerializeField] private RectTransform sledUnit;
        [SerializeField] private TMP_Text targetValueText;
        [Tooltip("★2026-09-16: SledImage + ReindeerImageを1枚絵(そり+トナカイ+積荷)に統合した要素。")]
        [FormerlySerializedAs("reindeerImage")]
        [SerializeField] private Image sledReindeerImage;

        [Header("そりのスライド演出の距離(速度は WeightGeneratorSettings.SledSlideDuration で調整)")]
        [Tooltip("SledUnitが左右へ抜けるときの anchoredPosition.x のオフセット量。" +
                 "ディレクターが速度・距離を後から調整できるよう、コードにハードコードせずここに置く。")]
        [SerializeField] private float sledSlideDistance = 1200f;

        [Header("積載メーター")]
        [SerializeField] private Image meterFill; // Image.type = Filled / fillMethod = Horizontal
        [SerializeField] private RectTransform loadMeterRect; // 目標線のx位置計算に使う幅の基準
        [SerializeField] private RectTransform meterTargetLine; // ★x位置のみ実行時に上書きする。高さ・太さ・色はディレクターの所有物
        [SerializeField] private GameObject meterOverflow;

        [Header("箱(常に6個・3列×2行)")]
        [Tooltip("選択中の箱を持ち上げる量(anchoredPositionのY方向オフセット)。")]
        [SerializeField] private float boxSelectedLiftOffset = 24f;
        [SerializeField] private WeightBoxRefs[] boxes;

        public RectTransform SledUnit => sledUnit;
        public TMP_Text TargetValueText => targetValueText;
        public Image SledReindeerImage => sledReindeerImage;
        public float SledSlideDistance => sledSlideDistance;
        public Image MeterFill => meterFill;
        public RectTransform LoadMeterRect => loadMeterRect;
        public RectTransform MeterTargetLine => meterTargetLine;
        public GameObject MeterOverflow => meterOverflow;
        public float BoxSelectedLiftOffset => boxSelectedLiftOffset;
        public WeightBoxRefs[] Boxes => boxes;
    }
}
