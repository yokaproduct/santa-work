# 31 `Overlay_Pause` —— 一時停止

- 作成: 2026-09-07 / プランナー(サブエージェント)
- **前提: [00_共通仕様.md](00_共通仕様.md) と [10_Screen_GamePlay.md](10_Screen_GamePlay.md) を先に読むこと。**
- Prefab: `Assets/Prefabs/UI/Overlays/Overlay_Pause.prefab`
- スクリプト: `PauseOverlayRefs` / `PauseOverlayController`
- **追記: 2026-09-17 / プランナー** —— **ディレクター決定: `Screen_ModeSelect` を `Screen_Title` に統合。**「やめる」の遷移先を **`Screen_Title`** に変更(§1.2 / §3 PA5 / §4.2)。
  ~~**★実装の現状: `Overlay_Pause.prefab` は未作成**(HUD のポーズボタンは `GameSessionController.SetPaused` を直接切り替えるだけ)。**中断処理 `GameSessionController.QuitWithoutRecording` の遷移先が `ScreenId.ModeSelect` になっているので、`ScreenId.Title` に変える**(`02_Screen_Title.md` §0.3)。~~ → **2026-09-17(2) に実装済み**(下記)。
- **改訂: 2026-09-17(2)/ プランナー** —— **ディレクター決定: 「アプリを裏へ回して戻ると再開できない」不具合(`30_Overlay_Countdown.md` §7-5)を、`Overlay_Pause` の実装で解消する。developer が実装した結果を反映した。**
  **実装済みの範囲と、仕様との暫定の差は §7 にまとめた。**§1.1 / §4.1 / §4.2 / §4.4 / §5 / §6 も更新。経緯は [決定ログ.md](../決定ログ.md) §4-12。
  **要点**: ①手動・自動のポーズは共通の入口 `RequestPause()` を通る ②**「はじめから」「やめる」は確認ダイアログなしで即実行(暫定。`Overlay_Confirm` 未実装のため。§5-2)** ③ボタンの色・文字サイズは仮の値(§7.2)。

> **レイアウト値の所有権**: 本書の px・座標・サイズは**初回生成時の初期値**であり、**以後はディレクターの所有物**。

---

## 1. 目的

**セッションを安全に止め、安全に再開する。**

90秒という短いセッションでも、**電話・通知・親に呼ばれる**といった中断は必ず起きる。
**中断でスコアが理不尽に失われないこと**が、この画面の存在理由である。

### 1.1 出るタイミング

| 経路 | 条件 |
|---|---|
| **手動** | `Screen_GamePlay` の HUD にある `PauseButton` を押した |
| **自動** | **アプリがバックグラウンドへ移行した**(`OnApplicationPause(true)` / `OnApplicationFocus(false)`) |

- **★実装(2026-09-17): 手動・自動のどちらも `GameSessionController.RequestPause()` という1つの入口を通る**(HUD の `PauseButtonController` は `RequestPause()` を呼ぶだけ。`OnApplicationPause(true)` / `OnApplicationFocus(false)` も内部で同じ処理を呼ぶ)。
  **`[Idle]`(セッション外)と `[Finish]`(終了演出)のときは何もしない**(§4.5)。**既に `Overlay_Pause` が出ているときも何もしない**(二重表示の防止。PA-6)。
- **手動のポーズボタンが押せるのは `[Play]` 中でポーズしていないときだけ**(`interactable = Phase == Play && !IsPaused`。`10_Screen_GamePlay.md` §3 H4)。**それ以外のフェーズ(`[Countdown]` / `[Prompt]` / `[Intro]` / `[Judge]` 等)でポーズに入るのは、自動ポーズのときだけ。**
- `OverlayLayer` に `Instantiate` される。**`Screen_GamePlay` の Prefab には含めない。**
- **優先順位は最上位**(`Pause > MicroGameIntro > Countdown`。共通仕様 §2.2)。
  他のオーバーレイが出ていたら、**それを閉じてから** `Overlay_Pause` を出す。

### 1.2 遷移先

| 選択 | 遷移先 |
|---|---|
| **つづける** | `Screen_GamePlay`(**`Overlay_Countdown` 短縮版1.5秒を挟んで再開**) |
| **はじめから** | `Screen_GamePlay`(**セッションを破棄して新規開始**。カウントダウン1.5秒) |
| **やめる** | **`Screen_Title`**(★2026-09-17。旧 `Screen_ModeSelect`)(**スコア・統計を一切記録しない**) |

