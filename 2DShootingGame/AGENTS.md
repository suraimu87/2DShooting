# AGENTS.md — AI 向けプロジェクト引き継ぎ

このファイルは、別 PC / 別チャットの AI が続きの作業をするときに必要な文脈です。  
詳細な授業用手順は `Shooting_BuildGuide.md` を正とします。

---

## 1. プロジェクト概要

| 項目 | 内容 |
|------|------|
| 種類 | Unity 2D シューティング（サンプル兼授業用） |
| シーン | `Assets/Scenes/Shooting.unity` |
| 対象読者 | **Unity / プログラム初心者の学生** |
| 言語 | コードコメント・手順書・ユーザー向け説明は **日本語** |
| 入力 | **Input System** のみ（`activeInputHandler: 1`）。旧 `Input.GetAxis` は使わない |

目的は「完成品」より **学生が追える段階的な実装サンプル** と **手順書** を揃えること。

---

## 2. 設計方針（必ず守る）

### 教える側の制約

1. **ScriptableObject はまだ使わない**（後の授業用に残す）。設定は Inspector の配列・public フィールド・enum で持つ。
2. 種類の分岐は **`enum` + `switch` + 種類ごとの関数**（弾・敵移動・アイテムで統一）。
3. コードは初心者向けに素直に。過度な抽象化・デザインパターンの押し付けはしない。
4. 追尾弾・敵の追尾移動は **発展課題**（完成コードを渡さない。ヒントのみ手順書に記載）。
5. 学生が Copilot 等で改修するときも **§6 の初心者ガード**（非同期禁止・日本語ログ・簡単案）を守る。

### 命名・既存のクセ

- `EnemyCreater` / `BulletCreater` は意図的な綴り（Creator ではない）。既存に合わせて維持する。
- スクリプトは主に `Assets/Scripts/`（`Shooting/` サブフォルダは手順書の名残で、実体は Scripts 直下）。

### Prefab / Tag

| Tag | 用途 |
|-----|------|
| `Player` | 自機 |
| `Enemy` | 敵 |
| `Bullet` | 自機弾 |
| `Item` | アイテム |

当たり判定は **Trigger（Is Trigger）+ Rigidbody2D Kinematic（Gravity 0）**。  
処理の担当: 弾×敵 → `Enemy`、自機×敵 → `Player`。

---

## 3. 現状のアーキテクチャ

```text
Player
  ├ InputSystem_Actions（既存アセット）… Move / Attack
  ├ BulletCreater.Shoot(currentBulletType)
  ├ ApplyItem(Item) … 移動・連射・武器
  └ 被弾 → gameOverObject.SetActive(true)

BulletCreater … switch(BulletType) → CreateStraight / Triple / Pierce
Bullet … 直進・pierce・距離で Destroy（ホーミング実装なし）

EnemyCreater … WaveInfo[] を順番消化
  └ 最終 Wave 後は敵 Tag が 0 になるまで待ってから GameClear
Enemy … switch(EnemyMoveType) → Straight / SideWays

Item … ItemType → Player.ApplyItem
GameOver / GameClear … Retry で Scene 再読込、表示時に Player.enabled = false
```

### 弾種 `BulletType`

- `Straight` … 1発・短いクールタイム  
- `Triple` … 正面＋斜め上下  
- `Pierce` … 貫通プレハブ  
- ~~Homing~~ … **未実装・発展課題**

### 敵移動 `EnemyMoveType`

- `Straight` / `SideWays`  
- ~~Homing~~ … **発展課題**

### Wave（`WaveInfo` は **struct**）

- ScriptableObject ではなく `EnemyCreater.waves[]`  
- Inspector 用に `[Header("■ …")]` 付き  
- クリア条件: 全 Wave 消化 **かつ** 画面上 `Enemy` が 0（最終 Wave は `waitUntilCleared` オフでも全滅待ち）

### アイテム `ItemType`

- `MoveSpeedUp` / `FireRateUp` / `WeaponChange`  
- 敵撃破時 `dropChance` でドロップ  

### UI

- Canvas 下の `GameOver` / `GameClear`（初期非アクティブ）  
- コントローラー: 表示時に `EventSystem.SetSelectedGameObject(retryButton)`  
- 表示時に `Player.enabled = false`（Input Action の OnDisable で Move/Attack を切る → UI と競合しにくくする）

---

## 4. 主要ファイル一覧

| ファイル | 役割 |
|----------|------|
| `Player.cs` | 移動・射撃指示・被弾・アイテム適用 |
| `BulletCreater.cs` / `BulletType.cs` / `Bullet.cs` | 弾の出し方・弾本体 |
| `EnemyCreater.cs` / `WaveInfo.cs` | Wave 制スポーン・Clear |
| `Enemy.cs` / `EnemyMoveType.cs` | 敵移動・被弾・ドロップ |
| `Item.cs` / `ItemType.cs` | アイテム |
| `GameOver.cs` / `GameClear.cs` | 結果 UI・Retry・Player 無効化 |
| `ScoreText.cs` / `SeAudioSource.cs` | スコア・SE |
| `Move.cs` | 汎用直進（弾以外向けの名残。弾は `Bullet`） |
| `Result.cs` | 旧結果用の可能性あり。現行は GameOver/Clear 側が主 |
| `InputSystem_Actions.inputactions` | 既存。新規 Input Actions を増やさない方針 |
| `Shooting_BuildGuide.md` | 学生向け手順書 |

