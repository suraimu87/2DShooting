# 2Dシューティング 実装手順書

既存の `Shooting` シーン・スクリプトをベースに、機能を段階的に拡張する手順です。  
**先に動く最小構成 → あとから種類・演出・最適化** の順で進めます。

---

## 前提・ベ
- パッケージ：`Input System`（導入済み）
- ベース資産：`Assets/Scripts/Shooting/`、`Prefabs/Shooting/`、`Scenes/Shooting`
- 既存の動きの対応表（拡張時の置き換え先）

| 既存 | 拡張後のイメージ |
|------|------------------|
| `Input.GetAxis` / `GetKey` | Input System（Action） |
| `moveSpeed` 定数移動 | 加速度・最高速度・慣性 |
| `Instantiate` / `Destroy` | オブジェクトプール |
| `EnemyCreater` の定間隔ランダム | Wave テーブル（秒・位置・プレハブ） |
| 弾1種・直進のみ | `BulletType` + `BulletCreater` の switch（直進／3方向／貫通） |

---

## 推奨実装順

1. Input System（キーボード＋コントローラー）
2. プレイヤー移動（慣性）＋画面制限
3. 弾の発射（単発／連射ボタン）＋弾プール
4. 弾の種類（`BulletType` + switch → 種類ごとの生成関数）
5. 背景スクロール
6. スプライトアニメーション
7. 敵の移動バリエーション
8. 敵の弾
9. Wave 制
10. アイテム（移動／連射／弾種）
11. 残機・UI
12. 敵プール・負荷調整

途中で「遊ぶ・調整する」時間を挟むと、パラメータ設計がブレにくいです。

---

## 1. コントローラー対応（Input System）

### やること

- キーボード・ゲームパッドを同一の Action で扱う
- 移動は `Vector2`、射撃は `Button`
- **新規の Input Actions は作らず**、プロジェクトに最初からある `InputSystem_Actions` を使う

### 使い方（既存アセット）

1. Project ウィンドウの `Assets/InputSystem_Actions` を開く  
   （中身に `Player/Move`・`Player/Attack` などがある。別ファイルではなく、同じアセットの Action）
2. 今使うのは次のとおり（それ以外の Jump などは無視してよい）

| Action | Type | バインド例 | 用途 |
|--------|------|------------|------|
| `Move` | Value / Vector2 | WASD・矢印、左スティック | 自機の移動 |
| `Attack` | Button | Space・Enter・マウス左、パッド | 弾の発射（手順書の `Fire` 相当） |

3. あとから連射用が必要なら、同じ `Player` マップに `FireAuto`（Button）を追加する
4. `Player` スクリプトに `InputSystem_Actions` を割り当て、コードで読む

```csharp
// Awake で Action を探す
moveAction = inputActions.FindAction("Player/Move", throwIfNotFound: true);
attackAction = inputActions.FindAction("Player/Attack", throwIfNotFound: true);

// OnEnable / OnDisable で Enable・Disable

// Update で読む
Vector2 input = moveAction.ReadValue<Vector2>();
if (input.sqrMagnitude > 1f) input.Normalize();

bool isFiring = attackAction.IsPressed();
```

（旧 `Input.GetAxis` / `GetKey` は置き換え）

### Inspector での割り当て（学生向け）

1. Hierarchy の `Player` を選択
2. Inspector の **Input Actions** 欄に、Project の `InputSystem_Actions` をドラッグ＆ドロップ

### 確認ポイント

- スティックのデッドゾーン（小さな揺れで動かない）
- キーボード斜め移動は入力を `normalized` すると速くなりすぎない
- Game ビューにフォーカスがないと入力が取れないことがある
- Play 中に WASD／矢印で自機が動くこと

### 補足

- Hierarchy に `PlayerInput` コンポーネントを付ける方法でもよい（行為でイベント）
- スクリプト直読み（`FindAction` + `ReadValue`）の方が「弾・慣性」との相性が単純で追いやすい
- `Generate C# Class` は使わなくても動く（既存アセット＋`FindAction`で十分）