> **【2026-09-07 決定】「タイトルへ」→「やめる」にラベルを変更し、遷移先を `Screen_ModeSelect` にした。**
> **理由**: `Screen_Title` が機能を持たなくなった(ロゴとデザインだけ)ため、
> そこへ戻すと**何も無い画面を経由することになる。**
> `Screen_ModeSelect` へ戻せば、**ハイスコアがすぐ見え、遊び直すのも1タップで済む。**
>
> **【2026-09-17】`Screen_ModeSelect` は `Screen_Title` に統合された。遷移先は `Screen_Title`(ホーム画面)。**上の理由は統合後の `Screen_Title` でそのまま満たされる。ラベルは「やめる」のまま。

---

## 2. Prefab構造(初回生成時)

```
Overlay_Pause                             stretch(OverlayLayer に出す)
├─ [PauseOverlayRefs]
├─ [PauseOverlayController]
├─ Blocker                                Image alpha 0.75 / stretch / raycastTarget = true
└─ Panel                                  anchor(0.5,0.5) / 900×900
    ├─ TitleText                          「きゅうけい ちゅう」
    ├─ ResumeButton                       (0.5,1) / 760×200  「つづける」★主役
    ├─ RestartButton                      (0.5,0.5) / 760×160  「はじめから」
    └─ QuitButton                         (0.5,0) / 760×160  「やめる」
```

- **`Blocker` の alpha は 0.75(暗くする)。**
  `Overlay_Countdown` と違い、**背後を読ませる必要がない**(むしろ問題を見せ続けると
  「ポーズ中に考える」ができてしまい、公平性を損なう)。**この差は意図的。**

### 2.1 レイアウト初期値(**以後ディレクターの所有物**)

| 要素 | anchor / pivot | anchoredPosition | sizeDelta |
|---|---|---|---|
| `Blocker` | stretch | — | 全面 |
| `Panel` | (0.5,0.5)/(0.5,0.5) | (0, 0) | (900, 900) |
| `TitleText` | (0.5,1)/(0.5,1) | (0, -40) | (760, 120) |
| `ResumeButton` | (0.5,1)/(0.5,1) | (0, -200) | (760, 200) |
| `RestartButton` | (0.5,1)/(0.5,1) | (0, -450) | (760, 160) |
| `QuitButton` | (0.5,1)/(0.5,1) | (0, -650) | (760, 160) |

**検算**(`Panel` の高さ 900)
- 見出し 40〜160 / つづける 200〜400 / はじめから 450〜610 / やめる 650〜810
- ボタン間隔: 400→450 = **50px** ✓ / 610→650 = **40px** ✓(共通ルール40px以上)
- タップ領域はいずれも 160×160 を上回る ✓
- 下端の余白 900−810 = 90px ✓

**`ResumeButton` が最も大きい。** 中断からの復帰が最頻の操作であるため。

---

## 3. 要素一覧

| # | 要素 | 役割 | 表示条件 | 状態 |
|---|---|---|---|---|
| PA1 | `Blocker` | 背後を暗くし、タップを遮断する | 常時 | alpha 0.75 |
| PA2 | `TitleText` | 「きゅうけい ちゅう」 | 常時 | — |
| PA3 | `ResumeButton` | **「つづける」。主役。最大サイズ** | 常時 | 常に活性 |
| PA4 | `RestartButton` | **「はじめから」** | **未確定(§5-1)。置くなら常時** | 常に活性 |
| PA5 | `QuitButton` | **「やめる」→ `Screen_Title` へ**(中断。★2026-09-17。旧 `Screen_ModeSelect`) | 常時 | 常に活性 |

> **HUD・スコア・残り時間はポーズ画面に表示しない。**
> 見せると「ポーズして状況を確認する」ことが戦略になり、90秒の公平性が崩れる。
> **`Blocker` を暗くするのはそのため。プランナー決定(§5-3)。**

---

## 4. 処理内容

### 4.1 表示するとき(★何を止めるかの完全なリスト)

```
1. T1(セッションタイマー90秒)を停止する
2. microGame.SetPaused(true) を呼ぶ
     → ミニゲーム内部のアニメーション・ベルトの流れ・自前の状態遷移が止まる
3. T2(ミニゲームタイマー12秒)を停止する
4. 進行中のフェーズ演出(業務提示 / 判定演出)を止める
5. BGM を一時停止する。再生中のループSE(se_belt)を止める。★再生中の長い単発SE(se_microgame_start)を停止する(2026-09-15)
6. HUD の PauseButton を非活性にする
7. 他のオーバーレイ(Countdown / MicroGameIntro)が出ていたら閉じる(§4.4)
8. Overlay_Pause を Instantiate して表示する
```

