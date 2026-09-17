# 02 `Screen_Title` —— タイトル(ホーム画面。★2026-09-17 モード選択を統合)

- 作成: 2026-09-07 / 改稿: 2026-09-07(第2版)/ プランナー(サブエージェント)
- **前提: [00_共通仕様.md](00_共通仕様.md) を先に読むこと。**
- Prefab: `Assets/Prefabs/UI/Screens/Screen_Title.prefab`(**★2026-09-17: この Prefab を土台に `Screen_ModeSelect` の要素を移す**)
- スクリプト: `TitleScreenRefs` / `TitleScreenController`(★2026-09-17 時点の実装は仮置きの `PlaceholderScreenRefs` / `PlaceholderScreenController`。§0.4 で差し替える)
- 改訂: 2026-09-16 / プランナー —— 右上に設定ボタンを仮置き(決定ログ §4-8)。
- 改訂: 2026-09-16(2) / プランナー —— developer の実装結果を反映(`SettingsButton` は `SafeAreaRoot` 直下の最後の子、右上、200×160、紺地に「せってい」)。
- **改訂: 2026-09-17(第3版)/ プランナー** —— **ディレクター決定: タイトル画面(`Screen_Title`)とゲーム選択画面(`Screen_ModeSelect`)を1つの画面に統合する。**
  **全面改稿。**旧 `99_廃止_Screen_ModeSelect.md` の要素(90秒モードの開始ボタン・記録表示・時間無制限モードの告知・初回ヒント)をこの画面に移し、
  **全面タップ(`TapAnywhereButton`)は廃止**、**`ScreenId.ModeSelect` も廃止**する。経緯は決定ログ §4-10。
  旧 `99_廃止_Screen_ModeSelect.md` は廃止記録として残す(冒頭に移管先の表)。
- **改訂: 2026-09-17(第3版・確定)/ プランナー** —— **ディレクターが §0.1 の構成を推奨どおり確定した**(画面名維持 / Title を土台 / `PlayButton_Time90` だけ移植 / 5要素を新規作成 / 背景は IMG-BG-01 / `ScreenId.ModeSelect` 廃止・数値固定 / 旧開始ボタンの位置・サイズは仮置き値 / **全面タップ廃止**)。
  §0.1 / §0.4 / §0.5 / §3 H1 / §7 を「決定」に更新。開始時のカウントダウンは**表示を実装する(長さは現状のまま)**に決まった(§7-13、`30_Overlay_Countdown.md`)。**ステータス: 確定。developer は着手してよい。**

> **レイアウト値の所有権**: 本書の px・座標・サイズ・フォントサイズは**初回生成時の初期値**であり、**以後はディレクターの所有物**。
> **既存要素(`Background` / `SafeAreaRoot` / `ContentRoot` / `BottomReserve` / `SettingsButton` とその `Label`)の値は、この改訂でも一切書き換えない。**

---

## 0. ★2026-09-17 統合の要点(developer はまずここを読む)

> **【2026-09-17 ディレクター決定】`Screen_Title` と `Screen_ModeSelect` を1つの画面に統合する。**
> **統合後の画面から、設定を開くこともゲームを選ぶこともできるようにする。**
> 理由(ディレクターの言葉):「BGMが一致する点と、それぞれの画面で特有にしたい操作がないため、同じ画面で設定やゲーム選択をできる方針にしてよいと判断した」

### 0.1 構成(一覧)—— **★2026-09-17 ディレクター確定**

| 項目 | 内容 |
|---|---|
| **画面名** | **`Screen_Title` のまま**(改名しない。§0.2)**【確定】** |
| **土台にする Prefab** | **`Screen_Title.prefab`**(既存の `Background` と右上の `SettingsButton` をそのまま残す)**【確定】** |
| **背景素材** | **IMG-BG-01(タイトル背景)【確定】**。IMG-BG-02(工場背景)は `Screen_GamePlay` 専用 |
| **`Screen_ModeSelect.prefab` から移す要素** | **`PlayButton_Time90` だけ【確定】**(`StartSessionAndNavigateButton` の設定ごと。子の `Label` は `ModeTitleText` に改名)。**移植元の位置・サイズ(700×300・中央)は仮置き値とみなし、§2.1 の初期値にする【確定】** |
| **新しく作る要素** | `LogoImage`(+ 仮文字 `LogoPlaceholderText`)/ `RecordPanel` / `FirstTimeHint` / `ComingSoonPanel` / `NewContentBadge`(MVPは非表示) |
| **廃止する要素** | **`TapAnywhereButton`(子の `Label` ごと)**/ 未作成だった `TapToStartText` / 旧 ModeSelect 仕様の `HeaderRoot`・`HeaderText`・`BackButton`・下部の `SettingsButton` |
| **全面タップ** | **廃止【確定】**(§0.5) |
| **廃止する画面** | **`Screen_ModeSelect`**(Prefab・`ScreenId.ModeSelect`・`Main.unity` の登録行)。**`ScreenId` の他の数値は固定【確定】**。§0.3 |
| **設定の入口** | **この画面の右上の `SettingsButton` だけ**(旧 §7-7「どこに置くか」は統合により解決) |

### 0.2 画面名と、どちらの Prefab を土台にするか —— 3案の比較

| 案 | 内容 | 長所 | 短所 | 実装コスト |
|---|---|---|---|---|
| **A. `Screen_Title` を土台にし、名前も維持(★確定)** | `Screen_Title.prefab` に ModeSelect の要素を移す | ①既存の右上 `SettingsButton` の位置をまったく動かさずに済む ②`ScreenId.Title` を指している既存の参照(`Main.unity` の登録・`InitialScreenBootstrap.initialScreen`・設定画面の「もどる」)が**そのまま正しくなる** ③BGM ID `bgm_title` と名前が一致したまま | 「Title」という名前が「ホーム」の役割も持つ(文書で補う) | **小** |
| B. `Screen_Home` を新設 | 両 Prefab を廃止し、新しい Prefab を作る | 名前が役割と一致する | **両方の Prefab の既存レイアウト(設定ボタン・背景)を捨てて作り直すことになる。**クラス名・ファイル名・enum 名・結合テスト・文書の改名が広がる。得るものは名前の分かりやすさだけ | 中 |
| C. `Screen_ModeSelect` を土台にする | ModeSelect にロゴと設定ボタンを足す | 旧仕様で「実質的なホーム」だったのは ModeSelect | **設定ボタン(Title 側)を作り直すことになる。**起動時の画面(`initialScreen = Title`)・設定の「もどる」(`Title`)の参照を全部付け替える必要がある | 中 |

