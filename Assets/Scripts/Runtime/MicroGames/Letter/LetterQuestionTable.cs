using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.MicroGames.Letter
{
    /// <summary>M1(手紙)の問題データ。`20_MicroGame_Letter.md` §5.1。CSVインポート結果。</summary>
    [CreateAssetMenu(menuName = "Santa/MicroGames/Letter Question Table", fileName = "LetterQuestionTable")]
    public class LetterQuestionTable : ScriptableObject
    {
        [SerializeField] private List<LetterQuestion> questions = new List<LetterQuestion>();
        [SerializeField] private List<StockItem> stockItems = new List<StockItem>();

        public List<LetterQuestion> Questions => questions;
        public List<StockItem> StockItems => stockItems;

        /// <summary>指定言語の問題だけを返す(id + lang が複合キー。共通仕様 §8.3)。</summary>
        public List<LetterQuestion> GetQuestionsForLanguage(string lang)
        {
            var result = new List<LetterQuestion>();
            foreach (var q in questions)
            {
                if (q.lang == lang) result.Add(q);
            }
            return result;
        }

        public StockItem FindStockItem(string id)
        {
            foreach (var item in stockItems)
            {
                if (item.id == id) return item;
            }
            return null;
        }
    }

    [Serializable]
    public class LetterQuestion
    {
        public string id;              // "L001"
        public string lang;            // "ja" / "en"  ★id + lang が複合キー
        public string templateType;    // "A" / "B" / "C"
        public string line1;
        public string line2;
        public string line3;
        public string correctItemId;   // "item_car_red"
        public string[] distractorIds; // 3件
        public string uniquenessNote;  // なぜ一意か(ビルドには含めなくてよい)
        public string reviewStatus;    // "draft" / "approved"  ★approved のみビルド
        public string addedVersion;    // "1.0" / "1.1"
    }

    [Serializable]
    public class StockItem
    {
        // ★2026-09-13 ディレクター決定: 2レイヤー着色方式(iconBase+iconLine)を廃止し、
        //   1アイテム1枚の完成画像方式に変更。画像が設定されている間は色を乗算しない。
        public string id;            // "item_car_red"
        public string nameKeyJa;     // 画面には出さない。正誤判定・保守・英語版のための保持
        public string nameKeyEn;
        public Sprite icon;          // 完成画像1枚(未設定の間はプレースホルダにフォールバック)
        public Color baseColor = Color.white; // icon未設定時のプレースホルダ色。iconがある場合は使わない
    }
}