> **`Time.timeScale = 0` を使わない**ことを推奨する(共通仕様 §13-G5)。
> ポーズ画面自身の演出まで止まるうえ、`timeScale` に依存しないコードとの整合が取りにくい。
> **ただし最終判断は開発チーム。**
> **★実装(2026-09-17): `timeScale` は使わず、`GameSessionController.SetPaused(true)` で自前で止めている**(T1・T2・フェーズ経過時間はポーズ中に加算しない / `microGame.SetPaused` / `AudioManager.PauseAll`)。

> **★実装の順序(2026-09-17)**: `RequestPause()` は ①`[Prompt]` 中なら `se_microgame_start` を停止し「業務提示をやり直す」印を付ける ②`SetPaused(true)`(上の 1〜6。HUD の `PauseButton` は `IsPaused` を見て自動で非活性になる。業務提示の演出は `PausedChanged` を受けた画面側が初期状態に戻す)③`ShowOverlay(Pause)`(**Countdown / MicroGameIntro が出ていれば、`ScreenFlowManager` が優先順位に従って先に閉じる**。上の 7・8)の順。手動ポーズの `se_button` はその後に鳴る。

### 4.2 各要素を操作したとき

| 要素 | 操作 | 処理 |
|---|---|---|
| `ResumeButton` | タップ | ① `se_button` ② `Overlay_Pause` を閉じる ③ **`Overlay_Countdown`(短縮版1.5秒「2・1・スタート!」。背景は暗くしない。この間もポーズは解除しない)** ④ カウントダウン終了後に T1・T2・`microGame.SetPaused(false)`・BGM を再開 ⑤ `PauseButton` を活性化(`[Play]` に戻る場合)。詳細は `30_Overlay_Countdown.md` §6.2 |
| `RestartButton` | タップ | ① `se_button` ② **`Overlay_Pause` を閉じる**(★2026-09-17 明記: 先に閉じないと優先順位で `Overlay_Countdown` を出せない)③ 現在のミニゲームを `Finish(SessionEnded)` して破棄 ④ **セッション状態を全部破棄**(HUD を初期状態に戻し、`PromptBackdrop` を消す)⑤ `StartSession(retry: true)` を呼び直す → `Overlay_Countdown`(Short 1.5秒「2・1・スタート!」)⑥ **スコアは記録しない**。詳細は `30_Overlay_Countdown.md` §6.3 |
| `QuitButton` | タップ | ① `se_button` ② 現在のミニゲームを破棄 ③ **スコア・統計・ハイスコアを一切記録せずに** **`Screen_Title` へ**(★2026-09-17。旧 `Screen_ModeSelect`)④ BGM を `bgm_title` へ(最初から再生) |
| `Blocker` | タップ | **何もしない**(誤操作で閉じないため) |
| (OS) 戻る操作 | — | **iOSには戻るボタンが無いので考慮不要** |

> **`QuitButton` に確認を挟むか(§5-2)。** 中断するとスコアが消えるため、
> **誤タップで90秒のプレイが失われる**。`Overlay_Confirm`(`04_Screen_Settings.md` §4.4)を流用できる。
>
> **★実装(2026-09-17)と上の表の対応**
>
> | 要素 | 実装 |
> |---|---|
> | `ResumeButton` | `se_button` → `GameSessionController.ResumeFromPause()`。**Pause を閉じる → Short カウントダウン(1.5秒。ポーズは解除しないまま進める)→ ポーズ前のフェーズで分岐**: `[Intro]` なら初出カードを再表示(**ポーズは解除しない**。OK で解除される)/ `[Prompt]` なら業務提示を t=0 からやり直す / **それ以外はそのまま再開**(`SetPaused(false)`)。連打しても1回しか走らない。`PauseButton` の活性は `Phase == Play && !IsPaused` で自動的に戻る |
> | `RestartButton` | `se_button` → **`Overlay_Pause` を閉じる** → `StartSession(現在のモード, retry: true)`。**★確認ダイアログは無し(暫定。§5-2 / §7.3)** |
> | `QuitButton` | `se_button` → `QuitWithoutRecording()`(ミニゲームを破棄 → `[Idle]` → **`Screen_Title` へ**。画面遷移が `Overlay_Pause` を閉じる)。**記録しない。★確認ダイアログは無し(暫定。§5-2 / §7.3)** |