**A で確定(2026-09-17 ディレクター)。採用理由は3つ。**

1. **レイアウト資産を守る観点**: 2026-09-17 時点で、両 Prefab はどちらもテストプレイ用の仮置き(`PlaceholderScreenRefs`)で、仕様書どおりの要素はほとんど作られていない。
   **ディレクターが手で調整した可能性がある値**は、Title 側の `SettingsButton`(2026-09-16 追加)と両画面の `Background`、ModeSelect 側の `PlayButton_Time90`(700×300。仕様の 900×320 と違う)だけ。
   **Title を土台にすれば、動かす既存要素は `PlayButton_Time90` の1個だけで済む。**
2. **要素名はコードとの契約**: `ScreenId.Title` と `Screen_Title.prefab`(GUID)を残すので、**付け替えが必要な参照は「ModeSelect を指している箇所」だけ**になる(§0.3 の表)。改名(B)は、この契約を全面的に張り替えることになる。
3. **BGM の一致**: 統合の理由そのものである `bgm_title` と名前が揃っている。

> **ドキュメント上の呼び方**: 以後、本書と関連文書では「**タイトル画面(ホーム)**」または `Screen_Title` と書く。

### 0.3 `ScreenId.ModeSelect` の廃止 —— ★enum の数値をずらさないこと(重要)

> **`ScreenId.ModeSelect` は廃止する。ただし、enum から項目を消すと、後ろの項目の数値が1つずつ繰り上がる。**

`ScreenId` は Prefab・シーンに**整数で保存**されている(2026-09-17 確認)。

| 保存されている場所 | 保存値 | 意味 |
|---|---|---|
| `Main.unity` の `ScreenFlowManager.screenPrefabs` | 1 / 2 / 3 / 5 / 6 | Title / ModeSelect / Settings / GamePlay / Result |
| `Main.unity` の `InitialScreenBootstrap.initialScreen` | 1 | Title |
| `Screen_Title.prefab` の `TapAnywhereButton`(`ScreenNavButton.target`) | 2 | ModeSelect(★この要素は削除する) |
| `Screen_Title.prefab` の `SettingsButton`(`ScreenNavButton.target`) | 3 | Settings |
| `Screen_Settings.prefab` の `BackButton`(`ScreenNavButton.target`) | 1 | Title |
| `Screen_Result.prefab` の「やめる」(`ScreenNavButton.target`) | **2** | **ModeSelect → Title(1)に変更が必要** |
| `Screen_Result.prefab` の「もう一回」/ `Screen_ModeSelect.prefab` の開始ボタン(`StartSessionAndNavigateButton.target`) | 5 | GamePlay |

**`ModeSelect` を enum から単純に消すと、Settings が 3 → 2、GamePlay が 5 → 4 のようにずれ、上の保存値がすべて別の画面を指すようになる**(設定ボタンを押すと存在しない画面へ行く等)。

**必須要件**

- **残す項目の数値を変えないこと。**方法は開発チームの判断でよい(例: 全項目に `Boot = 0, Title = 1, Settings = 3, Credits = 4, GamePlay = 5, Result = 6` と明示的に番号を振ってから `ModeSelect` を削除し、2 は欠番にする)。
- **値 2 を保存している箇所をすべて付け替えること**(上の表の太字。`TapAnywhereButton` は削除するので対象外)。
- **コード上の参照を付け替えること**: `GameSessionController.QuitWithoutRecording`(`ScreenId.ModeSelect` → `ScreenId.Title`)、`ScreenFlowManager.BgmTitleScreens`(`ModeSelect` を外す)。
- **`Main.unity` の `screenPrefabs` から id 2 の行を削除**し、`Screen_ModeSelect.prefab` を削除する(§0.4 の手順7)。
- **結合テスト** `GamePlaySceneIntegrationTests.FullLoop_TitleToModeSelectToGamePlayToResultToRetry_CompletesWithoutErrors` は「Title → ModeSelect」の経路を前提にしている(Title の最初の `Button` を押して ModeSelect を待つ)。**「Title の `PlayButton_Time90` → GamePlay」に書き換えること。**`GetComponentInChildren<Button>()` は `SettingsButton` を拾う可能性があるので、Refs 経由で取ること。

### 0.4 一度きりのスキャフォールドの方針

**CLAUDE.md「UIレイアウトの取り扱い(厳守)」に従う。** 方法(一度きりの Editor スクリプト / Unity Editor 起動中なら YAML の直接編集。2026-09-16 に前例あり)は developer の判断でよいが、**どちらでも次を守ること。**