---

## 2. 背景スクロール

### やること

- 画面裏に背景スプライトを並べ、一定速度で流す
- 必要なら **遠景／中景／近景** で速度を変える（パララックス）

### 作り方

1. 背景用画像を横（または縦）に継ぎ目なく並べられるサイズにする
2. 同種スプライトを2枚以上配置し、流れたら **反対側へワープ** する
3. `BackgroundScroller` のようなスクリプトで

```text
position += scrollDirection * scrollSpeed * Time.deltaTime
画面外に出たら、もう一方の背景の先へ移動
```

4. Sorting Layer / Order in Layer でキャラより後ろにする

### パラメータ例

| 項目 | 例 |
|------|-----|
| 遠景速度 | 0.5 |
| 中景速度 | 1.5 |
| 近景速度 | 3.0 |
| 流す方向 | 左（横スクロール）or 下（縦スクロール） |

### 確認ポイント

- 継ぎ目が見えないか（画像端の合わせ）
- カメラ Orthographic Size と背景幅の関係

---

## 3. プレイヤー

### 3-1. パラメータ設計

Inspector で触れる公開パラメータを先に決めると実装が安定します。

**移動**

| パラメータ | 意味 |
|------------|------|
| `acceleration` | 入力方向への加速度 |
| `maxSpeed` | 最高速度 |
| `drag` / `deceleration` | 入力なし時の減衰（慣性） |
| `limitX` / `limitY` | 画面内クランプ（既存踏襲） |

**弾・残機**

| パラメータ | 意味 |
|------------|------|
| `currentBulletType` | 今の弾種（`BulletType` enum） |
| `maxBulletsOnScreen` | 同時に出せる弾数（連射上限） |
| `fireInterval` | 弾と弾の最小間隔 |
| `life` / `maxLife` | 残機 |

既存 `Player.cs` の `moveSpeed` 一発移動を、速度ベクトル保持型に差し替えるイメージです。

### 3-2. 移動（ベクトル＋慣性）

### やること

- コントローラー入力 → 移動方向ベクトル
- 加速度で速度を増やし、最高速度でキャップ
- 入力ゼロでもすぐ止まらず減衰する

### 作り方（疑似コード）

```csharp
Vector2 input = moveAction.ReadValue<Vector2>();
if (input.sqrMagnitude > 1f) input.Normalize();

if (input.sqrMagnitude > 0.01f)
    velocity += input * acceleration * Time.deltaTime;
else
    velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration * Time.deltaTime);

if (velocity.magnitude > maxSpeed)
    velocity = velocity.normalized * maxSpeed;

transform.position += (Vector3)(velocity * Time.deltaTime);
// その後 Clamp（既存と同様）
```

### 確認ポイント

- 最高速度到達までの「のっそり感」が心地よいか
- 画面端で速度をリセットするか、そのままクランプだけにするか

### 3-3. スプライトアニメーション

### やること

- 待機ループ
- 移動中の見た目切り替え

### 作り方

1. `Animator` + Animation Clip（Idle / Move）  
   または Sprite 配列を `time` で切り替える簡易アニメ
2. 速度の大きさで分岐

```text
velocity.magnitude > threshold → Move
それ以外 → Idle
```

3. 左右反転が必要なら `SpriteRenderer.flipX`（または scale.x）

### 確認ポイント

- 微小なスティック入力でチカチカしないよう threshold を設ける

### 3-4. 弾の発射

### やること

- `Fire`：押した瞬間／押し続けで発射（設計で決める）
- `FireAuto`：連射専用（押しっぱなしで `fireInterval` ごとに撃つ）

### 作り方

1. クールタイム `fireTimer`（既存ロジック流用）
2. **画面上の弾数** が `maxBulletsOnScreen` 未満のときだけ生成
3. 生成は最終的にプール経由（後述）にする。最初は `Instantiate` でも可

### ボタン分担の例

| ボタン | 挙動 |
|--------|------|
| Fire | 単発（または半自動） |
| FireAuto | 連射（間隔は `fireInterval`） |