プレハブ例: `Player`, `Bullet` / `Bullet_Pierce`, `Enemy1` / `Enemy2`, `Item_MoveSpeed` / `Item_FireRate` / `Item_WeaponChange`

---

## 5. 実装済み / 未着手

### 実装済み（サンプルとして動く想定）

- Input System で移動・攻撃  
- 弾 3 種（直進 / 3-way / 貫通）  
- 敵 2 移動パターン + Wave 制  
- Trigger 当たり判定  
- アイテム 3 種 + ドロップ  
- GameOver / GameClear + Retry（シーン再読込）  
- コントローラー UI 選択・Player 無効化  

### 手順書にあるが薄い／未実装寄り

- プレイヤー移動の慣性（acceleration / maxSpeed）  
- 背景スクロール・スプライトアニメ  
- 敵の弾  
- 残機制  
- オブジェクトプール  
- ScriptableObject 化（弾データ・Wave アセット）  

### 発展課題（答えコードを書かない）

- 追尾弾  
- 敵のプレイヤー追尾移動  

---

## 6. AI / GitHub Copilot 向けルール（初心者ガード）

学生が **GitHub Copilot** や Cursor などの AI で改修するとき、高度・難解なコードにならないように次を守る。  
（教員・TA が AI に頼むときも同じ。）

### 6-1. 必守（ユーザー指定）

| ルール | 内容 |
|--------|------|
| **非同期処理を行わない** | `async` / `await`、`Task`、`Coroutine` の乱用、UniTask 等は使わない。待ちは `Update` のタイマーや `bool` フラグで書く |
| **エラーは日本語の Debug ログ** | 想定外や未設定は `Debug.LogWarning` / `Debug.LogError` で **日本語** の文を出す（英語メッセージや例外だけ投げて終わりにしない） |
| **初心者向けコメント** | 「何をしているか」が分かる日本語コメントを処理の区切りに書く。省略しすぎない |
| **難しい要求は簡単案を提案** | 高難度の仕様・アルゴリズムを求められたら、まず **簡単な代替案** を示してから進める（無理に高度実装しない） |

### 6-2. 使ってよい基本（授業範囲）

- `MonoBehaviour` の `Awake` / `Start` / `Update` / `OnTriggerEnter2D` / `OnEnable` / `OnDisable`
- `public` / `[SerializeField]` と Inspector 割り当て
- `enum` + `switch`、通常の `if`、`for`、配列
- `Instantiate` / `Destroy` / `SetActive` / `CompareTag`
- `FindObjectOfType`（少数・Start でキャッシュ推奨）
- Input System の Action 読み取り（既存 `InputSystem_Actions`）

### 6-3. 避けてほしい高度な内容（例）

学生が「それっぽく」書かれがちなもの。**求められても簡単案に置き換える。**

| 避ける | 代わりに提案する例 |
|--------|-------------------|
| `async` / `await` / `Task` | `Update` で秒数を減らす、フラグで状態を分ける |
| `Coroutine`（`StartCoroutine`） | 同上（クールタイム・待ち時間は float タイマー） |
| ScriptableObject | `public` フィールド、配列、`enum`（授業後半まで保留） |
| インターフェース・複雑な継承 | 1 クラスに処理を書く、または既存の switch に case を足す |
| ジェネリクス多用・LINQ | `for` ループ |
| イベント / Action・デリゲートの多用 | 直接メソッド呼び出し |
| オブジェクトプール（未学習時） | 当面は `Instantiate` / `Destroy` |
| DOTween 等の外部アセット（未導入時） | `transform.position` を毎フレーム動かす |
| シングルトンの複雑な GameManager | 必要な参照を Inspector で渡す |
| try-catch で握りつぶす | 事前に null チェック＋日本語 `Debug.LogWarning` |

### 6-4. コードの書き方（Unity 初心者向け）

1. **1 つの関数は 1 つの仕事**（長くなったらコメントで区切るか、関数に分ける）。  
2. **マジックナンバー**は可能な範囲で `public` 変数や定数にして Inspector で触れるようにする。  
3. **null のとき**は落ちる前に日本語ログを出して `return`（例: プレハブ未設定）。  
4. **名前は分かりやすく**（`tmp`、`a`、`data2` を避ける）。既存の綴り（`EnemyCreater` 等）は合わせる。  
5. **英語だけのコメントや変数名だらけの説明**にしない。学生向け説明は日本語。  
6. **既存パターンを真似る**（弾・敵・アイテムはどれも enum + switch）。新パターンを勝手に増やさない。  
7. **「動く最小」を先に出す**。最適化・綺麗な設計は後回し。  

### 6-5. 難しい要求への答え方（例）