### 4.3 ★バックグラウンド移行時の扱い(共通仕様 §13-G6)

```
OnApplicationPause(true) / OnApplicationFocus(false) を受けたら:
  1. §4.1 の停止処理を即座に実行する
  2. Overlay_Pause を表示する
  3. ★T1 に「バックグラウンドにいた時間」を絶対に加算しない
```

**実装上の要点**

| 方式 | バックグラウンドの時間を拾うか |
|---|---|
| `Time.deltaTime` の累積 | **拾わない**(バックグラウンド中は Update が呼ばれないため) |
| `Time.realtimeSinceStartup` の差分 | **拾ってしまう。この方式を使ってはならない** |
| `DateTime.Now` の差分 | **拾ってしまう。同上** |

> **`Time.deltaTime` 累積方式を推奨する。**
> ただし**復帰時に1フレームだけ巨大な `deltaTime` が来る**ことがあるため、
> **`Time.maximumDeltaTime` の設定、または復帰後1フレームを無視する処理**が要る(§6-2)。

**復帰時に `Overlay_Pause` を出すのは、ポーズ画面が既に出ていた場合も同じ**(二重に出さない)。

### 4.4 他のオーバーレイとの競合(★既存文書に定義がなかった)

共通仕様 §2.2 は「オーバーレイ同士は重ねない。優先度の高いものが出るとき低いものは閉じてから出す」
と定めているが、**閉じたオーバーレイをどう復元するかが未定義だった。**

| 閉じられたもの | 復帰時の扱い |
|---|---|
| `Overlay_Countdown` | **カウントダウンを最初からやり直す**(短縮版1.5秒)。`30_Overlay_Countdown.md` §4.3。**★2026-09-17 実装は簡略化されており、仕様と差がある(`30_Overlay_Countdown.md` §6.4 / 本書 §7.3-2)** |
| **`Overlay_MicroGameIntro`** | **「つづける」の後に、もう一度表示する。**T1 は停止したまま。**まだ読んでいないので初出フラグを立ててはならない**(§5-4 / `32_Overlay_MicroGameIntro.md` §4.4)。**★2026-09-17: 再表示のコード経路は実装済み。ただし `Overlay_MicroGameIntro` 自体が未実装なので、実際の見た目・操作は未確認**(§7.3-3) |

### 4.5 ★業務提示中・終了演出中の扱い(2026-09-15 追加)

| ポーズに入ったフェーズ | 扱い |
|---|---|
| **業務提示(1.5秒)** | ポーズボタンは押せないので**バックグラウンド移行のときだけ**起きる。§4.1 に加えて **`se_microgame_start` を停止**し、業務提示の演出(文字・集中線・揺れ)を初期状態に戻す。**問題を覆う `PromptBackdrop` は出したまま**(ポーズとカウントダウンの間に問題を先読みさせない)。「つづける」→ カウントダウン1.5秒 → **業務提示を最初からやり直す**(`10_Screen_GamePlay.md` §12.7) |
| **初出カード** | 従来どおり(§4.4)。**`PromptBackdrop` は出したまま** |
| **終了演出「しゅうりょう!」(1.5秒)** | **ポーズに入らない。この画面を出さない。**バックグラウンド移行時は未保存なら即保存し、復帰したら結果画面へ直行する(`10_Screen_GamePlay.md` §13.6)。**結果が確定した後に「やめる」(記録しない中断)を選べてしまう矛盾を避けるため** |

---

## 5. 未確定事項

