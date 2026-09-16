using UnityEngine;

namespace Santa.MicroGames.Wrap
{
    /// <summary>
    /// ベルト上にある箱1個の実行時状態。MonoBehaviour ではない(<see cref="WrapMicroGame"/> の内部でのみ使う)。
    /// </summary>
    internal class WrapBoxRuntime
    {
        public WrapBoxDef Definition;
        public GameObject GameObject;
        public RectTransform RectTransform;
        public WrapBoxItemRefs Refs;

        /// <summary>ベルト上のY座標(BoxContainer のローカル座標。判定ゾーンの下端 = 停止位置で 0 に近い値)。</summary>
        public float CurrentY;

        /// <summary>true の間、通常速度ではなく加速(詰める)速度で移動する(`23_MicroGame_Wrap.md` §4.2)。</summary>
        public bool IsCatchingUp;
    }
}