学生や依頼が高度なとき:

1. 「そのままだと〇〇が必要で難しい」と短く伝える  
2. **簡単な実現案**を 1〜2 個出す  
3. 簡単な案でよいか確認してから実装する  

例:

- 「追尾弾が欲しい」→ まず直進のまま弾を増やす／角度を変える案を出す（追尾は発展課題）  
- 「ネット対戦」→ この授業範囲外。ローカル 2P やスコア競いなど別案を出す  
- 「綺麗に非同期で読み込み」→ シーンは今のまま。必要なら最初から全部読み込む  

### 6-6. 一般の AI 作業ルール

1. **ユーザー向け返答は日本語。**  
2. 手順書・コメントも日本語で統一。  
3. 依頼範囲以外のリファクタや無関係ファイルを触らない。  
4. 初心者向けを崩す変更（いきなり SO 全面移行、複雑な継承階層など）は、ユーザーが明示しない限り避ける。  
5. コミットはユーザーが依頼したときだけ。  
6. 授業用なので「動く最小」→「種類を増やす」の順を優先。  
7. 迷ったら `Shooting_BuildGuide.md` とこの `AGENTS.md` を更新して方針を残す。  
8. Copilot / Cursor に渡すプロンプトにも、上記「非同期禁止・日本語ログ・コメント・簡単案」を書いてもらうと安全。  

---

## 7. よくある落とし穴

| 症状 | 確認点 |
|------|--------|
| Trigger が発火しない | 両方 Collider、どちらか Rigidbody2D、Is Trigger |
| コントローラーで UI が押せない | 表示時に Selected = RetryButton、EventSystem + Input System UI Module |
| 押しっぱなしで UI が反応しない | GameOver/Clear 表示時に `Player.enabled = false` |
| 敵が残っているのに Clear | 最終 Wave 後は敵 0 待ち（`waitingForGameClear`） |
| 弾が当たらない | Tag `Bullet` / `Enemy`、敵に Collider+RB |

---

## 8. 会話で決まってきた方針メモ

- Input Actions はテンプレの `InputSystem_Actions` を流用（新規作成しない）。  
- 射撃は当面 `Attack` を Fire 相当として使用。`FireAuto` は後回し可。  
- 弾種管理は「生成側 switch」＋「プレハブ側 pierce 等」。  
- Wave は時刻表より **配列の順番消化** の方が初心者向け、と採用。  
- GameClear = 最終 Wave を出し切り、かつ敵全滅、かつ未被弾。  

---

## 9. 学生が Copilot に貼る用・短いプロンプト例

```text
このプロジェクトは Unity 初心者向けです。
・async/await・Coroutine・Task は使わない（Update のタイマーで待つ）
・エラーは Debug.LogWarning / LogError で日本語
・処理に日本語コメントを付ける
・難しいことは簡単な方法に言い換えて提案してから実装する
・ScriptableObject・LINQ・複雑な継承は使わない
・既存の enum + switch の書き方に合わせる
・AGENTS.md と .github/copilot-instructions.md の方針に従う
```

---

## 10. GitHub Copilot でプロジェクト内ファイルを参照するには

Copilot は Cursor と違い、**自動ですべてのファイルを読むとは限らない**。次で参照できるようにする。

### リポジトリ側（用意済み）

| ファイル | 効果 |
|----------|------|
| `.github/copilot-instructions.md` | Copilot Chat がこのワークスペースの方針を自動参照しやすい |
| `AGENTS.md` | 詳細な引き継ぎ。必要ならチャットで「AGENTS.md を読んで」と指示 |

### 学生・PC 側の操作（VS Code / Visual Studio）

1. **プロジェクトのルート**（`2DShootingGame` フォルダ）を開く（親フォルダだけ開かない）  
2. 拡張機能 **GitHub Copilot** と **GitHub Copilot Chat** を入れる・サインインする  
3. チャットでファイルを明示する  

| やり方 | 例 |
|--------|-----|
| ファイルを添付 / `#` | `#Player.cs` やチャットの 📎 で `Assets/Scripts/Player.cs` |
| ワークスペース全体 | `@workspace`（または同等の「コードベース」指定）で質問する |
| 開いているファイル | 編集中のスクリプトを開いたままチャットする |

4. 設定（VS Code の例）  

- `github.copilot.chat.codeGeneration.useInstructionFiles`: **オン**  
- 応答の **References** に `copilot-instructions.md` が出るか確認  

5. Unity の `Library/` などは巨大なので、質問は「`Assets/Scripts` の Player を見て」のように **パスを絞る** と参照しやすい。  

### できない・限界

- 補完（灰色の提案）はチャットほどプロジェクト全体を検索しない  
- ネット未接続・組織の Copilot 制限・古い拡張だと `@workspace` が弱いことがある  
- **Cursor** を使う場合は `AGENTS.md` と `.cursor/rules/` が主（Copilot 用 `.github` とは別系統）  

---

*最終更新の目安: 2026-07 — Copilot 向け `.github/copilot-instructions.md` と参照手順を追加。*
