# モックアップ置き場

ディレクターが描いた**画面イメージ図**をここに入れてください。
描き方は [モックアップ作成ガイド.md](../モックアップ作成ガイド.md) を参照。

---

## ファイル名

**仕様書のファイル名と揃えてください。**どの仕様書に対応する絵かが一目で分かります。
拡張子は `.png` / `.jpg` / `.pdf` など何でも構いません。

| 描いたもの | ファイル名 | 対応する仕様書 |
|---|---|---|
| タイトル | `Screen_Title.png` | `02_Screen_Title.md` |
| しごとを えらぶ | `Screen_ModeSelect.png` | `03_Screen_ModeSelect.md` |
| ゲーム本編 | `Screen_GamePlay.png` | `10_Screen_GamePlay.md` |
| 結果 | `Screen_Result.png` | `11_Screen_Result.md` |
| せってい | `Screen_Settings.png` | `04_Screen_Settings.md` |
| クレジット | `Screen_Credits.png` | `05_Screen_Credits.md` |
| M1 手紙 | `MicroGame_Letter.png` | `20_MicroGame_Letter.md` |
| M2 住所 | `MicroGame_Address.png` | `21_MicroGame_Address.md` |
| M3 重さ | `MicroGame_Weight.png` | `22_MicroGame_Weight.md` |
| M5 ラッピング | `MicroGame_Wrap.png` | `23_MicroGame_Wrap.md` |
| カウントダウン | `Overlay_Countdown.png` | `30_Overlay_Countdown.md` |
| いちじ ていし | `Overlay_Pause.png` | `31_Overlay_Pause.md` |
| はじめての しごと | `Overlay_MicroGameIntro.png` | `32_Overlay_MicroGameIntro.md` |
| かくにん | `Overlay_Confirm.png` | `04_Screen_Settings.md` §4.4 |

**同じ画面を何案か描いた場合**は `Screen_Title_a.png` `Screen_Title_b.png` のように末尾で分けてください。

**`Screen_Boot` は絵が要りません**(タイトルと同じ背景を敷くだけのため)。

---

## モックアップと「実素材」は別の場所です

| | 置き場所 | 何を入れるか |
|---|---|---|
| **モックアップ** | **このフォルダ** | 配置を決めるための**イメージ図**。ラフでよい |
| **実素材** | **`Assets/Art/` 配下**(Unityプロジェクト内) | ゲームに実際に組み込む**画像ファイル** |

### 実素材の置き場所

| 素材 | 置き場所 |
|---|---|
| **世界地図(6地域を別ファイル)** | `Assets/Art/MicroGames/Address/` |
| M1 の在庫アイコン・便箋 | `Assets/Art/MicroGames/Letter/` |
| M3 の箱・そり・トナカイ・メーター | `Assets/Art/MicroGames/Weight/` |
| M5 のベルト・シュート・形・模様・ゴミ | `Assets/Art/MicroGames/Wrap/` |
| 背景 | `Assets/Art/Backgrounds/` |
| サンタ・トナカイ | `Assets/Art/Characters/` |
| ボタン枠・アイコン・ランク章 | `Assets/Art/UI/` |
| エフェクト | `Assets/Art/Effects/` |

**モックアップだけ先に置いて、実素材は後から**で構いません。
実素材が揃うまでは developer が仮の絵で組みます。

---

## 世界地図だけは扱いが特別です

**これだけは仮の絵で代替できません。**

- **6地域をそれぞれ別ファイル**にしてください(`Asia.png` `Europe.png` `Africa.png` `NorthAmerica.png` `SouthAmerica.png` `Oceania.png` など)
- 1枚にまとめると、地域ごとに「押せる / 色が変わる / 数字が出る」ができません
- **ヨーロッパとオセアニアは実際の面積比より大きく**描いてください(輪郭の内側だけが押せる仕組みのため、輪郭の大きさ = 押しやすさ)
- **国境線は描かない。国名も書かない。州の区分線だけ**(領土問題のある地域の帰属を表明することになるため)

詳細は [モックアップ作成ガイド.md](../モックアップ作成ガイド.md) §5.1。