| 手順 | 内容 |
|---|---|
| 0. ガード | **`Screen_Title.prefab` に `PlayButton_Time90` / `RecordPanel` / `LogoImage` のいずれかが既にあれば、何もせず中断する**(ログを出す)。二重実行でディレクターの調整を上書きしないため |
| 1. 既存要素を触らない | `Background` / `SafeAreaRoot` / `ContentRoot` / `BottomReserve` / `SettingsButton`(子の `Label` 含む)の **RectTransform・フォントサイズ・色・並び順を変えない** |
| 2. 移植 | `Screen_ModeSelect.prefab` の `PlayButton_Time90` を**複製して `Screen_Title` の `ContentRoot` の子**に入れる。`StartSessionAndNavigateButton` の設定(`mode` = `GameModeDefinition_Time90.asset` / `retry` = false / `target` = GamePlay)を**保ったまま**にする。RectTransform は §2.1 の初期値にする(★2026-09-17 確定: 移植元の 700×300・中央は仮置き値なので引き継がない)。子の `Label` は `ModeTitleText` に改名し、§2.1 の値にする |
| 3. 新規作成 | `LogoImage`・`RecordPanel`・`FirstTimeHint`・`ComingSoonPanel`・`NewContentBadge` とその子を §2.1 の初期値で作る。**描画順(兄弟の並び)は §2 のツリーのとおり**。`SettingsButton` は `SafeAreaRoot` の最後の子のまま(最前面) |
| 4. 削除 | **`TapAnywhereButton` を子の `Label` ごと削除する**(ディレクター決定の統合に含まれる削除。§0.5) |
| 5. 差し替え | ルートの `PlaceholderScreenRefs` / `PlaceholderScreenController` を **`TitleScreenRefs` / `TitleScreenController`** に差し替え、参照を配線する(§3 の末尾)。**`Screen_Result` など他の仮置き画面が `PlaceholderScreenRefs` を使い続けるので、クラス自体は消さない** |
| 6. 削除 | 実行したスクリプトを削除する |
| 7. 旧画面の片付け | **手順2〜5の結果を Unity で開いて確認した後で**、`Screen_ModeSelect.prefab` の削除と `Main.unity` の登録行(id 2)の削除、§0.3 の付け替えを行う。**移植元を先に消さない** |

### 0.5 全面タップ(`TapAnywhereButton`)の扱い —— **廃止(★2026-09-17 ディレクター確定)**

| 案 | 内容 | 評価 |
|---|---|---|
| **廃止する(★確定)** | 押せる場所は `PlayButton_Time90` と `SettingsButton` の2つだけ | **押せる場所と押せない場所が見た目どおりになる。**記録や告知を読もうとして触っただけでゲームが始まる事故がない |
| 残す(タップでゲーム開始) | 画面のどこを押しても90秒モードが始まる | ①`ComingSoonPanel` / `RecordPanel` は `raycastTarget = false` なので、**告知や記録を触ると背後の全面ボタンに届いてゲームが始まる** ②12月に時間無制限モードが増えると「どのモードで始まるのか」が決まらない ③ボタンの形をしていない場所で始まるのは、4+向けに分かりにくい |
| 残す(何もしない) | 置く意味がない | — |

> **旧 `TapAnywhereButton` が担っていた「起動直後の連打で素通りするのを防ぐ 0.3秒の入力遅延」は、`PlayButton_Time90` と `SettingsButton` に引き継ぐ**(§4.2)。
> **全面タップが無くなったので、「全面タップの下端をバナーから40px上げる」対策(旧 §6・§8 T-1/T-2)は不要になった。**

---

## 1. 画面の目的

> **本作の顔(ロゴ)を見せつつ、1タップで90秒モードを始められ、設定も開ける「ホーム画面」。**

**この画面が引き受けるもの**

| # | 役割 | 由来 |
|---|---|---|
| 1 | **ゲームロゴ・背景デザインを見せる** | 旧 Title |
| 2 | **90秒モードを開始する**(12月に時間無制限モードを追加) | 旧 ModeSelect |
| 3 | **ハイスコア・最高ランクの表示** | 旧 ModeSelect(もとは Title) |
| 4 | **時間無制限モードの告知**(12月) | 旧 ModeSelect(もとは Title) |
| 5 | **初回起動時の案内**(開始ボタンを指し示す) | 旧 ModeSelect |
| 6 | **設定を開く** | 旧 Title(2026-09-16 仮置き)→ **正式な入口** |

**設計基準は3つ。**

1. **`PlayButton_Time90` が圧倒的に主役**であること(他の要素と大きさで差をつける)
2. **初回起動時は、そのボタンを押せばよいと一目で分かる**こと(§4.5)
3. **ロゴを見せつつ過密にしない**こと(§2.2 で検証)

### 1.1 第2版(2026-09-07)からの変更

| 要素 | 旧 Title(第2版) | 旧 ModeSelect | **統合後(第3版)** |
|---|---|---|---|
| ロゴ | あり(中央) | なし | **あり(上部)** |
| 全面タップ | あり(→ ModeSelect) | — | **★廃止** |
| 「タップして すすむ」 | あり(未作成) | — | **★廃止** |
| 90秒モードの開始ボタン | — | あり | **あり(移植)** |
| 記録表示 | — | あり | **あり** |
| 時間無制限モードの告知 | — | あり | **あり** |
| 初回ヒント | — | あり | **あり** |
| 設定ボタン | 右上(仮置き) | 下部中央 | **右上の1つだけ**(既存を使う) |
| ヘッダ「しごとを えらぶ」 | — | あり | **★廃止**(ロゴが画面の見出しを兼ねる) |
| もどるボタン | — | あり(→ Title) | **★廃止**(戻る先が無い) |

### 1.2 遷移元 / 遷移先

| | 画面 | 条件 |
|---|---|---|
| **遷移元** | `Screen_Boot` | **アプリ起動時。常に**(★2026-09-17 時点は `Screen_Boot` 未作成のため、`InitialScreenBootstrap` が直接この画面を出している) |
| **遷移元** | `Screen_Settings` | 「もどる」 |
| **遷移元** ★ | **`Screen_Result`** | **「やめる」**(★2026-09-17 変更。旧遷移先 `Screen_ModeSelect`) |
| **遷移元** ★ | **`Overlay_Pause`** | **「やめる」**(★同上。セッション中断。スコアは記録しない) |
| **遷移先** | **`Screen_GamePlay`** | **`PlayButton_Time90`**(`retry = false`。**カウントダウン「3・2・1・スタート!」3.0秒**を表示してから開始。`30_Overlay_Countdown.md`) |
| **遷移先** | `Screen_Settings` | **`SettingsButton`**(右上) |