### 3-5. パワーアップ（プレイヤー側）

| 種類 | パラメータへの効き方 |
|------|----------------------|
| 移動アップ | `acceleration` / `maxSpeed` を加算 or 倍率 |
| 連射アップ | `maxBulletsOnScreen` 増加、必要なら `fireInterval` 短縮 |

上限値（キャップ）を決めておくと、取りすぎで壊れにくいです。

---

## 4. 弾の種類

### 4-1. 授業向けの管理方法（enum + switch）

ScriptableObject は後回しにし、次の流れで種類を増やします。

```text
Player（currentBulletType を保持）
  → ショットボタンで BulletCreater.Shoot(currentBulletType)
    → switch で生成関数を切り替え
      → 各プレハブを Instantiate
        → Bullet.Setup(向き)
```

| BulletType | 生成内容 | クールタイムの目安 |
|------------|----------|-------------------|
| `Straight` | 直進プレハブを**1発** | 短い（すぐ撃てる） |
| `Triple` | 直進プレハブを**3方向**（正面・斜め上・斜め下） | 少し長い |
| `Pierce` | 貫通プレハブを1発 | 普通 |

- 速度・貫通は **各プレハブの Inspector** で設定する
- クールタイムは `BulletCreater.Shoot` の戻り値で Player に渡し、種類ごとに変えられる
- 種類を増やすとき：enum に足す → `switch` に case を足す → 専用の `Create○○` 関数を書く

#### 発展課題（プログラマー志望向け・答えは渡さない）

**追尾弾（ホーミング）を自分で追加してみよう。**  
授業の完成形には含めない。ヒントだけ記す。

1. `BulletType` に種類を足す  
2. `BulletCreater` の `switch` に case と生成関数を足す  
3. 追尾用プレハブを用意する（`Bullet` に「敵へ向きを寄せる」処理を足す）  
4. 近い敵の探し方・旋回の速さ制限を考える  

（完成コードは配布しない。考えて実装するのが目的）

（発展その2：あとから ScriptableObject の `BulletData` に置き換えても、`Shoot` の入り口は同じにできる）

### 4-2. 挙動バリエーション

| 種類 | 作り方の要点 |
|------|----------------|
| **直進1発** | `CreateStraightBullet`：正面向きで1つ生成 |
| **3方向** | `CreateTripleBullets`：角度 0 / +20 / -20 など |
| **貫通** | 貫通プレハブで `pierce = true`。敵ヒットでも弾を消さない |
| **追尾** | （発展課題）近い敵へ向きを寄せる。実装は各自 |

### 4-3. 弾スクリプトの責務（実装済みの役割）

| スクリプト | 役割 |
|------------|------|
| `BulletType` | ショット種類の enum |
| `BulletCreater` | `switch` で出し方を切り替え（種類ごとの生成関数） |
| `Bullet` | 1発の移動・距離で消滅（`pierce` はプレハブ設定） |
| `Player` | いつ撃つか・`currentBulletType` を渡す → `Shoot(type)` |

既存 `Move.cs` は汎用直進用。弾は `Bullet` を使う。

敵ヒットは従来どおり `Enemy` が `Bullet` タグを検知。貫通時は `bullet.pierce` なら弾を消さない。

敵の弾とタグ／レイヤーを分ける（例：`PlayerBullet` / `EnemyBullet`）場合は、この構成のままでプレハブを分ける。

---

## 4-4. 当たり判定（Player / Enemy / Bullet）

シューティングでは **物理でぶつかって跳ね返る** のではなく、**触れたら処理する（Trigger）** を使います。

### 必要なコンポーネント（各プレハブ）

| オブジェクト | Tag | Collider2D | Rigidbody2D |
|--------------|-----|------------|-------------|
| Player | `Player` | Circle（**Is Trigger オン**） | Kinematic、Gravity Scale = 0 |
| Enemy | `Enemy` | Circle（**Is Trigger オン**） | Kinematic、Gravity Scale = 0 |
| Bullet | `Bullet` | Circle（**Is Trigger オン**） | Kinematic、Gravity Scale = 0 |

