using Santa.Core;
using UnityEngine;

namespace Santa.MicroGames
{
    /// <summary>
    /// M2/M3/M5の仮置き実装。ディレクター指示により「見た目は問わないが、
    /// `MicroGameBase` の契約は正しく実装し、3問正解で終わるところまでは動く」ことを満たす。
    ///
    /// 1つの大きなボタンをタップするたびに1件成立する(誤答は発生しない単純な実装)。
    /// M1(本実装)以外の3種は、この共通クラスをそのまま3つのPrefabへ差し込んで使う
    /// (`MicroGame_Address` / `MicroGame_Weight` / `MicroGame_Wrap`)。
    /// </summary>
    public class PlaceholderMicroGame : MicroGameBase
    {
        [SerializeField] private PlaceholderMicroGameRefs refs;

        private int _requiredUnits;
        private int _unitsCleared;
        private bool _began;
        private bool _finished;

        public override void Prepare(MicroGameContext context)
        {
            _requiredUnits = context.RequiredUnits;
            _unitsCleared = 0;
            _began = false;
            _finished = false;

            if (refs.Label != null)
            {
                refs.Label.text = $"{context.Definition.Id}\n(仮実装。タップで1件成立)";
            }

            refs.TapButton.onClick.RemoveAllListeners();
            refs.TapButton.onClick.AddListener(OnTapped);
            refs.TapButton.interactable = false;
        }

        public override void Begin()
        {
            _began = true;
            refs.TapButton.interactable = true;
        }

        public override void SetPaused(bool paused)
        {
            refs.TapButton.interactable = !paused && _began && !_finished;
        }

        public override void Finish(MicroGameFinishReason reason)
        {
            _finished = true;
            refs.TapButton.interactable = false;
        }

        public override MicroGameSummary GetSummary()
        {
            return new MicroGameSummary(_unitsCleared, 0, _unitsCleared);
        }

        private void OnTapped()
        {
            if (ServiceLocator.TryGet<IAudioManager>(out var audio))
            {
                audio.PlaySe(AudioIds.Se.Tap);
            }

            _unitsCleared++;
            RaiseUnitCleared();

            if (_unitsCleared >= _requiredUnits)
            {
                RaiseAllUnitsCleared();
            }
        }
    }
}
