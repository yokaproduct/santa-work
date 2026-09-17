using UnityEngine;

namespace Santa.Game
{
    /// <summary>
    /// モード定義。`99_廃止_Screen_ModeSelect.md` §6.1(2026-09-07 承認: MVPから導入)。
    /// MVPでは `GameModeDefinition_Time90.asset` の1つだけを使う。
    /// 12月の時間無制限モードのため、GameSessionController は「90秒固定」で作り込まない
    /// (R1〜R5。同ファイル §6.1 参照)。
    /// </summary>
    [CreateAssetMenu(menuName = "Santa/Game Mode Definition", fileName = "GameModeDefinition_")]
    public class GameModeDefinition : ScriptableObject
    {
        [SerializeField] private string id; // "time90" / 将来"endless"

        [Tooltip("セッション長(秒)。★0 なら無制限(MVPでは分岐点だけ用意し、実装はしない。R2)。")]
        [SerializeField] private float sessionDuration = 90f;

        [SerializeField] private bool recordHighScore = true;
        [SerializeField] private bool evaluateRank = true;
        [SerializeField] private RankTable rankTable;
        [SerializeField] private string titleTextKey;
        [SerializeField] private string descTextKey;

        public string Id => id;
        public float SessionDuration => sessionDuration;
        public bool IsEndless => sessionDuration <= 0f;
        public bool RecordHighScore => recordHighScore;
        public bool EvaluateRank => evaluateRank;
        public RankTable RankTable => rankTable;
        public string TitleTextKey => titleTextKey;
        public string DescTextKey => descTextKey;
    }
}
