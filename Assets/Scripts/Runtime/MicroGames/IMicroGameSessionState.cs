namespace Santa.MicroGames
{
    /// <summary>
    /// 種目がセッションを通じて保持したい状態のマーカーインターフェース。
    /// 共通仕様 00_共通仕様.md §4.7。
    ///
    /// 中身は各ミニゲームが自由に定義する。共通側(GameSessionController)は型を知らない。
    /// 例: M2(住所)の地図の累積カウンタ(AddressSessionState)。
    ///
    /// GameSessionController は definition.id をキーにした辞書でインスタンスを保持し、
    /// 初回アクセス時に null ならミニゲーム側が生成して入れる。セッション終了時に破棄する。
    /// </summary>
    public interface IMicroGameSessionState
    {
    }
}
