using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Santa.MicroGames.Wrap
{
    /// <summary>M5(ラッピング)の問題データと生成器設定。`23_MicroGame_Wrap.md` §5.1。</summary>
    [CreateAssetMenu(menuName = "Santa/MicroGames/Wrap Generator Settings", fileName = "WrapGeneratorSettings")]
    public class WrapGeneratorSettings : ScriptableObject
    {
        [Header("プロトタイプ用の固定プリセット")]
        [Tooltip("★2026-09-13: 形カテゴリ(R04〜R06)はディレクター決定でいったん出題対象から外した。" +
                 "削除はせず、各プリセットの enabled を false にして抽選(ActivePresets)から除外している。" +
                 "戻すときは enabled を true に戻すだけでよい。")]
        [SerializeField] private List<WrapPreset> presets = new List<WrapPreset>(); // ★6件

        [Header("ベルト")]
        [SerializeField] private float boxSpacing = 180f;   // px(基準解像度)
        [SerializeField] private float beltSpeed = 135f;    // px/秒
        [SerializeField] private float catchUpMultiplier = 3f; // 詰めるときの加速倍率
        [SerializeField] private float itemFeedbackDuration = 0.2f;

        [Header("生成パラメータ(本番用)")]
        [SerializeField] private int boxCount = 9; // ★3問×3ループ。変えない
        [SerializeField] private int trashMin = 1; // ★最低1個
        [SerializeField] private int trashMax = 3; // ★最大3個

        [Header("見た目(未設定なら現状のプレースホルダ表示にフォールバック)")]
        [Tooltip("ゴミ(つぶれた箱)の絵。IMG-M5-06。未設定の間は Background の灰色 + 大きな×のままになる。")]
        [SerializeField] private Sprite trashSprite;

        [Header("箱の完成画像(2026-09-13: 白い土台+色乗算+模様重ねをやめ、色ごとに完成した1枚絵にする方式)")]
        [Tooltip("箱・シュート見本の両方に使う「あか」の完成画像。リボン・影・色覚対策の模様も絵に含めること。" +
                 "未設定の間は従来どおり Background の色乗算 + Pattern の模様重ねにフォールバックする。")]
        [SerializeField] private Sprite redBoxSprite;
        [Tooltip("箱・シュート見本の両方に使う「あお」の完成画像。未設定の間は従来のプレースホルダ表示にフォールバックする。")]
        [SerializeField] private Sprite blueBoxSprite;
        [Tooltip("箱・シュート見本の両方に使う「みどり」の完成画像。未設定の間は従来のプレースホルダ表示にフォールバックする。" +
                 "★2026-09-16: 「あか・あお・きいろ」から「あか・あお・みどり」へ変更(ディレクター決定)。")]
        [FormerlySerializedAs("yellowBoxSprite")]
        [SerializeField] private Sprite greenBoxSprite;

        public List<WrapPreset> Presets => presets;

        /// <summary>
        /// 出題対象のプリセットだけを返す(`enabled == true` のもの)。
        /// ★2026-09-13: 形カテゴリ(R04〜R06)を一時停止するために追加。`Presets` 自体からは削除しない
        /// (プリセットのデータそのものは残し、抽選対象から外すだけ。戻すときは enabled を true に戻す)。
        /// </summary>
        public List<WrapPreset> ActivePresets => presets.FindAll(p => p != null && p.enabled);

        public float BoxSpacing => boxSpacing;
        public float BeltSpeed => beltSpeed;
        public float CatchUpMultiplier => catchUpMultiplier;
        public float ItemFeedbackDuration => itemFeedbackDuration;
        public int BoxCount => boxCount;
        public int TrashMin => trashMin;
        public int TrashMax => trashMax;
        public Sprite TrashSprite => trashSprite;

        /// <summary>
        /// 色カテゴリの完成箱画像を1か所で引けるようにする(依頼: 「1か所で3枚設定すれば全プリセットに反映される」)。
        /// 該当色が未設定、または <paramref name="colorId"/> が <see cref="WrapColorId.None"/> のときは null を返す
        /// (呼び出し側は null のとき従来のプレースホルダ表示にフォールバックすること)。
        /// </summary>
        public Sprite GetCompletedBoxSprite(WrapColorId colorId)
        {
            switch (colorId)
            {
                case WrapColorId.Red: return redBoxSprite;
                case WrapColorId.Blue: return blueBoxSprite;
                case WrapColorId.Green: return greenBoxSprite;
                default: return null;
            }
        }
    }

    public enum WrapCategory
    {
        Color,
        Shape,
    }

    /// <summary>
    /// 色カテゴリの完成箱画像(<see cref="WrapGeneratorSettings"/>)を引くための色の識別子。
    /// ★色の見た目そのものは引き続き <see cref="WrapSample.color"/>(プレースホルダ用)が持つ。
    /// こちらは「どの完成画像を使うか」を明示的に指定するためのもので、Color値の比較に頼らない。
    /// </summary>
    public enum WrapColorId
    {
        None,
        Red,
        Blue,
        Green,
    }

    [Serializable]
    public class WrapPreset
    {
        public string id;                  // "R01"
        public WrapCategory category;

        [Tooltip("★2026-09-13: false にすると出題対象(ActivePresets)から外れる。" +
                 "形カテゴリ(R04〜R06)の一時停止に使用。データは削除しない。")]
        public bool enabled = true;

        public WrapSample[] chuteSamples;  // 3件。左・中・右の順
        public WrapBoxDef[] sequence;      // ★9件。上から流れてくる順
    }

    [Serializable]
    public class WrapBoxDef
    {
        public bool isTrash;      // true ならゴミ
        public int chuteIndex;    // isTrash == false のとき、正解のシュート(0〜2)
    }

    [Serializable]
    public class WrapSample
    {
        public Sprite shapeSprite;   // 形カテゴリで使う
        public Color color;          // 色カテゴリで使う(完成画像が無い間のプレースホルダ表示にも使う)
        public Sprite patternSprite; // ★色カテゴリでの色覚対策(包み紙の模様)。完成画像が無い間のフォールバックで使う
        [Tooltip("色カテゴリのとき、WrapGeneratorSettingsの完成箱画像を引くための色ID。Noneのままだと完成画像方式は使われず、従来のプレースホルダ表示になる。")]
        public WrapColorId colorId;
        [Tooltip("プレースホルダ表示用のラベル(例: あか/みずたま、まる 等)。素材が無い間の識別用。")]
        public string debugLabel;
    }
}