**プレイのループ**

```
[起動] → Title(ホーム) ─[90びょう モード]→ GamePlay → Result ─┬─[もう一回]→ GamePlay(1タップ)
            ↑   ⇅ [せってい]/[もどる]                           │
            │  Settings ⇄ Credits                               │
            └──────────────────[やめる]────────────────────────┘
                (Pause の「やめる」も同じ)
```

### 1.3 ★起動からプレイ開始までのタップ数が 2 → 1 に戻った

| | 統合前 | **統合後** |
|---|---|---|
| アプリ起動 → 90秒モード開始 | 2タップ(Title を押す → ModeSelect の開始ボタン) | **1タップ**(開始ボタン) |
| 結果画面 → もう一度(もう一回) | 1タップ | 1タップ(変わらない) |
| 結果画面 → やめる → もう一度 | 2タップ | 2タップ(変わらない) |
| アプリ起動 → 設定 | 1タップ | 1タップ |
| プレイ後 → 設定 | **3タップ**(やめる → ModeSelect のもどる → Title の設定。ModeSelect に設定導線が無かったため) | **2タップ**(やめる → 設定) |

---

## 2. Prefab構造(統合後)

```
Screen_Title                              stretch 全面
├─ [TitleScreenRefs]                      ★2026-09-17 PlaceholderScreenRefs から差し替え
├─ [TitleScreenController]                ★同上
└─ SafeAreaRoot                           stretch(既存)
    ├─ Background                         Image / stretch(既存。★値を変えない)
    ├─ ContentRoot                        上下ストレッチ(既存。★値を変えない)
    │   ├─ LogoImage                      ★新規 anchor(0.5,1) / 900×380   ロゴ(IMG-TI-01)
    │   │   └─ LogoPlaceholderText        ★新規 stretch 「サンタのお仕事(仮)」(ロゴ画像が入るまでの仮)
    │   ├─ RecordPanel                    ★新規 anchor(0.5,1) / 900×140
    │   │   ├─ HighScoreLabel             「ハイスコア」
    │   │   ├─ HighScoreValueText         「3,240」/「—」
    │   │   └─ BestRankValueText          「ベテランサンタ」
    │   ├─ PlayButton_Time90              ★移植 anchor(0.5,1) / 900×320   主役
    │   │   ├─ ModeTitleText              ★移植(旧 Label を改名)「90びょう モード」
    │   │   └─ ModeDescText               ★新規 「90びょうで しごとを かたづけよう」
    │   ├─ FirstTimeHint                  ★新規 anchor(0.5,1) / 900×110   初回のみ
    │   │   ├─ HintArrowImage             上向きの矢印/指(IMG-MS-02)
    │   │   └─ HintText                   「ここを おして はじめよう」
    │   ├─ ComingSoonPanel                ★新規 anchor(0.5,1) / 900×180   押せない告知
    │   │   ├─ ComingSoonIconImage        告知アイコン(IMG-MS-03。無い間は透明)
    │   │   ├─ ComingSoonTitleText        「じかん むせいげん モード」
    │   │   └─ ComingSoonBodyText         「12がつに とうじょう よてい」
    │   └─ NewContentBadge                ★新規 anchor(0.5,1) / 900×90    12月用。MVPは非表示
    │       └─ NewContentText             「あたらしい しごとが ふえました」
    ├─ BottomReserve                      (既存。高さは実行時計算)
    └─ SettingsButton                     (既存。右上 200×160 / SafeAreaRoot の最後の子 = 最前面。★値を変えない)
        └─ Label                          (既存)「せってい」
```

- **`LayoutGroup` を使わない。** すべて固定の絶対配置(ディレクターがドラッグで動かせること)。
- ~~`TapAnywhereButton`~~ / ~~`TapToStartText`~~ は**置かない**(§0.5)。
- **`CreditsButton` は置かない**(2026-09-07 決定。クレジットは `Screen_Settings` の中だけ)。

### 2.1 レイアウト初期値(★新規・移植する要素のみ。**以後ディレクターの所有物**)

**親が `ContentRoot` の要素**(`ContentRoot` の上端から測る。基準解像度 1080×1920 で `ContentRoot` の高さ ≒ 1464)

| 要素 | anchor / pivot | anchoredPosition | sizeDelta | 初期の見た目(仮) |
|---|---|---|---|---|
| `LogoImage` | (0.5,1)/(0.5,1) | (0, -60) | (900, 380) | Image。**Sprite なし・色 alpha 0**(ロゴ画像が入ったら alpha 1)。`raycastTarget = false` |
| `RecordPanel` | (0.5,1)/(0.5,1) | (0, -480) | (900, 140) | Image(IMG-UI-03)。仮は黒 alpha 0.25。`raycastTarget = false` |
| `PlayButton_Time90` | (0.5,1)/(0.5,1) | (0, -660) | (900, 320) | 移植元の色(赤 0.8/0.3/0.3)のまま |
| `FirstTimeHint` | (0.5,1)/(0.5,1) | (0, -1000) | (900, 110) | 背景なし。`CanvasGroup`(alpha で切替。`blocksRaycasts = false`) |
| `ComingSoonPanel` | (0.5,1)/(0.5,1) | (0, -1130) | (900, 180) | Image(IMG-UI-03)。仮は黒 alpha 0.25。`CanvasGroup`。**子を含めて `raycastTarget = false`** |
| `NewContentBadge` | (0.5,1)/(0.5,1) | (0, -1330) | (900, 90) | `CanvasGroup` alpha 0(MVP) |

**子要素**

