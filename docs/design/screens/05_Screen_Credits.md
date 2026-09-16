# 05 `Screen_Credits` —— クレジット

- 作成: 2026-09-07 / プランナー(サブエージェント)
- **前提: [00_共通仕様.md](00_共通仕様.md) を先に読むこと。**
- Prefab: `Assets/Prefabs/UI/Screens/Screen_Credits.prefab`
- スクリプト: `CreditsScreenRefs` / `CreditsScreenController`

> **レイアウト値の所有権**: 本書の px・座標・サイズは**初回生成時の初期値**であり、**以後はディレクターの所有物**。

---

## 1. 画面の目的

**使用素材のライセンス表記を掲示する。ストア申請の必須要件。**

- フォント・音源・画像・アセット・ライブラリ(広告SDK等)の**帰属表示(attribution)**が必要なものを列挙する。
- **表示義務があるものを1つでも漏らすと、ライセンス違反になる。**
  企画書 §8.4 は「使用素材のライセンスは `docs/licenses.md` に**随時記録**し、クレジット画面に反映する。
  **後からまとめて調べると必ず漏れる**」としている。

### 1.1 遷移元 / 遷移先

| | 画面 | 条件 |
|---|---|---|
| 遷移元 | `Screen_Settings` | 「クレジット」の行 |
| 遷移元 | `Screen_ModeSelect` | 「クレジット」ボタン(**`03_Screen_ModeSelect.md` §8-3 で置くか決める**) |
| **遷移先** | **来た画面へ戻る** | 「もどる」。**遷移元を保持して戻る**(`Screen_Settings` または `Screen_ModeSelect`) |

**この画面から他へは行かない**(外部リンクを除く)。

---

## 2. Prefab構造(初回生成時)

```
Screen_Credits                            stretch 全面
├─ [CreditsScreenRefs]
├─ [CreditsScreenController]
└─ SafeAreaRoot                           stretch
    ├─ Background                         Image / stretch
    ├─ HeaderRoot                         anchor(0.5,1) / 1080×160
    │   ├─ HeaderText                     「クレジット」
    │   └─ BackButton                     anchor(0,1) / 200×160
    ├─ ContentRoot                        上下ストレッチ
    │   └─ ScrollView                     stretch(ScrollRect / 縦のみ)
    │       └─ Viewport
    │           └─ Content                ★VerticalLayoutGroup + ContentSizeFitter
    │               └─ (Row_CreditSection / Row_CreditEntry を実行時に Instantiate)
    └─ BottomReserve                      anchor(0.5,0) / 高さは実行時計算
```

### 2.1 行Prefab

| Prefab | 用途 | 中身 |
|---|---|---|
| `Row_CreditSection.prefab` | 見出し(「フォント」「おんがく」など) | `SectionTitleText` |
| `Row_CreditEntry.prefab` | 1件のクレジット | `NameText` / `AuthorText` / `LicenseText` / `UrlButton`(URLがある場合のみ表示) |

**コードで `new GameObject` して行を組み立てない**(ガイドライン §5.6)。

### 2.2 レイアウト初期値(**以後ディレクターの所有物**)

| 要素 | anchor / pivot | anchoredPosition | sizeDelta |
|---|---|---|---|
| `HeaderRoot` | (0.5,1)/(0.5,1) | (0, 0) | (1080, 160) |
| `BackButton` | (0,1)/(0,1) | (20, 0) | (200, 160) |
| `ContentRoot` | stretch | — | offsetMax(0, -160) / offsetMin(0, **実行時**) |
| `Row_CreditSection` | — | — | (1000, 120) |
| `Row_CreditEntry` | — | — | (1000, **可変**。`ContentSizeFitter` で本文に合わせる) |
| `BottomReserve` | (0.5,0)/(0.5,0) | (0, 0) | (1080, 296) |

- `VerticalLayoutGroup.spacing = 16`、`padding` 上下40・左右40・**下 80**(バナーと接近しないため)
- `Row_CreditEntry` は**高さ可変**にする。ライセンス全文(MITなど)を載せる可能性があるため。

---

## 3. 要素一覧

| # | 要素 | 役割 | 表示条件 | 状態 |
|---|---|---|---|---|
| C1 | `BackButton` | `Screen_Settings` へ戻る | 常時 | 常に活性 |
| C2 | `Row_CreditSection` | カテゴリの見出し | カテゴリごとに1つ | — |
| C3 | `NameText` | 素材名 | 常時 | — |
| C4 | `AuthorText` | 作者・提供元 | 常時 | — |
| C5 | `LicenseText` | ライセンス名(または全文) | 常時 | — |
| C6 | `UrlButton` | 出典URL。タップで外部ブラウザ | **URLがある項目のみ** | — |

### 3.1 掲載するカテゴリ(仮)

| カテゴリ | 想定される中身 |
|---|---|
| フォント | **商用利用・アプリ組み込みの許諾を確認したうえで、必要なら表記**(企画書 §8.4) |
| おんがく | ロイヤリティフリー音源(BGM 3曲)。**既存クリスマス曲の録音物は使わない** |
| こうかおん | SE 17点 |
| イラスト | 有料アセット / 生成AI / 自作の別を明記 |
| **こっき・ちず** | **国旗はパブリックドメイン / CC0 の正確なデータ。**出典を明記 |
| ソフトウェア | **広告SDK / UMP** などのサードパーティライブラリ |

