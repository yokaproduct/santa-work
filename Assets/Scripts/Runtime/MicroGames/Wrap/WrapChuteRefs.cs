using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Santa.MicroGames.Wrap
{
    /// <summary>
    /// シュート1口ぶんの参照集約。`23_MicroGame_Wrap.md` §3 の `Chute0`〜`Chute2`。
    /// </summary>
    public class WrapChuteRefs : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image sampleImage; // ChuteSampleImage。色カテゴリ: Image.color を見本色にする(§4.1 P5)
        [SerializeField] private TMP_Text sampleLabel; // ★形/色のプレースホルダ表示。素材が無い間の識別用(仕様書§5.3の注記)
        [SerializeField] private Image samplePattern; // ★2026-09-13追加。色カテゴリの色覚対策模様。素材が無い間は無効のまま

        public Button Button => button;
        public Image SampleImage => sampleImage;
        public TMP_Text SampleLabel => sampleLabel;
        public Image SamplePattern => samplePattern;
    }
}