| 親 | 要素 | anchor / pivot | anchoredPosition | sizeDelta | フォントサイズ / 揃え |
|---|---|---|---|---|---|
| `LogoImage` | `LogoPlaceholderText` | stretch(0,0)-(1,1) | offset 0 | — | 96 / 中央 |
| `RecordPanel` | `HighScoreLabel` | (0,1)/(0,1) | (40, -20) | (260, 50) | 36 / 左 |
| `RecordPanel` | `HighScoreValueText` | (1,1)/(1,1) | (-40, -5) | (540, 65) | 60 / 右 |
| `RecordPanel` | `BestRankValueText` | (0,0)/(0,0) | (40, 12) | (820, 50) | 36 / 左 |
| `PlayButton_Time90` | `ModeTitleText` | (0.5,1)/(0.5,1) | (0, -50) | (820, 130) | 88 / 中央 |
| `PlayButton_Time90` | `ModeDescText` | (0.5,0)/(0.5,0) | (0, 45) | (820, 70) | 40 / 中央 |
| `FirstTimeHint` | `HintArrowImage` | (0,0.5)/(0,0.5) | (60, 0) | (90, 90) | — |
| `FirstTimeHint` | `HintText` | (0,0.5)/(0,0.5) | (180, 0) | (680, 90) | 44 / 左 |
| `ComingSoonPanel` | `ComingSoonIconImage` | (0,0.5)/(0,0.5) | (30, 0) | (120, 120) | — (Sprite が無い間は alpha 0) |
| `ComingSoonPanel` | `ComingSoonTitleText` | (0,1)/(0,1) | (180, -25) | (690, 70) | 44 / 左 |
| `ComingSoonPanel` | `ComingSoonBodyText` | (0,0)/(0,0) | (180, 25) | (690, 60) | 36 / 左 |
| `NewContentBadge` | `NewContentText` | stretch | offset 0 | — | 40 / 中央 |

- **文字色の初期値は白**(背景が紺のため)。**背景素材(IMG-BG-01)を差し替えたら文字色も見直す**(共通仕様 §7.7)。
- **既存の `SettingsButton`**: `SafeAreaRoot` の子、anchor/pivot (1,1)、位置 (-20, -20)、200×160(2026-09-17 時点の Prefab の値。**変更しない**)。

### 2.2 検算(過密になっていないか)

**`ContentRoot` の上端は `SafeAreaRoot` の上端から 160px 下。**

| `ContentRoot` 上端からの位置 | 要素 | 高さ | 次との余白 |
|---|---|---|---|
| 60 〜 440 | `LogoImage` | 380 | 40 |
| 480 〜 620 | `RecordPanel` | 140 | 40 |
| **660 〜 980** | **`PlayButton_Time90`** | **320** | 20 |
| 1000 〜 1110 | `FirstTimeHint`(初回のみ) | 110 | 20 |
| 1130 〜 1310 | `ComingSoonPanel`(2回目以降) | 180 | 20 |
| 1330 〜 1420 | `NewContentBadge`(MVPは非表示) | 90 | **44**(+ 予約領域内の40) |

| 観点 | 判定 |
|---|---|
| **設定ボタンとロゴ** | 設定ボタンの下端 = `SafeAreaRoot` 上端から 180。ロゴの上端 = 160 + 60 = 220。**間隔 40px** ✓(共通ルール §7.5) |
| **主役の優位性** | `PlayButton_Time90` は 900×320 で、次に大きい告知(900×180)の **1.8倍** ✓ |
| タップできる要素 | **2つだけ**(開始ボタン / 設定)。迷う余地がない ✓ |
| タップ領域 | 開始 900×320 / 設定 200×160。**いずれも最小 160×160 以上** ✓ |
| 開始ボタンとバナー | 開始ボタンの下端から `ContentRoot` の下端まで 484px + 予約領域内の40px。**誤タップの危険がない** ✓ |
| 同時に見える情報 | **平常時は ロゴ / 記録 / 開始ボタン / 告知 の4ブロック**(`FirstTimeHint` と `ComingSoonPanel` は同時に出ない。§3.3)。✓ |
| 親指の届きやすさ | 開始ボタンの中心は画面上端から 160 + 820 = **980px**(画面の縦中央 960 のすぐ下)✓ |

**12月アップデート時の見通し**

- `ComingSoonPanel`(900×180)を `PlayButton_Endless` に差し替える。**主役と同じ 320px にする場合は 140px 足りない。**
  `FirstTimeHint`(110 + 余白20)は2回目以降は出ないので、**その枠を含めて詰め直す**必要がある(ディレクターのレイアウト調整)。§7-9。
- 無制限モードにもハイスコアが必要になる(旧 `99_廃止_Screen_ModeSelect.md` §6.2 E2)。**`RecordPanel` を2行にするか、各モードボタンの中に記録を持たせる。**

---

## 3. 要素一覧

| # | 要素 | 役割 | 表示条件 | 状態 |
|---|---|---|---|---|
| H1 | `Background` | 背景デザイン(既存) | 常時 | 変化しない。**素材は IMG-BG-01(タイトル背景)を使う**(★2026-09-17 確定) |
| H2 | `LogoImage` / `LogoPlaceholderText` | **ゲームロゴ。本作の顔**。ロゴ画像(IMG-TI-01)が入るまでは仮の文字を出す | 常時 | タップ不可。演出は任意(§7-7) |
| H3 | `RecordPanel` | **ハイスコアと最高ランクの表示**(§3.1) | 常時 | タップ不可(`raycastTarget = false`) |
| H4 | **`PlayButton_Time90`** | **90秒モードを開始する。この画面の主役** | 常時 | 画面に入ってから **0.3秒は非活性**(§4.2)。それ以外は活性。**初回のみ脈動演出**(§4.5)。押したら二重遷移防止のため非活性(§4.6) |
| H5 | `FirstTimeHint` | **初回起動時の案内。**開始ボタンを指し示す | **`santa.firstLaunchDone == false` のときのみ** | `CanvasGroup.alpha` で切替(レイアウトは動かない)。タップ不可 |
| H6 | `ComingSoonPanel` | **時間無制限モードの告知。ボタンではない**(§3.2) | **`santa.firstLaunchDone == true` のときのみ**(§3.3) | `CanvasGroup.alpha` で切替。**子も含めてタップ不可** |
| H7 | `NewContentBadge` | 「あたらしい しごとが ふえました」(12月) | **MVPでは常に非表示** | `CanvasGroup.alpha = 0` |
| H8 | `SettingsButton` | **`Screen_Settings` を開く**(既存) | 常時 | 画面に入ってから **0.3秒は非活性**(§4.2)。見た目は仮(紺地 + 「せってい」)。歯車アイコン IMG-TI-03 が入ったら差し替え |
| — | ~~`TapAnywhereButton`~~ / ~~`TapToStartText`~~ / ~~`HeaderText`~~ / ~~`BackButton`~~ | — | **置かない**(§0.5 / §1.1) | — |

