namespace Santa.Core
{
    /// <summary>
    /// 音声再生の唯一の入口。共通仕様 00_共通仕様.md §11.3。
    /// 各画面・各ミニゲームは AudioSource を直接持たず、これを経由する
    /// (音量設定を一元管理するため)。
    /// </summary>
    public interface IAudioManager
    {
        /// <summary>BGMを切り替える。同じIDなら何もしない(頭出しし直さない)。</summary>
        void PlayBgm(string bgmId);

        void StopBgm();

        /// <summary>SEを再生する。多重再生時の音割れ対策は実装側の判断(共通仕様 §11.3 / §13-13)。</summary>
        void PlaySe(string seId);

        /// <summary>
        /// ★2026-09-15 追加。<see cref="PlaySe"/> で再生した「長いSE」(`se_microgame_start` 1.42秒 /
        /// `se_finish` 0.75秒 等)を途中で止める。共通仕様 §11.3。再生中でなければ何もしない。
        /// 短いSE(`se_correct` 等)にも呼んでよいが、既に鳴り終わっていれば何もしない。
        /// </summary>
        void StopSe(string seId);

        /// <summary>
        /// ★2026-09-15 追加。現在再生中のBGMを指定秒数でフェードアウトして停止する
        /// (終了演出 §3.9。`bgm_gameplay` を0.15秒で)。フェード中に <see cref="PlayBgm"/> が
        /// 呼ばれた場合はフェードを中断して新しいBGMを即座に再生する。
        /// </summary>
        void FadeOutBgm(float seconds);

        /// <summary>
        /// ループ再生されるSE(例: `se_belt`)を再生する。同じIDが既に再生中なら何もしない
        /// (頭出しし直さない。<see cref="PlayBgm"/> と同じ考え方)。
        /// </summary>
        void PlayLoopSe(string seId);

        /// <summary>指定IDのループSEを止める。再生していなければ何もしない。</summary>
        void StopLoopSe(string seId);

        /// <summary>
        /// 再生中のループSEをすべて止める。画面遷移・セッション終了時の「確実に止める」ための
        /// 安全弁として、個別のStopLoopSe呼び出し漏れがあっても鳴りっぱなしにならないようにする。
        /// </summary>
        void StopAllLoopSe();

        /// <summary>ポーズ・バックグラウンド移行時に呼ぶ。BGM/SE(ループSEを含む)を一時停止する。</summary>
        void PauseAll();

        void ResumeAll();

        void ApplyVolumeSettings(float bgmVolume, float seVolume);
    }
}