ポイント（学生に必ず伝えること）：

1. **両方に Collider2D** がないと当たらない  
2. **どちらか一方に Rigidbody2D** がないと Trigger が発火しない（両方付けておくと安全）  
3. **Is Trigger** をオンにする（オフだと物理衝突になり、スクリプトは `OnCollisionEnter2D` になる）  
4. Tag で「誰と当たったか」を判別する  

### 誰が・何を処理するか

| 当たった相手 | 処理するスクリプト | 内容 |
|--------------|-------------------|------|
| Enemy × Bullet | `Enemy.OnTriggerEnter2D` | スコア加算・敵撃破・弾削除（貫通なら残す） |
| Player × Enemy | `Player.OnTriggerEnter2D` | 被弾（操作停止・SE・スプライト変更） |

```csharp
// Enemy 側（弾を受けたとき）
void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Bullet")) { /* 撃破処理 */ }
}

// Player 側（敵に当たったとき）
void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Enemy")) { /* 被弾処理 */ }
}
```

### 確認ポイント

- Scene ビューで Collider の緑円がスプライトより少し小さめか（大きすぎると理不尽）  
- 自機の弾が自機に当たらないこと（Player は `Enemy` タグだけ見るので通常は問題にならない）  
- 敵プレハブに Collider / Rigidbody を付け忘れていないか  

---

## 5. Wave 制（設計）

### 初心者向けに選ぶ方針

Wave の作り方は大きく2つあります。

| 方式 | 内容 | むずかしさ | 向いている人 |
|------|------|------------|--------------|
| **A. 時刻表** | 「開始から5秒に敵A、8秒に敵B…」 | 中〜高 | 細かい演出を時間で決めたいとき |
| **B. 順番消化（推奨）** | Wave1 → 出し切る →（全滅待ち）→ Wave2 | **低** | Unity / プログラム初心者 |

**授業・サンプルでは B を採用します。**

理由：

- 「配列を上から順に見る」だけで理解できる  
- ScriptableObject や複雑な時刻計算が不要  
- Inspector で Wave を増やすだけ難易度を上げられる  
- 弾の `BulletType` + switch と同じく、「データ（WaveInfo）と処理（EnemyCreater）を分ける」練習になる  

### 全体像

```text
EnemyCreater
  └ waves[] … WaveInfo の配列（Inspector で並べる）
       ├ Wave 1: 敵A × 4体 / 直進 / 1秒おき
       ├ Wave 2: 敵B × 6体 / 左右 / 0.8秒おき
       └ Wave 3: …

処理の流れ（1 Wave）:
  まだ出す → spawnInterval ごとに 1体 Instantiate
  出し切った →（任意）画面の敵が0体になるまで待つ
  → waitAfterSpawn 秒待つ → 次の Wave
  全部終わった → Clear
```

### WaveInfo（1 Wave の設定・構造体）

Inspector では `[Header]` で次のようにグループ分けされます。

| Header | 項目 | 意味 | 例 |
|--------|------|------|----|
| ■ 名前 | `waveName` | UI・ログ用の名前 | Wave 1 |
| ■ 出す敵 | `enemyPrefab` | 出す敵プレハブ | Enemy1 |
| | `enemyCount` | 何体出すか | 5 |
| | `spawnInterval` | 何秒おきに1体か | 1.0 |
| ■ 動き・出現 | `moveType` | Straight / SideWays | Straight |
| | `spawnSide` | 出現方向（-1=ランダム） | 1（右） |
| ■ 次の Wave への区切り | `waitAfterSpawn` | 次 Wave までの待ち秒 | 2 |
| | `waitUntilCleared` | 全滅してから待つか | true |

### やらないこと（今は）

- ScriptableObject の Wave アセット（後の授業で差し替え可能）  
- 「絶対時刻で一斉出現」のタイムライン  
- 1 Wave の中に複数種類の敵を混ぜる（必要なら Wave を分けるか、発展課題）  

