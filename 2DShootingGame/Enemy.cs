using UnityEngine;

/// <summary>
/// 敵の移動・体力・撃破・アイテムドロップを管理するスクリプト。
/// 追加: enemyType による複数の挙動を実装しました（Normal, Accelerate, MatchPlayerHeight, RandomVertical, Ambush）。
/// </summary>
public class Enemy : MonoBehaviour
{
    /// <summary>
    /// 敵の移動の種類（列挙型）。
    /// </summary>
    public enum EnemyMoveType
    {
        /// <summary>進行方向へまっすぐ移動する</summary>
        Straight = 0,

        /// <summary>進行方向へ進みながら、横方向へ往復する</summary>
        SideWays = 1
    }

    [Header("敵の種類（挙動）")]
    public EnemyType enemyType = EnemyType.Normal;

    [Header("移動パターン（既存）")]
    public EnemyMoveType moveType = EnemyMoveType.Straight;

    [Header("移動スピード")]
    public float moveSpeed = 3f;

    // （既存フィールド）
    [Header("飛ぶ方向（X: 左右, Y: 上下）(-1,0)=左, (1,0)=右, (0,-1)=下")]
    public Vector2 moveDirection = new Vector2(-1f, 0f);

    [Header("左右移動（SideWays）用 横方向にどれだけ振れるか")]
    public float sideAmplitude = 1.5f;

    [Tooltip("左右移動（SideWays）用 往復の速さ（大きいほど速く振れる）")]
    public float sideFrequency = 3f;

    [Header("画面端の座標（出現用）")]
    public float spawnBoundLeft = -12f;
    public float spawnBoundRight = 12f;
    public float spawnBoundUp = 6f;
    public float spawnBoundDown = -6f;

    [Header("出現位置からこの距離を超えたら削除")]
    public float destroyDistance = 25f;

    [Header("体力（倒すために必要な弾の数）")]
    [Min(1)]
    public int maxHealth = 1;

    [Header("撃破時のスコア")]
    public int scoreValue = 10;

    [Header("撃破時の効果音")]
    public AudioClip hitSound;

    [Header("敵を撃破したときのスプライト")]
    public Sprite hitSprite;

    [Header("敵を撃破したときのスプライトのサイズ")]
    public Vector3 hitSpriteSize = new Vector3(1f, 1f, 1f);

    [Header("敵を撃破した後、消えるまでの時間（秒）")]
    public float timeHit = 0.6f;

    [Header("アイテムドロップ")]
    public GameObject[] dropItemPrefabs;

    [Tooltip("アイテムドロップの確率 0?1。例: 0.3 = 30%")]
    [Range(0f, 1f)]
    public float dropChance = 0.3f;

    /// <summary> 出現方向 0=左, 1=右, 2=上, 3=下。EnemyCreater が設定。 </summary>
    [HideInInspector]
    public int spawnSide = 1;

    // 出現位置（削除判定用）
    Vector3 spawnPosition;

    // 左右移動の基準位置（前進した中心線）
    Vector3 basePosition;

    // 左右移動の経過時間（sin の入力）
    float sideTimer;

    // シーンに1つあるスコア表示を探して、撃破時の加算に使う
    ScoreText scoreText;

    // シーンに1つある効果音再生用のオブジェクト
    SeAudioSource se;

    // 画像切り替え用
    SpriteRenderer spriteRenderer;

    // 当たり判定
    Collider2D collider2D;

    // 現在の体力
    int currentHealth;

    // 体力が0になり、撃破演出中かを覚えておくフラグ
    bool isHit = false;

    // ヒットしてから消えるまでの時間
    float hitTimer;

    // プレイヤー参照（いくつかの挙動で使用）
    Player playerRef;

    // --- Accelerate 用 ---
    [Header("Accelerate: 初期速度から徐々に増加させる速度加算量（秒あたり）")]
    public float acceleratePerSecond = 0.5f;
    [Header("Accelerate: 最高速度")]
    public float accelerateMaxSpeed = 6f;
    float originalMoveSpeed;

    // --- MatchPlayerHeight 用 ---
    [Header("MatchPlayerHeight: Y を追従する速さ（Lerp 比率）")]
    public float matchYFollowSpeed = 4f;

