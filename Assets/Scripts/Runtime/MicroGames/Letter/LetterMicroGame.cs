using System;
using System.Collections.Generic;
using System.Linq;
using Santa.Core;
using UnityEngine;

namespace Santa.MicroGames.Letter
{
    /// <summary>
    /// M1「手紙を読んでしわけろ」。`20_MicroGame_Letter.md` §4 の実装。
    /// 唯一、モックアップに基づいて作り込んだミニゲーム。
    /// </summary>
    public class LetterMicroGame : MicroGameBase
    {
        [SerializeField] private LetterMicroGameRefs refs;

        private LetterQuestionTable _table;
        private LetterSessionState _sessionState;
        private LetterQuestion _currentQuestion;
        private string[] _currentCardItemIds;
        private System.Random _rng;
        private int _requiredUnits;
        private int _unitsCleared;
        private int _unitsMissed;
        private bool _began;
        private bool _finished;

        public override void Prepare(MicroGameContext context)
        {
            _table = context.Definition.QuestionSource as LetterQuestionTable;
            _rng = context.Rng;
            _requiredUnits = context.RequiredUnits;
            _unitsCleared = 0;
            _unitsMissed = 0;
            _began = false;
            _finished = false;

            if (_table == null)
            {
                Debug.LogError("[LetterMicroGame] questionSource が LetterQuestionTable ではありません。");
                return;
            }

            string lang = ServiceLocator.TryGet<ILocalizationService>(out var loc) ? loc.CurrentLanguage : "ja";

            // ★共通仕様 §4.7 の想定漏れ(LetterSessionState.cs のコメント参照)。
            // プールはセッション単位で1回だけシャッフルし、このミニゲームが何度出ても続きから消費する。
            _sessionState = context.GetOrCreateState(() =>
            {
                var questions = _table.GetQuestionsForLanguage(lang);
                return new LetterSessionState { Pool = new ShuffledPoolCursor<LetterQuestion>(questions, context.Rng) };
            });

            LoadNextQuestion();

            for (int i = 0; i < refs.StockCards.Length; i++)
            {
                refs.StockCards[i].Button.interactable = false;
            }

            // ★罫線(IMG-M1-03)。Sprite未設定の間は無効のまま(現状どおり罫線が無い見た目。壊れない)。
            //   Source Imageが直接Prefab側に割り当てられる想定(差し替え方法B寄り)なので、
            //   ここでは「割り当てられていれば表示する」判定だけを行う。
            if (refs.RuleLines != null)
            {
                refs.RuleLines.enabled = refs.RuleLines.sprite != null;
            }
        }

        public override void Begin()
        {
            _began = true;
            SetCardsInteractable(true);
        }

        public override void SetPaused(bool paused)
        {
            SetCardsInteractable(!paused && _began && !_finished);
        }

        public override void Finish(MicroGameFinishReason reason)
        {
            _finished = true;
            SetCardsInteractable(false);
        }

        public override MicroGameSummary GetSummary()
        {
            return new MicroGameSummary(_unitsCleared, _unitsMissed, _unitsCleared + _unitsMissed);
        }

        private void LoadNextQuestion()
        {
            _currentQuestion = _sessionState.Pool.Next();

            var lines = new[] { _currentQuestion.line1, _currentQuestion.line2, _currentQuestion.line3 }
                .Where(l => !string.IsNullOrEmpty(l));
            refs.LetterText.text = string.Join("\n", lines);

            // 在庫4枚 = 正解1 + 誤答3。並び順をシャッフルする(共通仕様 §4.1 必須要件)。
            var itemIds = new List<string> { _currentQuestion.correctItemId };
            itemIds.AddRange(_currentQuestion.distractorIds);
            Shuffle(itemIds, _rng);
            _currentCardItemIds = itemIds.ToArray();

            for (int i = 0; i < refs.StockCards.Length; i++)
            {
                var card = refs.StockCards[i];
                string itemId = i < _currentCardItemIds.Length ? _currentCardItemIds[i] : "";
                var stockItem = _table.FindStockItem(itemId);

                card.DebugLabel.text = itemId;
                // ★1枚画像方式(2026-09-13 ディレクター決定)。
                //   icon が設定されていればそのまま(色を乗算しない)。未設定の間は baseColor によるプレースホルダ表示。
                if (stockItem?.icon != null)
                {
                    card.Icon.sprite = stockItem.icon;
                    card.Icon.color = Color.white;
                }
                else
                {
                    card.Icon.sprite = null;
                    card.Icon.color = stockItem?.baseColor ?? Color.white;
                }

                int capturedIndex = i;
                card.Button.onClick.RemoveAllListeners();
                card.Button.onClick.AddListener(() => OnCardTapped(capturedIndex));
            }
        }

        private void OnCardTapped(int index)
        {
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Tap);
            }

            SetCardsInteractable(false); // 連打による二重回答の防止(共通仕様 §4.1-M1-5)

            bool correct = _currentCardItemIds[index] == _currentQuestion.correctItemId;
            if (correct)
            {
                _unitsCleared++;
                RaiseUnitCleared();
            }
            else
            {
                _unitsMissed++;
                RaiseMissed();
            }

            if (_unitsCleared >= _requiredUnits)
            {
                RaiseAllUnitsCleared();
                return; // 共通側が Finish() を呼ぶまで、これ以上カードを操作させない
            }

            LoadNextQuestion();
            SetCardsInteractable(true);
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void SetCardsInteractable(bool interactable)
        {
            foreach (var card in refs.StockCards)
            {
                card.Button.interactable = interactable;
            }
        }
    }
}
