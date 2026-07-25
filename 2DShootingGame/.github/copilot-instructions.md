# GitHub Copilot 向け — このプロジェクトの約束事

このリポジトリは **Unity / プログラミング初心者の学生** 向け 2D シューティング授業サンプルです。  
改修・提案は必ず初心者向けに保つこと。詳細はリポジトリ直下の `AGENTS.md` と `Shooting_BuildGuide.md` を参照する。

## コードを書くときの必守

1. **非同期処理をしない**  
   `async` / `await` / `Task`、安易な `Coroutine` は使わない。待ちは `Update` 内の float タイマーや `bool` フラグで書く。

2. **エラーは日本語の Debug ログ**  
   未設定・想定外は `Debug.LogWarning` / `Debug.LogError` で日本語の文を出し、必要なら `return`。

3. **初心者向けの日本語コメント**を処理の区切りに書く。

4. **難しい要求は簡単な方法に言い換えて提案**してから実装する（高度なまま実装しない）。

5. **ScriptableObject・LINQ・複雑な継承・インターフェース多用は使わない。**  
   種類分けは既存どおり `enum` + `switch` + 専用関数。

6. Input は既存の `Assets/InputSystem_Actions.inputactions` を使う（新規 Input Actions を増やさない）。

7. `EnemyCreater` / `BulletCreater` の綴りは変えない。

## 参照してほしい主な場所

- 方針の全文: `AGENTS.md`
- 授業用手順: `Shooting_BuildGuide.md`
- ゲーム本体スクリプト: `Assets/Scripts/*.cs`
- シーン: `Assets/Scenes/Shooting.unity`
- プレハブ: `Assets/Prefabs/`

`Library/`・`Temp/`・`Logs/`・`Obj/` は Unity の生成物なので、改修・説明の主対象にしない。

## 既存の書き方に合わせる

- 弾: `BulletType` → `BulletCreater` の switch
- 敵移動: `EnemyMoveType` → `Enemy` の switch
- Wave: `WaveInfo`（struct）の配列を `EnemyCreater` が順番消化
- アイテム: `ItemType` → `Player.ApplyItem`
- 結果画面: `GameOver` / `GameClear`（表示時に `Player.enabled = false`）

追尾弾・敵の追尾移動は発展課題。完成コードをそのまま渡さない。