    // --- RandomVertical 用 ---
    [Header("RandomVertical: 垂直振れ幅の最小値")]
    public float randomVerticalAmpMin = 0.5f;
    [Header("RandomVertical: 垂直振れ幅の最大値")]
    public float randomVerticalAmpMax = 2.5f;
    [Header("RandomVertical: 上下往復の速さ")]
    public float randomVerticalFreqMin = 1f;
    public float randomVerticalFreqMax = 4f;
    [Header("RandomVertical: スケールの最小/最大")]
    public float randomScaleMin = 0.6f;
    public float randomScaleMax = 1.4f;
    float randomVerticalAmp;
    float randomVerticalFreq;
    float randomTimer = 0f;

    // --- Ambush 用 ---
    [Header("Ambush: プレイヤーに近づくトリガー距離")]
    public float ambushTriggerDistance = 6f;
    [Header("Ambush: 止まる時間（秒）")]
    public float ambushStopDuration = 2f;
    [Header("Ambush: 突撃時の速度倍率")]
    public float ambushDashMultiplier = 6f;
    [Header("Ambush: 突撃前の演出時間（秒）")]
    public float ambushPrepTime = 0.2f;
    bool ambushTriggered = false;
    float ambushTimer = 0f;
    bool isDashing = false;
    float ambushPrepTimer = 0f;
    Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalMoveSpeed = moveSpeed;

        // 現在の体力を設定
        currentHealth = Mathf.Max(1, maxHealth);

        // spawnSide に応じて、画面のどの端から出るかと進行方向を決める（既存の挙動）
        if (spawnSide == 0) // 左から右へ
        {
            transform.position = new Vector3(spawnBoundLeft, Random.Range(spawnBoundDown, spawnBoundUp), 0f);
            moveDirection = new Vector2(1f, 0f);
        }
        if (spawnSide == 1) // 右から左へ
        {
            transform.position = new Vector3(spawnBoundRight, Random.Range(spawnBoundDown, spawnBoundUp), 0f);
            moveDirection = new Vector2(-1f, 0f);
        }
        if (spawnSide == 2) // 上から下へ
        {
            transform.position = new Vector3(Random.Range(spawnBoundLeft, spawnBoundRight), spawnBoundUp, 0f);
            moveDirection = new Vector2(0f, -1f);
        }
        if (spawnSide == 3) // 下から上へ
        {
            transform.position = new Vector3(Random.Range(spawnBoundLeft, spawnBoundRight), spawnBoundDown, 0f);
            moveDirection = new Vector2(0f, 1f);
        }

        spawnPosition = transform.position;
        basePosition = transform.position;
        sideTimer = 0f;

        // シーン内で共有しているスコア表示と効果音再生用オブジェクトを探す
        scoreText = FindAnyObjectByType<ScoreText>();
        se = FindAnyObjectByType<SeAudioSource>();
        collider2D = GetComponent<Collider2D>();

        // プレイヤー参照（必要な挙動で使用）
        playerRef = FindAnyObjectByType<Player>();

        // RandomVertical 用にランダム設定
        if (enemyType == EnemyType.RandomVertical)
        {
            randomVerticalAmp = Random.Range(randomVerticalAmpMin, randomVerticalAmpMax);
            randomVerticalFreq = Random.Range(randomVerticalFreqMin, randomVerticalFreqMax);
            float scale = Random.Range(randomScaleMin, randomScaleMax);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        originalColor = (spriteRenderer != null) ? spriteRenderer.color : Color.white;
    }

    void Update()
    {
        if (isHit)
        {
            // 撃破後すぐには削除せず、スプライトを timeHit 秒だけ表示する
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                Destroy(gameObject);
            }
            return;
        }

        // 敵の種類ごとに関数を分ける（enum + switch）
        switch (enemyType)
        {
            case EnemyType.Normal:
                // 既存の移動タイプに従う
                MoveByMoveType();
                break;

            case EnemyType.Accelerate:
                MoveAccelerate();
                break;

            case EnemyType.MatchPlayerHeight:
                MoveMatchPlayerHeight();
                break;

            case EnemyType.RandomVertical:
                MoveRandomVertical();
                break;

            case EnemyType.Ambush:
                MoveAmbush();
                break;

            default:
                MoveByMoveType();
                break;
        }

