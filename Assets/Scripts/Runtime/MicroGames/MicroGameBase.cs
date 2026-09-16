using System;
using UnityEngine;

namespace Santa.MicroGames
{
    /// <summary>
    /// 全ミニゲームの共通基底。MicroGame_*.prefab のルートに、これを継承したコンポーネントを1つだけ付ける。
    /// 共通仕様 00_共通仕様.md §4.3(★最重要契約)。
    ///
    /// ★ここに書かれた5つのメソッド以外を GameSessionController から呼んではならない。
    /// ★MicroGameSlot の RectTransform を書き換えない。自Prefabのルートは stretch で親に合わせる。
    ///
    /// 呼び出し順序の契約(共通仕様 §4.3 より):
    /// <code>
    /// Instantiate(prefab, MicroGameSlot)
    ///   → Prepare(context)
    ///   → (初出なら Overlay_MicroGameIntro。その間 SetPaused(true) → 閉じたら SetPaused(false))
    ///   → (業務提示 0.8秒。この間 Begin() は呼ばない)
    ///   → Begin()
    ///   → …プレイ中。OnUnitCleared / OnMissed / OnAllUnitsCleared を発火…
    ///   → Finish(reason)
    ///   → GetSummary()
    ///   → (判定演出 0.35秒)
    ///   → Destroy
    /// </code>
    ///
    /// ミニゲーム側が守ること(継承先の実装者は必読):
    /// <list type="number">
    /// <item><description><see cref="OnUnitCleared"/> / <see cref="OnMissed"/> は
    /// <see cref="Begin"/> から <see cref="Finish"/> までの間にだけ発火する。</description></item>
    /// <item><description><see cref="Prepare"/> で問題を用意し切る。<see cref="Begin"/> 以降に
    /// 問題プールへアクセスしない(フレーム落ちの原因になる)。</description></item>
    /// <item><description>T2(12秒)を自分で管理しない。残り時間の表示が必要なら
    /// 共通の Part_MicroGameTimerBar を使う。</description></item>
    /// <item><description>自分から画面遷移・スコア加算・セーブを行わない。
    /// できるのはイベントを発火することだけ。</description></item>
    /// <item><description>MicroGameSlot の RectTransform を書き換えない。</description></item>
    /// </list>
    /// </summary>
    public abstract class MicroGameBase : MonoBehaviour
    {
        /// <summary>件が成立した(= スコア加算 + コンボ+1)。</summary>
        public event Action OnUnitCleared;

        /// <summary>誤答した(= コンボリセット)。★プレイヤーが明確に誤った選択をしたときだけ発火する。
        /// 時間切れ・中断・M3のオーバーでは絶対に発火しない(共通仕様 §3.4)。</summary>
        public event Action OnMissed;

        /// <summary>必要件数に達した(= 3件クリア)。共通側はこれを受けて操作フェーズを即終了する。
        /// <see cref="OnUnitCleared"/> の直後(同一フレーム)に発火してよい。共通側は
        /// OnUnitCleared の処理を先に済ませてから OnAllUnitsCleared を処理する。</summary>
        public event Action OnAllUnitsCleared;

        /// <summary>
        /// [1] 準備。Instantiate 直後、業務提示の前に1回だけ呼ばれる。
        /// 問題の生成・選択と初期表示をここで完了させる。★入力は受け付けない状態にする。
        /// ★問題内容(手紙・荷物・箱)はこの時点で画面に見えていること(業務提示中に読ませるため)。
        /// 重い処理は避ける。1フレームで完了させること。
        /// </summary>
        public abstract void Prepare(MicroGameContext context);

        /// <summary>[2] 開始。操作フェーズの開始と同時に呼ばれる。入力受付を開始する。</summary>
        public abstract void Begin();

        /// <summary>
        /// [3] 一時停止 / 再開。ポーズ・初出カード表示中に呼ばれる。
        /// 内部のアニメーション・ベルトの流れ・自前のタイマーを止める。
        /// </summary>
        public abstract void SetPaused(bool paused);

        /// <summary>
        /// [4] 終了。3件クリア / 時間切れ / セッション終了のいずれでも必ず呼ばれる。
        /// 入力受付を止め、判定演出(0.35秒)に入れる状態にする。
        /// ★ここで OnMissed / OnUnitCleared を発火してはならない。
        /// </summary>
        public abstract void Finish(MicroGameFinishReason reason);

        /// <summary>
        /// [5] このミニゲームの結果サマリ。Finish の後に共通側が1回だけ読む。
        /// 種目別成績・結果画面の集計に使う。
        /// </summary>
        public abstract MicroGameSummary GetSummary();

        /// <summary>
        /// 件が成立したことを共通側へ通知する。継承先はイベントを直接 invoke できないため、
        /// このヘルパーを経由する。
        /// </summary>
        protected void RaiseUnitCleared() => OnUnitCleared?.Invoke();

        /// <summary>誤答したことを共通側へ通知する。</summary>
        protected void RaiseMissed() => OnMissed?.Invoke();

        /// <summary>必要件数に達したことを共通側へ通知する。</summary>
        protected void RaiseAllUnitsCleared() => OnAllUnitsCleared?.Invoke();
    }
}
