using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.MicroGames.Weight
{
    /// <summary>M3(重さ)の問題データと生成器設定。`22_MicroGame_Weight.md` §5.1。</summary>
    [CreateAssetMenu(menuName = "Santa/MicroGames/Weight Generator Settings", fileName = "WeightGeneratorSettings")]
    public class WeightGeneratorSettings : ScriptableObject
    {
        [Header("プロトタイプ用の固定プリセット")]
        [SerializeField] private List<WeightPreset> presets = new List<WeightPreset>(); // ★6件

        [Header("本番用の生成パラメータ")]
        [SerializeField] private int boxCount = 6; // ★常に6。変えない(設計原則2)
        [SerializeField] private int valueMin = 1;
        [SerializeField] private int valueMax = 35;
        [SerializeField] private int answerBoxCountMin = 2;
        [SerializeField] private int answerBoxCountMax = 3;
        [SerializeField] private int maxRetry = 50; // ★リトライ上限(無限ループ防止)
        [SerializeField] private WeightPreset fallbackPreset; // ★リトライ上限を超えたときの保険

        [Header("テストプレイ用の暫定難易度(2026-09-16 ディレクター決定)")]
        [Tooltip("生成される問題の目標値(target)の上限。プリセットもこれを超えないように作る。" +
                 "本番の難易度が決まったら緩める/撤廃する想定のため、コード定数ではなくInspectorで調整できる値にしてある。")]
        [SerializeField] private int maxTargetValue = 20;

        [Header("演出")]
        [Tooltip("そりのスライド演出。2026-09-07 決定: 0.25秒。")]
        [SerializeField] private float sledSlideDuration = 0.25f;

        [Header("そり+トナカイ統合画像(2026-09-16: SledImage+ReindeerImageを1要素(SledReindeerImage)に統合)")]
        [Tooltip("選択数0のときの絵。初期状態、および新しい問題(そり)が出たときも常にこれを使う。")]
        [SerializeField] private Sprite sledReindeerIdleSprite;
        [Tooltip("選択数1〜3個の通常時の絵。要素0が1個選択、要素1が2個選択、要素2が3個選択に対応。")]
        [SerializeField] private Sprite[] sledNormalSprites = new Sprite[3];
        [Tooltip("そり成立(正解)時点の絵。要素0〜2の対応は通常時と同じ。")]
        [SerializeField] private Sprite[] sledHappySprites = new Sprite[3];
        [Tooltip("重量オーバー時点の絵(従来の「悲しい顔」に相当)。要素0〜2の対応は通常時と同じ。")]
        [SerializeField] private Sprite[] sledAngrySprites = new Sprite[3];

        public List<WeightPreset> Presets => presets;
        public int BoxCount => boxCount;
        public int ValueMin => valueMin;
        public int ValueMax => valueMax;
        public int AnswerBoxCountMin => answerBoxCountMin;
        public int AnswerBoxCountMax => answerBoxCountMax;
        public int MaxRetry => maxRetry;
        public WeightPreset FallbackPreset => fallbackPreset;
        public int MaxTargetValue => maxTargetValue;
        public float SledSlideDuration => sledSlideDuration;
        public Sprite SledReindeerIdleSprite => sledReindeerIdleSprite;

        /// <summary>選択数(1〜3)に応じた通常時の絵。範囲外・未設定なら null(呼び出し側はフォールバックすること)。</summary>
        public Sprite GetSledNormalSprite(int selectedCount) => GetBySelectedCount(sledNormalSprites, selectedCount);

        /// <summary>選択数(1〜3)に応じた成立(正解)時の絵。範囲外・未設定なら null。</summary>
        public Sprite GetSledHappySprite(int selectedCount) => GetBySelectedCount(sledHappySprites, selectedCount);

        /// <summary>選択数(1〜3)に応じた重量オーバー時の絵。範囲外・未設定なら null。</summary>
        public Sprite GetSledAngrySprite(int selectedCount) => GetBySelectedCount(sledAngrySprites, selectedCount);

        private static Sprite GetBySelectedCount(Sprite[] sprites, int selectedCount)
        {
            int index = selectedCount - 1; // 1個選択 → 要素0
            if (sprites == null || index < 0 || index >= sprites.Length) return null;
            return sprites[index];
        }
    }

    [Serializable]
    public class WeightPreset
    {
        public string id;          // "W01"
        public int[] boxValues;    // 6件
        public int target;
        public int[] answerIndex;  // 検証用。実装では使わないがデータの正しさを人間が確認できる
    }
}
