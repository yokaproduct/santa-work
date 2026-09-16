using UnityEngine;

namespace Santa.MicroGames
{
    /// <summary>
    /// 種目の登録データ。共通仕様 00_共通仕様.md §4.2。
    ///
    /// 種目の同一性は enum ではなく ScriptableObject アセットで表す。
    /// enum を使うと種目追加のたびに共有コードを編集することになり、
    /// 「1種追加 = 1Prefab + 1スクリプト + データ追加」という必須要件から外れるため。
    ///
    /// ★<see cref="Id"/> はセーブデータのキーに埋め込まれるため、一度決めたら変更しない。
    /// MVPの4種は letter / address / weight / wrap。
    /// </summary>
    [CreateAssetMenu(menuName = "Santa/MicroGame Definition", fileName = "MicroGameDefinition_")]
    public class MicroGameDefinition : ScriptableObject
    {
        [Header("識別子(★セーブキーに使われる。変更禁止)")]
        [SerializeField] private string id;

        [Header("Prefab")]
        [Tooltip("MicroGame_*.prefab のルートに付いた MicroGameBase 継承コンポーネント。")]
        [SerializeField] private MicroGameBase prefab;

        [Header("文言キー")]
        [Tooltip("業務提示の動詞(例: \"micro.letter.prompt\")。")]
        [SerializeField] private string promptTextKey;

        [Tooltip("HUDのミニゲーム名表示で使う名称(横300px。長めでもよい)。")]
        [SerializeField] private string titleTextKey;

        [Tooltip("結果画面の種目別内訳で使う短い名称(4文字以内。共通仕様 §15.5-71 / §6.1.1)。")]
        [SerializeField] private string shortTitleTextKey;

        [Tooltip("初出カードの本文。")]
        [SerializeField] private string introCardTextKey;

        [SerializeField] private Sprite introCardImage;

        [Header("問題データ")]
        [Tooltip("問題データ or 生成器設定。種目ごとに型が違うため ScriptableObject 型で受け、" +
                 "各ミニゲームがキャストする(共通仕様 §4.2 / §13-9)。")]
        [SerializeField] private ScriptableObject questionSource;

        [Header("バランス")]
        [Tooltip("1件を構成する問数。M1/M2/M3 = 1、M5 = 3。")]
        [SerializeField] private int questionsPerUnit = 1;

        [Tooltip("false の間は出題対象から除外される。12月アップデート前の種目を false で仕込んでおける。")]
        [SerializeField] private bool enabled = true;

        public string Id => id;
        public MicroGameBase Prefab => prefab;
        public string PromptTextKey => promptTextKey;
        public string TitleTextKey => titleTextKey;
        public string ShortTitleTextKey => shortTitleTextKey;
        public string IntroCardTextKey => introCardTextKey;
        public Sprite IntroCardImage => introCardImage;
        public ScriptableObject QuestionSource => questionSource;
        public int QuestionsPerUnit => questionsPerUnit;
        public bool Enabled => enabled;
    }
}