**`TitleScreenRefs` が持つ参照**

```csharp
public class TitleScreenRefs : ScreenRefsBase   // safeAreaRoot / contentRoot / bottomReserve は基底が持つ
{
    [Header("この画面固有")]
    [SerializeField] private Button      playButtonTime90;
    [SerializeField] private Button      settingsButton;
    [SerializeField] private TMP_Text    highScoreValueText;
    [SerializeField] private TMP_Text    bestRankValueText;
    [SerializeField] private CanvasGroup firstTimeHint;
    [SerializeField] private CanvasGroup comingSoonPanel;
    [SerializeField] private CanvasGroup newContentBadge;
    [SerializeField] private TMP_Text    modeTitleText;     // GameModeDefinition の文言キーを流し込む場合(§3.4)
    [SerializeField] private TMP_Text    modeDescText;
    // ロゴ・背景・ラベル類はコードから触らないので持たない
}
```

> ※上は形の例。`ScreenRefsBase` の実際のメンバー名・テキスト型は既存コードに合わせること。

### 3.1 記録表示(`RecordPanel`)

| 条件 | `HighScoreValueText` | `BestRankValueText` |
|---|---|---|
| `santa.totalPlays == 0` | **「—」** | **「みならいサンタ」**(`RankTable` の0番目) |
| それ以外 | ハイスコア(3桁区切り。例「3,240」) | `GameModeDefinition_Time90` の `RankTable.Ranks[santa.bestRankIndex].titleTextKey` をローカライズした文字 |

- **「0」ではなく「—」にする理由**: 「0点だった」ように見えるのを避ける。
- **`RecordPanel` 自体を非表示にしない**(レイアウトが動くため)。
- `bestRankIndex` が `RankTable` の範囲外なら0番目を出す(データ破損・表の縮小に備える)。
- **最高ランクを表示する場所はこの画面だけ。**ここに置かないと `santa.bestRankIndex` は誰も読まないデータになる。

### 3.2 時間無制限モードの枠を「押せないボタン」にしない理由(旧 ModeSelect §3.1 から継承。2026-09-07 承認済み)

| 案 | 評価 |
|---|---|
| 非表示 | 12月に要素を足すことになり、ディレクターの手調整が崩れる |
| グレーアウトしたボタン | **審査リスク。**App Store ガイドライン 2.1 / 4.2 で「機能しないUI」「未完成に見えるApp」として指摘されうる |
| **告知パネル(★採用)** | **ボタンの形をしていない告知。**レイアウト枠を先に確保でき、審査上も「機能しないボタン」にならない |

**12月に `PlayButton_Endless` へ差し替える**(§2.2 の見通し)。

### 3.3 初回起動時は告知を出さない(旧 ModeSelect §3.3 から継承)

**`FirstTimeHint`(初回案内)と `ComingSoonPanel`(告知)は同時に出さない。**初回は「ここを押す」だけに集中させる。
`ComingSoonPanel` は `santa.firstLaunchDone == true` のときだけ表示する。**`CanvasGroup.alpha` で切り替えるので、レイアウトは動かない。**

### 3.4 文言(ローカライズキー)

| 要素 | 日本語(仮) | キー(案) |
|---|---|---|
| `LogoPlaceholderText` | サンタのお仕事(仮) | (仮置きなので固定文字でよい。ロゴ画像に置き換える) |
| `HighScoreLabel` | ハイスコア | `home.record.highscore` |
| `ModeTitleText` | 90びょう モード | **`GameModeDefinition_Time90.titleTextKey`**(12月に無制限モードのボタンも同じ仕組みで作れるように) |
| `ModeDescText` | 90びょうで しごとを かたづけよう | **`GameModeDefinition_Time90.descTextKey`** |
| `HintText` | ここを おして はじめよう | `home.hint.first` |
| `ComingSoonTitleText` | じかん むせいげん モード | `home.comingsoon.title` |
| `ComingSoonBodyText` | 12がつに とうじょう よてい | `home.comingsoon.body` |
| `NewContentText` | あたらしい しごとが ふえました | `home.newcontent` |

- **教育漢字 + 分かち書き**のルールに従う。キー名は既存の `LocalizationTable` の命名に合わせて変えてよい。
- **共通仕様 §9 のキー `title.start.label`(「しごとを はじめる」)は、この改訂で使わなくなる**(開始ボタンの文字は `GameModeDefinition` のキーを使うため)。

---

## 4. 処理内容

### 4.1 画面に入ったとき

```
1. (基底クラス)BottomReserve / ContentRoot の実行時追従
2. SaveManager から highScore / bestRankIndex / totalPlays / firstLaunchDone を読む
3. RecordPanel に反映(§3.1)
4. FirstTimeHint.alpha = firstLaunchDone ? 0 : 1
   → 表示するなら PlayButton_Time90 の脈動演出を開始(§4.5)
5. ComingSoonPanel.alpha = firstLaunchDone ? 1 : 0(§3.3)
6. NewContentBadge.alpha = 0(MVP)
7. PlayButton_Time90 / SettingsButton を 0.3秒 非活性にする(§4.2)
8. BGM は bgm_title(ScreenFlowManager.ShowScreen が処理済み。同じ曲なら頭出ししない)
9. AdDisplayPolicy に従いバナーを表示する(★この画面は表示する)
```

