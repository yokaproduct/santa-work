using UnityEngine;
using UnityEngine.UI;

namespace Santa.UI
{
    /// <summary>
    /// `Part_UnitProgressDots.prefab`。「3件正解でクリア」を伝える唯一の手段
    /// (共通仕様 §6.5)。個数は `GameBalanceSettings.RequiredUnits` から取り、ハードコードしない。
    /// ドットの数がRequiredUnitsと異なる場合は、あるだけのドットを埋める(不足分は無視)。
    /// </summary>
    public class UnitProgressDotsRefs : MonoBehaviour
    {
        [SerializeField] private Image[] dots;

        [SerializeField] private Color filledColor = Color.white;
        [SerializeField] private Color emptyColor = new Color(1, 1, 1, 0.25f);

        public void ResetDots()
        {
            foreach (var dot in dots)
            {
                if (dot != null) dot.color = emptyColor;
            }
        }

        public void SetCleared(int clearedCount)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null) continue;
                dots[i].color = i < clearedCount ? filledColor : emptyColor;
            }
        }
    }
}
