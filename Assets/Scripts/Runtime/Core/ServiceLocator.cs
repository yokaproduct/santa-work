using System;
using System.Collections.Generic;
using UnityEngine;

namespace Santa.Core
{
    /// <summary>
    /// シーン上シングルトンの解決方式(★開発チーム決定。共通仕様 §13-1 / ガイドライン §8-2)。
    ///
    /// 【決定】ServiceLocator パターンを採用する。静的Instance / DI(Zenject等) / SOイベントは不採用。
    /// 理由は本タスクの報告(取りまとめ役への引き継ぎ)を参照。
    ///
    /// 使い方:
    /// - 各サービス(ScreenFlowManager 等)は自身の Awake() で
    ///   <see cref="Register{T}"/> を呼んで自分を登録する。
    ///   登録の確実な先行を保証するため、サービス側のクラスには
    ///   [DefaultExecutionOrder(-100)] を付けること。
    /// - 消費側(ScreenNavButton 等)は <see cref="Get{T}"/> で解決する。
    /// - シーン切り替え・ドメインリロードに備え、<see cref="Clear"/> を
    ///   Bootのエントリポイントで一度呼ぶこと。
    ///
    /// 型はインターフェースで登録・解決することを推奨する(テスト時の差し替え・
    /// Editorデバッグ機能からのモック注入がしやすくなるため)。
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        /// <summary>サービスを登録する。同じ型が既に登録されていれば上書きする。</summary>
        public static void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            Services[typeof(T)] = instance;
        }

        /// <summary>登録を解除する(主に破棄時に呼ぶ。オプション)。</summary>
        public static void Unregister<T>() where T : class
        {
            Services.Remove(typeof(T));
        }

        /// <summary>
        /// サービスを取得する。未登録の場合は例外を投げる
        /// (呼び出し順序の誤りを早期に発見するため。null を握りつぶさない)。
        /// </summary>
        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var value))
            {
                return (T)value;
            }

            throw new InvalidOperationException(
                $"[ServiceLocator] {typeof(T).Name} が未登録です。" +
                "Services 内の対応コンポーネントの Awake() が先に実行されているか確認してください。");
        }

        /// <summary>未登録でも例外を投げず null を返す版。デバッグ用途・任意サービス向け。</summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var value))
            {
                service = (T)value;
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>登録済みサービスを全部消す。ドメインリロード対策として Boot の入口で呼ぶこと。</summary>
        public static void Clear()
        {
            Services.Clear();
        }

#if UNITY_EDITOR
        // Editorのドメインリロード無効化(Enter Play Mode Options)を使っていても、
        // 静的辞書が前回のPlay終了時の値を引き継がないようにする安全策。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Services.Clear();
        }
#endif
    }
}