> **設定画面の「もどる」で戻ったときも同じ処理を行う**(画面は都度生成されるため)。BGM は `bgm_title` のまま鳴り続ける。
> **結果画面・ポーズの「やめる」で来たときは、BGM が `bgm_result` / `bgm_gameplay` から切り替わるので最初から再生される**(共通仕様 §11.1)。

### 4.2 入力受付の遅延 0.3秒 —— **開始ボタンと設定ボタンに掛ける**

| 目的 | 内容 |
|---|---|
| ① 起動直後の連打 | この画面は Unity ロゴのスプラッシュの直後に出る。**スプラッシュを飛ばそうとする連打がそのまま開始ボタンに当たり、ロゴも記録も見ないままゲームが始まる**のを防ぐ(旧 Title §4.2 の目的を引き継ぐ) |
| ② **★結果画面の「やめる」からの残留タップ(2026-09-17 新たに生じた)** | 結果画面の `QuitButton`(初期値: 画面下端から約 696〜816px、横 400px・中央)と、この画面の `PlayButton_Time90`(画面下端から約 780〜1100px、横 900px・中央)は**縦に約36px重なる。**「やめる」を2回続けて押すと、**2回目が開始ボタンに当たり、やめたはずのゲームが始まる。**0.3秒の遅延がこれを吸収する |

- 値は `GameBalanceSettings` に置く(既存の「タイトルの入力遅延 0.3秒」。無ければ追加)。
- **遅延中は `Button.interactable = false`**(見た目が一瞬暗くなるのが気になる場合は、`ColorBlock.disabledColor` を通常色に揃えるか、`CanvasGroup.interactable` で止める。方法は developer 判断)。
- **「最低表示時間」は設けない**(企画書 §1.1「テンポを削る要素は入れない」)。
- **★2026-09-16 時点で、この遅延は未実装**(旧 `TapAnywhereButton` にも無かった)。今回の統合で実装する。

### 4.3 操作

| 要素 | 操作 | 処理 |
|---|---|---|
| **`PlayButton_Time90`** | タップ | ① `se_button` ② **`santa.firstLaunchDone = true` を保存**(§4.4)③ 脈動演出を止める ④ **開始ボタンと設定ボタンを両方非活性にする**(§4.6)⑤ `GameSessionController.StartSession(GameModeDefinition_Time90, retry: false)` ⑥ **`Screen_GamePlay` へ遷移** ⑦ BGM は `StartSession` が `bgm_gameplay` に切り替える ⑧ バナーは `AdDisplayPolicy` に従い非表示 |
| **`SettingsButton`** | タップ | ① `se_button` ② **開始ボタンと設定ボタンを両方非活性にする** ③ **`Screen_Settings` へ遷移**。BGM は切り替えない |
| `RecordPanel` / `ComingSoonPanel` / `LogoImage` / `FirstTimeHint` / 背景 | タップ | **何も起きない**(`raycastTarget = false`。将来 `Screen_Records` を追加するなら `RecordPanel` を押せるようにする) |

- ⑤⑥ は既存の `StartSessionAndNavigateButton`(`PlayButton_Time90` に付いている)がそのまま行う。**①②④ を `TitleScreenController` 側で足す**形でよい(`onClick` のリスナー追加。呼ばれる順序に依存しない書き方にする)。
- `SettingsButton` の遷移は既存の `ScreenNavButton`(target = Settings)がそのまま行う。

### 4.4 `santa.firstLaunchDone` の意味(旧 ModeSelect §4.3 から継承)

| | 内容 |
|---|---|
| 意味 | **一度でもプレイを開始した** |
| 立てる場所 | **この画面の `PlayButton_Time90` を押したとき**(★2026-09-17: 旧 `Screen_ModeSelect` から移った) |
| 使い道 | `FirstTimeHint` と `ComingSoonPanel` の表示判定 |

**キー名は変えない**(マイグレーションを避けるため)。

### 4.5 初回案内の演出(旧 ModeSelect §4.4 から継承)

- `FirstTimeHint` を表示し、**`PlayButton_Time90` に軽い脈動**(`localScale` 1.0 ⇔ 1.03、1往復 約1.2秒)を付ける。
- **点滅は毎秒2回まで**(光過敏性発作への配慮。共通仕様 §13-12)。
- **`localScale` だけを動かす。**`sizeDelta` / `anchoredPosition` には触れない。**画面を離れる・演出を止めるときは必ず 1.0 に戻す。Prefab アセットを書き換えないこと。**
- 押したら二度と出ない(`firstLaunchDone` が立つため)。
- **ここで伝えられるのは「押す場所」だけ。**ゲームのルール(3件でクリア / コンボ / 誤答だけが失敗)は `Screen_GamePlay` 側の仕組みが伝える(`99_廃止_Screen_Tutorial.md` §2)。

### 4.6 二重遷移の防止

- **開始ボタンの連打で `StartSession` が2回呼ばれない / `Screen_GamePlay` が二重に生成されないこと。**
- **開始ボタンと設定ボタンをほぼ同時に(マルチタッチで)押したとき、遷移が1回だけになること。**どちらかが押された時点で両方を非活性にする。

---

## 5. 音声

- BGM: `bgm_title`(仮素材 `Assets/Audio/BGM/test/bgm.ogg` 29.0秒・ループ)
  - **アプリ起動後、この画面に来たときに最初から再生を始める。**
  - **`Screen_Settings` / `Screen_Credits` との行き来では途切れさせず、頭出しもしない**(共通仕様 §11.1)。
  - **結果画面・ポーズの「やめる」で戻ったときは最初から再生**(別の BGM から切り替わるため)。
  - 開始ボタンで `bgm_gameplay` に切り替わる。
- SE: `se_button`(ボタン)/ `se_screen`(画面遷移。`ScreenFlowManager` が鳴らす)

---

## 6. 広告バナー