> **国旗と地図はディレクターが用意する方針**(企画書 §8.4)だが、
> **元データがCC0/PD由来である以上、出典の記録は必要。** ここに載せる。

> **⚠ 生成AIで作った素材の扱いが未定義(§6-2)。**
> 企画書 §8.4 は「有料アセット / 生成AI / 自作の**併用**」としているが、
> **生成AIで作った素材をクレジットに書くか、書くならどう書くか**が決まっていない。

---

## 4. 処理内容

### 4.1 データの持ち方(★ここが本画面の設計の要)

> **問題**: 企画書は「`docs/licenses.md` に随時記録し、クレジット画面に反映する」としているが、
> **`licenses.md`(Markdown)からアプリ内表示への同期手段が定義されていない。**
> **手でコピーする運用にすると、必ずずれる。**「後からまとめて調べると必ず漏れる」と
> 企画書自身が警告している問題が、そのまま形を変えて残っている。

**【2026-09-07 承認済み】以下の方式で確定。**

```
docs/data/credits.csv          ← ★ここを唯一の正とする
        ↓ Tools/データ/クレジットをインポート
Assets/Settings/CreditTable.asset   (ScriptableObject)
        ↓ 実行時
Screen_Credits
        ↓ 別の Editor ツール(任意)
docs/licenses.md               ← ★CSVから生成する(手で書かない)
```

- **M1・M2の問題データと同じ「CSV → Editorインポータ → ScriptableObject」の仕組みを流用する。**
  新しい仕組みを作らずに済む(共通仕様 §8.2)。
- **`docs/licenses.md` を CSV から生成する**ようにすれば、**二重管理が構造的に消える。**

**CSV列**

```
category, name, author, license, url, note, added_version
```

**ScriptableObject**

```csharp
[CreateAssetMenu(menuName = "Santa/Credit Table")]
public class CreditTable : ScriptableObject
{
    [SerializeField] private List<CreditEntry> entries;
}

[Serializable]
public class CreditEntry
{
    public string category;   // "font" / "music" / "se" / "art" / "flag" / "software"
    public string name;
    public string author;
    public string license;    // "CC0" / "MIT" / "商用ライセンス購入済み" 等
    public string url;        // 空可
    public string note;       // 空可
}
```

> **クレジットの文言は `LocalizationTable` に入れない。**
> **素材名・作者名・ライセンス名は翻訳してはいけない**(固有名詞・法的文言のため)。
> **カテゴリの見出しだけを `LocalizedText` にする。**

### 4.2 画面に入ったとき

```
1. CreditTable.asset を読む
2. category ごとにグルーピングし、決められた順序で並べる
3. カテゴリごとに Row_CreditSection を Instantiate
4. その配下に Row_CreditEntry を Instantiate
5. url が空の項目は UrlButton を SetActive(false)
6. ScrollRect を先頭にリセット
7. AdDisplayPolicy に従いバナーを表示する(★この画面は表示する)
```

### 4.3 各要素を操作したとき

| 要素 | 操作 | 処理 |
|---|---|---|
| `BackButton` | タップ | ① `se_button` ② `Screen_Settings` へ |
| `UrlButton` | タップ | ① `se_button` ② `Application.OpenURL(url)` |

---

## 5. 音声

- BGM: `bgm_title` のまま
- SE: `se_button` のみ

---

## 6. 未確定事項

| # | 内容 | 影響 | 判断者 |
|---|---|---|---|
| ~~1~~ | ~~`docs/licenses.md` とアプリ内表示の同期方法~~ | **承認済み(2026-09-07): CSVを唯一の正とし、`licenses.md` はCSVから生成する** | — |
| **2** | **生成AIで作った素材をクレジットに書くか、書くならどう書くか** | 企画書 §8.4 は生成AIの併用を認めているが、表記の方針が未定義 | ディレクター |
| **3** | **フォントのライセンス**(商用利用・アプリ組み込みの可否)。**決まらないとこの画面が完成しない** | 共通仕様 §14-17 と同じ論点 | ディレクター |
| **4** | ライセンス全文を載せる必要があるもの(MIT等)があるか。ある場合は `Row_CreditEntry` の高さが大きくなる | レイアウト | ディレクター |
| 5 | カテゴリの表示順 | 見た目 | ディレクター |
| 6 | 開発者名 / スタジオ名の表記 | ストア掲載名と揃える必要がある | ディレクター |

---

## 7. 開発チームの技術判断が必要な項目

| # | 内容 |
|---|---|
| **CR-1** | **CSV → `CreditTable.asset` のインポータ**(M1・M2の問題データと同じ仕組みを流用できるか) |
| **CR-2** | **`docs/licenses.md` を CSV から生成する Editor ツール**を作るか(任意だが推奨) |
| **CR-3** | `Row_CreditEntry` の高さを本文に合わせて可変にする(`ContentSizeFitter` + `VerticalLayoutGroup` の入れ子)。**レイアウトの再計算コストと、行数が多いときのスクロール性能** |
| **CR-4** | 広告SDK が要求する帰属表示(Google Mobile Ads の場合、依存ライブラリの一覧が必要になることがある)を**自動で取得できるか、手で書くか** |