| # | 内容 | 影響 | 判断者 |
|---|---|---|---|
| **1** | **「はじめから」を置くか。** 既存文書(企画書 §3.2 / HTML §6-2)は `Overlay_Pause` の役割を「一時停止・再開・中断」としており、**「はじめから」は planner が追加したもの** | 選択肢が3つになると誤タップが増える。**プランナー推奨: 置く**(スコアが悪いとき即やり直せるのはリプレイ性に効く)。**★2026-09-17: 推奨どおり「置く」で実装済み(ディレクターの確定は未)** | ディレクター |
| **2** ★ | **「やめる」(と「はじめから」)に確認を挟むか。** 誤タップで90秒のプレイが消える | `Overlay_Confirm` を流用できる。**プランナー推奨: 「やめる」は挟む。「はじめから」は当面挟まない**(すぐやり直せることに価値があるため。テストプレイで誤タップが見られたら挟む)。**★2026-09-17 実装: `Overlay_Confirm` が未実装のため、両方とも確認なしで即実行する(暫定)。**挟む場合は `PauseOverlayController` の「はじめから」「やめる」の処理の先頭に `Overlay_Confirm` を呼ぶ形になる(`Overlay_Confirm` は設定画面の「データを けす」でも必要) | ディレクター |
| **3** | **ポーズ画面にスコア・残り時間を表示しない**(プランナー決定)。`Blocker` で背後も隠す | 表示すると「ポーズして状況確認」が戦略になり公平性が崩れる | ディレクター |
| **4** | **`Overlay_MicroGameIntro` を閉じた場合、復帰後に再表示する**(プランナー決定・新規) | 再表示しないと、**その種目のルールを一度も見ないまま本編が進む** | ディレクター |
| **5** | **ポーズ中にバナー広告を表示するか。** 本書は**非表示のまま**とした(ゲームプレイ中の設定を引き継ぐ) | 表示すると**ポーズ画面のボタンの近くにバナーが出て誤タップリスクが上がる** | ディレクター |
| 6 | 「きゅうけい ちゅう」という文言(教育漢字 + 分かち書き) | UI文言 | ディレクター |
| 7 | ポーズ中に BGM を止めるか、音量を下げるだけにするか | **プランナー推奨: 止める**(通知や電話と重なるため)。**★2026-09-17 実装: 止める(`AudioManager.PauseAll`)** | ディレクター |
| **8** ★ | **ボタンの色・文字サイズ(仮の値)**: 「つづける」は緑系、「はじめから」「やめる」は灰系。見出し 64 / ボタンの文字 48(§7.2) | 見た目・読みやすさ。**レイアウト値・フォントサイズはディレクターの所有物** | ディレクター(**推奨: 実機で読めるか確認。ボタン画像 IMG-UI-01 が入るまではこのままでよい**) |
| **9** ★ | **`[Countdown]` 中の自動ポーズから「つづける」したときの簡略実装**(`30_Overlay_Countdown.md` §6.4) | 短縮カウントダウンの後に、表示の無い待ちが入る | ディレクター(**推奨: 30 §8-11 を参照**) |

---

## 6. 開発チームの技術判断が必要な項目

| # | 内容 |
|---|---|
| ~~PA-1~~ | ~~`Time.timeScale = 0` を使うか、自前で止めるか~~ **★実装済み(2026-09-17): 自前で止める**(`timeScale` は使わない) |
| ~~PA-2~~ | ~~バックグラウンド復帰時に巨大な `deltaTime` が1フレーム来る問題~~ **★実装済み: 1フレームで進める時間を 0.25秒までに制限**(`Time.unscaledDeltaTime` を上限付きで加算。2026-09-08 の実-2 の対策と同じ) |
| **PA-3** | **`OnApplicationPause` と `OnApplicationFocus` のどちらを使うか。** iOSでの発火タイミングの違い(コントロールセンターを引き下げただけのときの挙動など)。**★実装: 両方を使い、どちらでも同じ `RequestPause` 相当の処理に入る(二重表示は防止)。iOS 実機での挙動は未確認** |
| **PA-4** | **アプリがOSに強制終了された場合**、セッションは失われる。**MVPでは復元しない**でよいか(復元するならセッション状態の永続化が必要) |
| **PA-5** | **ミニゲームの `SetPaused(true)` で止め漏れが無いこと。** 特にM5(ベルト・箱の移動・ループSE)が4種で最も複雑 |
| ~~PA-6~~ | ~~`Overlay_Pause` 表示中に再度バックグラウンドへ行った場合、二重に処理が走らないこと~~ **★実装済み: 表示中フラグで二重表示を防止**(テスト `RequestPause_WhileAlreadyPauseOverlayIsVisible_DoesNotShowOverlayTwice`) |
| **PA-7** ★ | **「つづける」の短縮カウントダウン中に、もう一度バックグラウンドへ移行した場合。**コードを読む限り、この間は「`Overlay_Pause` が出ている」扱いのままなので `Overlay_Pause` は出ず、**カウントダウンはポーズの影響を受けずに進む**(アプリが止まっている間は進まないが、Editor でフォーカスを外しただけの場合などは、見ていない間に再開まで進みうる)。**開発チームに確認が必要**(本来は「カウントダウンを止めて `Overlay_Pause` を出し直す」のが §4.3 の趣旨) |
| **PA-8** ★ | **ボタンの実際の配線(`PauseOverlayController` → `GameSessionController`)は自動テストの範囲外**(テストは `GameSessionController` の呼び分けだけを検証している)。**Editor / 実機で3ボタンを実際に押して確認すること** |