- **この画面ではバナーを表示する**(2026-09-07 決定。全画面で一貫して広告を出す)。予約領域は常に確保する。
- **全面タップを廃止したので、旧 §6 の「全面タップの下端をバナーから40px上げる」対策は不要になった。**
- 開始ボタンの下端とバナー予約領域の間は 484px 以上空いている(§2.2)。**押せる要素がバナーに近い、という誤タップのリスクは無い。**

---

## 7. 未確定事項

| # | 内容 | 影響 | 判断者 |
|---|---|---|---|
| ~~1~~ | ~~画面名を `Screen_Title` のまま使うか~~ | **決定(2026-09-17 ディレクター): 変えない** | — |
| ~~2~~ | ~~全面タップを廃止するか~~ | **決定(2026-09-17 ディレクター): 廃止**(要素の削除に合意済み) | — |
| ~~3~~ | ~~旧 ModeSelect の `PlayButton_Time90`(700×300・中央)はディレクターの調整値か~~ | **決定(2026-09-17 ディレクター): 仮置き値とみなす。**§2.1 の初期値(900×320、ロゴ・記録の下)にする | — |
| ~~4~~ | ~~`ScreenId.ModeSelect` を廃止するか~~ | **決定(2026-09-17): 廃止。他の項目の数値は固定**(§0.3) | — |
| **5** | **正式タイトルとロゴ素材**(「サンタのお仕事(仮)」のまま) | ロゴ・アプリ名・ストア掲載名 | ディレクター |
| ~~6~~ | ~~背景素材(IMG-BG-01 / IMG-BG-02)~~ | **決定(2026-09-17 ディレクター): IMG-BG-01(タイトル背景)。**IMG-BG-02 は `Screen_GamePlay` 専用 | — |
| 7 | ロゴの登場演出を付けるか | 起動のたびに見る画面なので**短く・スキップ不要な程度に** | ディレクター |
| 8 | 設定ボタンの見た目(歯車アイコン IMG-TI-03 / 文字)と正確な位置 | 見た目のみ | ディレクター(レイアウト所有物) |
| **9** ★ | **12月に `ComingSoonPanel` を `PlayButton_Endless`(320px)に差し替えるときの詰め直し**(§2.2) | 140px 足りない。`FirstTimeHint` の枠を使って詰め直す | ディレクター(12月) |
| 10 | 初回起動時に `ComingSoonPanel` を出さない(§3.3。旧 ModeSelect §8-5) | 初めて見る画面の情報量 | ディレクター |
| 11 | セーブキー `santa.lastSeenVersion`(12月の告知判定用)を MVP から用意するか(旧 ModeSelect §8-10) | 後から足すとマイグレーションが要る | ディレクター |
| 12 | 文言(§3.4)。「ここを おして はじめよう」と矢印の絵(IMG-MS-02) | UI文言・アセット | ディレクター |
| ~~13~~ | ~~開始ボタンを押してから操作できるまでの待ち時間~~ | **決定(2026-09-17 ディレクター): カウントダウン「3・2・1・スタート!」を表示する(長さは 3.0秒のまま、SE は当面無音)。**押してから操作まで 4.5秒は了承済み(`30_Overlay_Countdown.md` §0.2) | — |
| 14 | Android で配信する場合、**この画面で OS の「戻る」操作をしたときの扱い**(アプリを閉じる / 何もしない) | ホーム画面が1つになったので「戻る先」が無い | ディレクター + developer(配信先が決まってから) |
| ~~旧7~~ | ~~設定の入口をどこに置くか(タイトル / ModeSelect / 両方)~~ | **統合により解決(2026-09-17)。**入口はこの画面の右上の1つだけ | — |
| ~~旧 ModeSelect 7~~ | ~~ヘッダの文言「しごとを えらぶ」~~ | **統合によりヘッダごと廃止** | — |

---

## 8. 開発チームの技術判断が必要な項目

| # | 内容 |
|---|---|
| **H-1** ★ | **`ScreenId` の数値を固定したまま `ModeSelect` を消す方法**(§0.3)。値 2 を保存している箇所が残っていないかを確認する手段(Editor の検査・テスト) |
| **H-2** ★ | **統合の作業方法**(一度きりの Editor スクリプト / YAML 直接編集)。どちらでも §0.4 の手順とガードを守ること |
| **H-3** ★ | **開始ボタンに付いている `StartSessionAndNavigateButton` と、`TitleScreenController` が足す処理(`firstLaunchDone` の保存・両ボタンの非活性化)の関係。**リスナーの呼ばれる順序に依存しないこと。コンポーネントに統合するかは developer 判断 |
| **H-4** | 0.3秒の入力遅延を、**画面遷移の共通処理として持つか、この画面だけに持つか**(旧 T-3) |
| **H-5** | 脈動演出で `localScale` を実行時に動かすとき、Prefab アセットを書き換えないこと(旧 MS-2) |
| **H-6** | `ComingSoonPanel` の `raycastTarget = false` が、子要素も含めて確実にタップを受けないこと(旧 MS-3) |
| **H-7** | ロゴ画像の解像度(900×380 で表示。Retina を考慮した原寸) |
| **H-8** ★ | **結合テストの書き換え**(§0.3 の最後)。Title → GamePlay → Result → もう一回 / やめる → Title |

---

## 9. 記録(旧版の経緯)

- **2026-09-07**: タイトルは「ロゴとデザインだけ。機能を持たない」と決定し、ハイスコア・最高ランク・12月告知を `Screen_ModeSelect` へ移した。「タイトルへ」は「やめる」(→ `Screen_ModeSelect`)に変わり、タイトルは起動時に1回だけ通る画面になった。
- **2026-09-16**: 例外として右上に設定ボタンを仮置き。設定の入口をどこに置くかは未決(旧 §7-7。推奨は「タイトルと ModeSelect の両方」)だった。
- **2026-09-17**: **ディレクター決定で `Screen_ModeSelect` を統合。**2026-09-07 に ModeSelect へ移した機能がこの画面に戻り、**「タイトル = ホーム画面」**になった。旧 §7-7 は解決。