### 発展課題（プログラマー志望向け）

1. 1つの Wave の中で敵プレハブを2種類混ぜる  
2. Wave クリア時に UI へ「Wave 2!」と出す  
3. 全 Wave クリアでリザルト画面へ遷移する  

### Inspector での作り方

1. Hierarchy の `EnemyCreater` を選ぶ  
2. **Waves** の Size を Wave 数にする  
3. 各要素にプレハブ・数・間隔・moveType を入れる  
4. Play して Console の「Wave 開始」ログと出現を確認する  

---

## 6. アイテム

### やること

敵撃破時に確率ドロップし、取るとプレイヤーを強化する。  
弾・敵と同じく **enum + switch** で種類を分ける（ScriptableObject は使わない）。

| ItemType | 効果 | プレハブ例 |
|----------|------|------------|
| `MoveSpeedUp` | `moveSpeed` を加算（上限あり） | 緑 `Item_MoveSpeed` |
| `FireRateUp` | `fireCoolTimeScale` を下げる（連射が速くなる） | 黄 `Item_FireRate` |
| `WeaponChange` | `currentBulletType` を指定の弾種に変更 | 青 `Item_WeaponChange` |

### 全体像

```text
敵撃破 → 確率で Item プレハブを Instantiate
  → 左へゆっくり流れる
  → Player に触れる
    → Player.ApplyItem(item) の switch で効果
    → Item を Destroy
```

### 作り方

1. `ItemType` enum を定義する  
2. `Item` スクリプト（種類・パラメータ・移動・Trigger）  
3. 種類ごとにプレハブを作り、色や `itemType` / `weaponType` を変える  
4. `Player.ApplyItem` で switch  
5. `Enemy` の `dropItemPrefabs` と `dropChance` で撃破ドロップ  

### 連射の考え方（初心者向け）

- `BulletCreater.Shoot` は「基本のクールタイム」を返す  
- Player 側で `fireTimer = 基本 × fireCoolTimeScale`  
- 連射アイテムは倍率を下げる（例: 1.0 → 0.85 → … → 下限 0.35）  

### 確認ポイント

- Tag `Item` / `Player`、両方 Trigger + Rigidbody2D  
- 連取しても `maxMoveSpeed` / `minFireCoolTimeScale` で壊れない  
- 武器チェンジ後も画面に残った旧弾はそのままでよい  
- 撃破直後にアイテムが出ること  

### 発展課題

- 効果に時間制限（数秒で元に戻る）を付ける  
- Wave クリア報酬として必ず1つ出す  

---

## 7. 敵

既存 `Enemy` の「一定方向移動」をベースに、**移動モード**を足す。  
弾と同じく **enum + switch + 種類ごとの関数** で管理する（ScriptableObject は後回し）。

| モード | 動き | 実装 |
|--------|------|------|
| `Straight` | 一直線 | `MoveStraight()` |
| `SideWays` | 進みながら横に往復 | `MoveSideWays()`（`sin` + 振幅） |
| 追尾 | プレイヤーへ近づく | **発展課題（完成コードは渡さない）** |

### 作り方の方針

```text
EnemyCreater（出現・moveType の割り当て）
  → Enemy.moveType
    → switch で MoveStraight / MoveSideWays
```

- `enum EnemyMoveType { Straight, SideWays }`
- 左右移動：進行方向に垂直な軸で `sin` すると、右から来る敵は上下に揺れる
- `EnemyCreater` の `randomizeMoveType` で 2 パターンを混ぜて出せる
- プレハブごとに `moveType` を固定してもよい（同じ見た目・違う動き）

#### 発展課題（プログラマー志望向け）

**プレイヤー追尾の移動タイプを自分で追加してみよう。**

1. `EnemyMoveType` に種類を足す  
2. `switch` に case と `MoveHoming()` を足す  
3. プレイヤー Transform の探し方・近づき方・速さの上限を考える  

### 撃破