---

## 7. ★実装の現状(2026-09-17)

### 7.1 実装済みのもの

| 項目 | 内容 |
|---|---|
| Prefab | `Assets/Prefabs/UI/Overlays/Overlay_Pause.prefab`(**§2 / §2.1 の初期値どおり**。`Blocker` alpha 0.75 / `Panel` 900×900 / `TitleText` / `ResumeButton` / `RestartButton` / `QuitButton`) |
| スクリプト | `PauseOverlayRefs` / `PauseOverlayController`(3ボタンの配線と `se_button`) |
| 登録 | `Main.unity` の `ScreenFlowManager.overlayPrefabs` に登録(`OverlayId.Pause` = 1) |
| `GameSessionController.RequestPause()` | **手動・自動ポーズの共通の入口**(§1.1 / §4.1)。`[Idle]` / `[Finish]` 以外で `SetPaused(true)` → `ShowOverlay(Pause)`。二重表示を防止 |
| `GameSessionController.ResumeFromPause()` | 「つづける」(§4.2 の実装表) |
| `PauseButtonController`(HUD) | **トグル式をやめ、`RequestPause()` を呼ぶだけ。**再開は `Overlay_Pause` の「つづける」だけが担う |
| HUD `PauseButton` の活性 | `Phase == Play && !IsPaused` |
| `CountdownOverlayController` | 表示条件を `Phase == Countdown` から **`CountdownStepIndex >= 0`** に変更(復帰時の短縮カウントダウンは `Phase` が Play / Prompt / Intro のまま出るため。`30_Overlay_Countdown.md` §4.3) |
| テスト | `[Countdown]` / `[Prompt]` / `[Play]` 中のポーズ → 「つづける」でセッションが完走する / 二重表示しない / 「はじめから」で状態が初期化される / 「やめる」で Title へ行き記録しない / `[Finish]` 中はポーズしない。**EditMode 117件・PlayMode 33件すべて成功** |

**これで `30_Overlay_Countdown.md` §7-5(`[Countdown]` / `[Prompt]` / `[Intro]` 中に裏へ回すと再開できない)は解消した。**

### 7.2 仮の値(ディレクターの所有物。§5-8)

| 要素 | 仮の値 |
|---|---|
| `ResumeButton` の色 | 緑系 |
| `RestartButton` / `QuitButton` の色 | 灰系 |
| `TitleText` のフォントサイズ | 64 |
| ボタンの文字のフォントサイズ | 48 |

> 仕様書は色・フォントサイズを指定していなかった。**以後 developer はこれらを書き換えない**(CLAUDE.md「UIレイアウトの取り扱い」)。

### 7.3 仕様との差(ディレクター確認待ち・暫定)

| # | 差 | 仕様 | 実装 | 記載先 |
|---|---|---|---|---|
| 1 | **確認ダイアログ** | §5-2(プランナー推奨: 「やめる」に確認を挟む) | **`Overlay_Confirm` が未実装のため、「はじめから」「やめる」とも確認なしで即実行** | §5-2 |
| 2 | **`[Countdown]` 中の自動ポーズからの復帰** | カウントダウンを Short 1.5秒で最初からやり直し、終わったら 1本目へ(`30_Overlay_Countdown.md` §6.2) | **短縮カウントダウンの後に、中断していた元のカウントダウンの残りが「表示も音も無い待ち」として続く**(developer 報告は「最大1ステップ分」。プランナーがコードを読んだ限りでは、もっと長くなりうる。30 §6.4)。起きるのは裏へ回したときだけ | `30_Overlay_Countdown.md` §6.4 / §8-11 |
| 3 | **`[Intro]` 中の自動ポーズからの復帰** | 「つづける」→ 1.5秒 → 初出カードを再表示(§4.4) | **コードの経路だけ用意。`Overlay_MicroGameIntro` が未実装なので見た目は未確認** | §4.4 / `32_Overlay_MicroGameIntro.md` |
