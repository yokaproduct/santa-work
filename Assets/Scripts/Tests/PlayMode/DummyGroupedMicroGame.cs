using System.Collections.Generic;
using Santa.MicroGames;
using UnityEngine;

namespace Santa.Tests
{
    /// <summary>
    /// M5(ラッピング)の「固定グルーピング」(共通仕様 §4.5)を模したテスト用ミニゲーム。
    /// 3回の生タップを1グループとして扱い、3回とも正解のときだけ OnUnitCleared を1回発火する。
    /// 誤答はグループ内で即座に OnMissed を発火する(コンボはすぐ切れるが、そのグループの
    /// OnUnitCleared は発火しない)。
    ///
    /// これは実際のM5実装ではなく、「GameSessionControllerがこのグルーピングを一切知らなくても
    /// 正しく点数計算できる」ことを検証するための最小限の代替品。
    /// </summary>
    public class DummyGroupedMicroGame : MicroGameBase
    {
        // ★[SerializeField] が無いと Instantiate() 時にクローンへ引き継がれない(DummyMicroGame参照)。
        [SerializeField] private List<bool> _rawTaps = new List<bool>();
        private int _cursor;
        private int _groupIndex;
        private bool _groupHasMiss;
        private int _unitsCleared;
        private int _unitsMissed;

        public void ConfigureRawTaps(IEnumerable<bool> correctFlags)
        {
            _rawTaps = new List<bool>(correctFlags);
        }

        public override void Prepare(MicroGameContext context)
        {
            _cursor = 0;
            _groupIndex = 0;
            _groupHasMiss = false;
            _unitsCleared = 0;
            _unitsMissed = 0;
        }

        public override void Begin()
        {
            // タップ操作を模して全ぶんを即座に処理する(タイミングの検証はDummyMicroGame側で行うため、
            // ここではグルーピングのロジック自体の正しさに焦点を当てる)。
            while (_cursor < _rawTaps.Count)
            {
                ProcessNextTap();
            }
        }

        private void ProcessNextTap()
        {
            bool correct = _rawTaps[_cursor];
            _cursor++;

            if (!correct)
            {
                _groupHasMiss = true;
                _unitsMissed++;
                RaiseMissed(); // ★誤答はその場で即座に発火(共通仕様 §4.5)
            }

            bool isLastOfGroup = _cursor % 3 == 0;
            if (isLastOfGroup)
            {
                if (!_groupHasMiss)
                {
                    _unitsCleared++;
                    RaiseUnitCleared(); // ★3問とも正解だったグループだけ1件として成立させる
                }
                _groupHasMiss = false;
                _groupIndex++;

                if (_groupIndex >= 3)
                {
                    RaiseAllUnitsCleared();
                }
            }
        }

        public override void SetPaused(bool paused)
        {
        }

        public override void Finish(MicroGameFinishReason reason)
        {
        }

        public override MicroGameSummary GetSummary()
        {
            return new MicroGameSummary(_unitsCleared, _unitsMissed, _rawTaps.Count);
        }
    }
}