- 既存のスコア・ヒットスプライト・SE を流用
- あとからプール返却に切り替える（見た目の消滅演出後に Return）

---

## 8. 敵の弾

| 種類 | 発射時の向きの決め方 |
|------|----------------------|
| 直進 | 敵の正面 or 固定角度 |
| プレイヤー狙い | 発射瞬間のプレイヤー方向を計算して固定（撃ち出し後は曲がらない） |
| 奇数拡散 | 狙い角度を中央に、左右対称 |
| 偶数拡散 | 中央なしの左右対称 |

### 作り方

1. `EnemyShooter`（間隔・BulletType または弾プレハブ参照）
2. プレイヤー狙い：

```csharp
Vector2 dir = (player.position - transform.position).normalized;
```

3. プレイヤー弾と同様、プール推奨
4. プレイヤー側は `EnemyBullet` タグで残機減少

敵が画面内に入ってから撃つ、画面外では撃たない、などすると負荷と理不尽さが下がります。

---

## 9. UI（GameOver / GameClear）

| 表示 | 更新タイミング |
|------|----------------|
| スコア | 敵撃破時（既存 `ScoreText` 流用可） |
| GameOver | プレイヤー被弾時にオブジェクトを `SetActive(true)` |
| GameClear | 最終 Wave まで生き残ったとき（`EnemyCreater` が全 Wave 終了） |
| Wave 数 | （任意）Wave 開始時 |

### 初心者向けの作り方（残機なし）

1. Canvas 下に `GameOver` / `GameClear` オブジェクトを用意し、最初は **非アクティブ**  
2. それぞれに `GameOver` / `GameClear` スクリプトを付け、**Retry Button** を割り当てる  
3. `Player` の **Game Over Object** に `GameOver` を割り当て  
4. `EnemyCreater` の **Game Clear Object** に `GameClear` を割り当て  
5. 被弾 → `gameOverObject.SetActive(true)`  
6. 全 Wave 終了かつ **画面上の敵が0体** かつ未被弾 → `gameClearObject.SetActive(true)`  
7. RetryButton → `SceneManager.LoadScene(今のシーン名)` で最初からやり直し  

```csharp
// GameOver / GameClear 共通の考え方
void OnRetryButtonClicked()
{
    Time.timeScale = 1f;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
```

```text
被弾                    → GameOver 表示
最終Waveの敵を出し切る
  → 敵が全滅するまで待つ
  → GameClear 表示（被弾済みなら出さない）
RetryButton             → 現在の Scene を読み直す
```

### コントローラーでボタンが押せないとき

マウスはクリック位置で押せますが、**ゲームパッドは「選択中の UI」がないと Submit（A / ×）が効きません。**

直し方（推奨・授業向け）：

1. `GameOver` / `GameClear` 表示時に RetryButton を選択する  

```csharp
EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
```

2. Hierarchy に **EventSystem** があり、**Input System UI Input Module** が付いていること  
3. Button の **Interactable** がオンであること  

（発展）複数ボタンがある画面では、Navigation を Automatic / Explicit にしてスティックで移動できるようにする。

※ 最終 Wave は `waitUntilCleared` がオフでも、クリア前は必ず敵全滅を待ちます。

### 残機を足す場合（あとから）

```text
life-- → UI更新 → 一瞬無敵 → life==0 で GameOver
```

---

## 10. オブジェクトプーリング（負荷対策）

### なぜ必要か

弾・敵を `Instantiate` / `Destroy` し続けると、ゴミ収集（GC）でフレームがカクつきやすいです。

### やること

- 敵・プレイヤー弾・敵弾をプール
- 「消す」＝ `Destroy` ではなく **非アクティブにしてキューへ返す**

### 作り方（最小）

1. `ObjectPool`：プレハブごとに Queue を持つ
2. `Get()`：あれば取り出して `SetActive(true)`、なければ生成
3. `Release()`：状態リセット → `SetActive(false)` → キューへ
4. 弾・敵の「画面外／撃破後」は `Destroy` の代わりに `Release`

### 併用すると効く対策