        // 出現位置から一定距離以上離れたら削除
        if (Vector3.Distance(transform.position, spawnPosition) > destroyDistance)
        {
            Destroy(gameObject);
        }
    }

    // 既存の移動判定（MoveType に従う）
    void MoveByMoveType()
    {
        switch (moveType)
        {
            case EnemyMoveType.Straight:
                MoveStraight();
                break;
            case EnemyMoveType.SideWays:
                MoveSideWays();
                break;
            default:
                MoveStraight();
                break;
        }
    }

    /// <summary>
    /// 通常の直進
    /// </summary>
    void MoveStraight()
    {
        Vector2 dir = GetForward();
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 進みながら、進行方向に対して横方向へ往復する。
    /// </summary>
    void MoveSideWays()
    {
        Vector2 forward = GetForward();
        Vector2 side = new Vector2(-forward.y, forward.x);

        basePosition += (Vector3)(forward * moveSpeed * Time.deltaTime);

        sideTimer += Time.deltaTime;
        float offset = Mathf.Sin(sideTimer * sideFrequency) * sideAmplitude;

        transform.position = basePosition + (Vector3)(side * offset);
    }

    Vector2 GetForward()
    {
        Vector2 dir = moveDirection.normalized;
        if (dir.sqrMagnitude < 0.01f)
        {
            dir = new Vector2(-1f, 0f);
        }
        return dir;
    }

    // --- 新規挙動の実装 ---

    /// <summary>
    /// Accelerate: 徐々に速度を上げる。基本は直進/SideWays に従うが速度だけ増える。
    /// </summary>
    void MoveAccelerate()
    {
        // 速度をだんだん増やす（元の速度を保存しておき、最大値まで増加）
        moveSpeed = Mathf.Min(accelerateMaxSpeed, moveSpeed + acceleratePerSecond * Time.deltaTime);

        // 既存の移動パターンに任せる
        MoveByMoveType();
    }

    /// <summary>
    /// MatchPlayerHeight: 前進しつつ Y 座標をプレイヤーへ追従する
    /// </summary>
    void MoveMatchPlayerHeight()
    {
        // 水平方向の前進は既存の forward を使う
        Vector2 forward = GetForward();
        basePosition += (Vector3)(forward * moveSpeed * Time.deltaTime);

        // プレイヤーが見つかれば、Y を滑らかに追従する
        if (playerRef != null)
        {
            Vector3 pos = transform.position;
            float targetY = Mathf.Clamp(playerRef.transform.position.y, spawnBoundDown, spawnBoundUp);
            pos.y = Mathf.Lerp(pos.y, targetY, Mathf.Clamp01(matchYFollowSpeed * Time.deltaTime));
            // X は前進による basePosition に合わせる
            pos.x = basePosition.x;
            transform.position = pos;
        }
        else
        {
            // プレイヤーがいない場合は通常移動
            transform.position = basePosition;
        }
    }

    /// <summary>
    /// RandomVertical: 上下にランダムに往復、振れ幅とスケールは Start でランダム化
    /// </summary>
    void MoveRandomVertical()
    {
        Vector2 forward = GetForward();
        // 前進する中心線を進める
        basePosition += (Vector3)(forward * moveSpeed * Time.deltaTime);

        // Sin を使って上下に揺らす（forward に垂直な方向へ振る）
        Vector2 side = new Vector2(-forward.y, forward.x);

        randomTimer += Time.deltaTime;
        float offset = Mathf.Sin(randomTimer * randomVerticalFreq) * randomVerticalAmp;

        transform.position = basePosition + (Vector3)(side * offset);
    }

    /// <summary>
    /// Ambush: プレイヤーに近づくと停止してから演出→爆速突撃
    /// </summary>
    void MoveAmbush()
    {
        // ダッシュ中ならその向きへ直進
        if (isDashing)
        {
            transform.position += (Vector3)(moveDirection.normalized * moveSpeed * Time.deltaTime);
            return;
        }

        // プレイヤーがいない場合は通常移動
        if (playerRef == null)
        {
            MoveByMoveType();
            return;
        }

        // 距離判定
        float dist = Vector3.Distance(transform.position, playerRef.transform.position);

        // まだトリガーされていない場合、近づいたら停止状態へ
        if (!ambushTriggered && dist <= ambushTriggerDistance)
        {
            ambushTriggered = true;
            ambushTimer = ambushStopDuration;
            // 停止開始（止めるために移動しない）
            return;
        }

        // 停止中
        if (ambushTriggered && ambushTimer > 0f)
        {
            ambushTimer -= Time.deltaTime;
            // 完全に停止するため移動処理は行わない
            return;
        }

        // 停止が終わり、まだ演出フェーズが残っている場合
        if (ambushTriggered && ambushTimer <= 0f && ambushPrepTimer <= 0f)
        {
            // 演出（色を変えるなど）を短時間行う
            ambushPrepTimer = ambushPrepTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.yellow;
            }
            return;
        }

        // 演出タイマーを回す
        if (ambushPrepTimer > 0f)
        {
            ambushPrepTimer -= Time.deltaTime;
            if (ambushPrepTimer <= 0f)
            {
                // 演出終了、元の色にもどす
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }

                // 突撃開始：プレイヤー方向へ向けて速度を上げる
                Vector2 dirToPlayer = (playerRef.transform.position - transform.position).normalized;
                moveDirection = dirToPlayer;
                moveSpeed = originalMoveSpeed * ambushDashMultiplier;
                isDashing = true;
            }
            return;
        }

        // 通常移動（まだトリガーしていない or トリガー済みだが上のいずれの条件にも当てはまらない）
        MoveByMoveType();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 処理の流れ：
        // Bullet タグを確認 → 体力を減らす → 通常弾を消す → 体力0なら撃破
        if (!other.CompareTag("Bullet"))
        {
            return;
        }

        Bullet bullet = other.GetComponent<Bullet>();

        if (isHit)
        {
            // 通常弾だけを削除する。貫通弾はそのまま先へ進ませる
            if (bullet == null || !bullet.isPierce)
            {
                Destroy(other.gameObject);
            }
            return;
        }

        currentHealth = currentHealth - 1;

        // 通常弾は敵に当たった時点で削除する
        if (bullet == null || !bullet.isPierce)
        {
            Destroy(other.gameObject);
        }

        if (currentHealth > 0)
        {
            return;
        }

        Defeat();
    }

    void Defeat()
    {
        scoreText.AddScore(scoreValue);

        if (se != null)
        {
            se.PlaySE(hitSound);
        }

        if (hitSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = hitSprite;
            transform.localScale = hitSpriteSize;
        }

        hitTimer = timeHit;
        isHit = true;

        collider2D.enabled = false;

        TryDropItem();
    }

    void TryDropItem()
    {
        if (dropItemPrefabs == null || dropItemPrefabs.Length == 0)
        {
            return;
        }

        if (Random.value > dropChance)
        {
            return;
        }

        GameObject prefab = null;
        int guard = 0;

        while (prefab == null && guard < 10)
        {
            prefab = dropItemPrefabs[Random.Range(0, dropItemPrefabs.Length)];
            guard++;
        }

        if (prefab != null)
        {
            Instantiate(prefab, transform.position, Quaternion.identity);
        }
    }
}
```

注意と確認ポイント（短く）
- Inspector から各敵プレハブに `enemyType` を設定してください（例: ノーマル / Accelerate / MatchPlayerHeight / RandomVertical / Ambush）。
- `RandomVertical` は開始時にスケールと振れ幅をランダム化します（Inspector の min/max を調整可）。
- `Ambush` はプレイヤー検出に `FindAnyObjectByType<Player>()` の参照を使用します。シーン上に Player が存在することを確認してください。
- 演出は授業用に簡潔化（色変化）しています。Animator を使う場合は別途提案しますが、初心者向けの簡単な実装を優先しました。
- 既存の動作（体力・撃破・ドロップ等）は変更していません。

次にやること（提案）
- 各プレハブの Inspector を更新して `enemyType` と（必要なら）各パラメータを調整します。
- 突撃のアニメーションを Animator で本格化する場合は提案します（簡易実装 ? Animator に置換する手順を示します）。

続けますか？（例）
- 既存プレハブに `enemyType` を反映して自動で設定する小さなエディタスクリプトを作る  
- Ambush の演出を Sprite 切替やアニメータトリガに置き換える（提案を出します）- RandomVertical
