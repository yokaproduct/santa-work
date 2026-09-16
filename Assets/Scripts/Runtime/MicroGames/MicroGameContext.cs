using System;
using System.Collections.Generic;

namespace Santa.MicroGames
{
    /// <summary>
    /// 共通側(GameSessionController)からミニゲームへ渡す情報。共通仕様 00_共通仕様.md §4.3 / §4.7。
    /// ★ここに無いものをミニゲームが参照してはならない。
    /// </summary>
    public readonly struct MicroGameContext
    {
        /// <summary>この種目の登録データ。questionSource / questionsPerUnit を含む。</summary>
        public MicroGameDefinition Definition { get; }

        /// <summary>クリアに必要な件数。= 3(GameBalanceSettings から)。</summary>
        public int RequiredUnits { get; }

        /// <summary>ミニゲームタイマー T2 の長さ(表示用。= 12.0秒)。時間管理は共通側が行う。</summary>
        public float PlayDuration { get; }

        /// <summary>セッション共通の乱数。シード固定でリプレイ可能にする(共通仕様 §3.6)。</summary>
        public Random Rng { get; }

        /// <summary>このセッションで自分が何本目か(0始まり)。</summary>
        public int SessionPlayIndex { get; }

        // ★共通仕様 §4.7 の記述には「ミニゲーム側が生成して入れる」とあるが、
        //   MicroGameContext は readonly struct でありゲッターしか持たないため、
        //   「入れる」ための書き込み手段が仕様上どこにも定義されていなかった(実装上の穴。
        //   取りまとめ役への報告に記載する)。
        //   ここでは GameSessionController が保持する辞書への参照を非公開で持たせ、
        //   GetOrCreateState<T>() を唯一の読み書き経路とすることで解決する。
        //   共通側(GameSessionController)は具象型を一切知らないままでいられる。
        private readonly IDictionary<string, IMicroGameSessionState> _sessionStateStore;

        public MicroGameContext(
            MicroGameDefinition definition,
            int requiredUnits,
            float playDuration,
            Random rng,
            int sessionPlayIndex,
            IDictionary<string, IMicroGameSessionState> sessionStateStore)
        {
            Definition = definition;
            RequiredUnits = requiredUnits;
            PlayDuration = playDuration;
            Rng = rng;
            SessionPlayIndex = sessionPlayIndex;
            _sessionStateStore = sessionStateStore;
        }

        /// <summary>
        /// この種目のセッション永続状態を取得する。無ければ <paramref name="factory"/> で生成して保持し、
        /// 次にこの種目が出たときも同じインスタンスを返す。セッション終了時に破棄される(共通仕様 §4.7)。
        /// </summary>
        public T GetOrCreateState<T>(Func<T> factory) where T : class, IMicroGameSessionState
        {
            if (_sessionStateStore == null)
            {
                // Editorデバッグ用の簡易呼び出し等、辞書が渡されていない文脈では毎回新規生成にフォールバックする。
                return factory();
            }

            if (_sessionStateStore.TryGetValue(Definition.Id, out var existing) && existing is T typed)
            {
                return typed;
            }

            var created = factory();
            _sessionStateStore[Definition.Id] = created;
            return created;
        }
    }
}