- 画面外判定を距離ではなく **カメラ外（または固定のワールド矩形）** にする
- 追尾・当たりは必要最小限のオブジェクトだけ
- `FindObjectOfType` は `Start` でキャッシュ（既存の毎回探しはやめる）
- パーティクルや SE の同時再生数に上限

### 実装順の注意

プールは **発射と敵出現が動いてから** 導入してよい。  
先にゲームとして回ることを優先し、カクついたら差し替える、でも問題ありません。

---

## クラス構成の目安（最終形イメージ）

```text
Player
  ├ Input（Move / Attack / FireAuto）
  ├ 移動（velocity, accel, maxSpeed）
  ├ 射撃（currentBulletType → BulletCreater, プール, 画面内弾数）
  └ 残機・被弾

BulletType（enum）
BulletCreater（switch で出し方を切り替え）
Bullet（1発の挙動／プレハブごとに pierce）
ObjectPool

Enemy（EnemyMoveType: Straight / SideWays）
EnemyCreater（WaveInfo[] を順番消化）
WaveInfo（1 Wave の設定：数・間隔・moveType）

Item（ItemType → Player.ApplyItem）
Item_MoveSpeed / Item_FireRate / Item_WeaponChange
BackgroundScroller（レイヤー複数可）
GameManager（スコア, Wave, ゲーム状態）
UI（Score / Life / Wave）
```

既存ファイルの増やし方の例：

- `Item.cs` / `ItemType.cs` … アイテム取得と種類
- `Player.cs` … `ApplyItem` で移動・連射・武器を強化
- `Enemy.cs` … 撃破時ドロップ（`dropItemPrefabs`）
- `BulletCreater.cs` … `switch(type)` で `CreateStraight` / `CreateTriple` など
- `Bullet.cs` … 弾の移動・距離で消滅（追尾は発展課題）
- `BulletType.cs` … ショット種類の enum
- `Enemy.cs` … `switch(moveType)` で直進／左右移動
- `EnemyMoveType.cs` … 敵の移動パターン enum
- `EnemyCreater.cs` … Wave 配列を順番に消化して敵を出す
- `WaveInfo.cs` … 1 Wave 分の設定（Serializable クラス）
- `Move.cs` … 汎用直進のまま（弾以外で使う場合）

---

## 動作確認チェックリスト（章ごと）

- [ ] キー・パッド両方で移動・単発・連射ができる
- [ ] 慣性がある／最高速度で頭打ちになる
- [ ] 背景が途切れずループする
- [ ] 弾種（直進／3方向／貫通）がタイプ切り替えで変わる
- [ ] （発展）追尾弾を自分で追加できた
- [ ] Wave が順番に進み、全滅待ちのあと次 Wave に入る
- [ ] 全 Wave 終了で Clear になる
- [ ] アイテムで移動・連射・弾種が変わる
- [ ] 敵の直進／左右移動が動く
- [ ] Player / Enemy / Bullet の Trigger 当たり判定が動く（弾で敵撃破・接触で被弾）
- [ ] （発展）敵の追尾移動を自分で追加できた
- [ ] 敵弾の狙い・拡散が動く
- [ ] 被弾で GameOver が表示される
- [ ] 最終 Wave 生き残りで GameClear が表示される
- [ ] UI（スコア・Wave）が正しい
- [ ] 長時間プレイしても Destroy 多用時より安定する（プール導入後）

---

## 既存サンプルからの最初の一歩（具体）

いますぐ着手するならこの順が短いです。

1. **Input System** で既存 `InputSystem_Actions` の `Move` / `Attack` を既存 `Player` に接続（見た目はまだそのままでよい）
2. **速度ベクトル＋加速度** に移動を変更
3. **FireAuto** と **maxBulletsOnScreen** を追加
4. **BulletType** を渡して `BulletCreater` の switch で直進1発を撃つ
5. その後、Triple → Pierce → アイテムで `SetBulletType` → プール（追尾は発展課題）

この順だと、毎ステップ「前より面白くなった」状態で止められます。
