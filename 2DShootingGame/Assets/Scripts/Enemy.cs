using UnityEngine;

/// <summary>
/// 敵の移動・体力・撃破・アイテムドロップを管理するスクリプト。
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

    [Header("移動パターン")]
    public EnemyMoveType moveType = EnemyMoveType.Straight;

    [Header("移動スピード")]
    public float moveSpeed = 3f;

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
 
    [Tooltip("アイテムドロップの確率 0〜1。例: 0.3 = 30%")]
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

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Inspector で設定した最大体力を、現在の体力に入れる Mathf.Max を使い、スクリプトから0以下が設定されても最低1にする
        currentHealth = Mathf.Max(1, maxHealth);

        // spawnSide に応じて、画面のどの端から出るかと進行方向を決める
        // 左から出る敵は右へ、右から出る敵は左へ進む
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

        // 移動タイプごとに関数を分ける
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

        // 出現位置から一定距離以上離れたら削除
        if (Vector3.Distance(transform.position, spawnPosition) > destroyDistance)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 一直線に進む。
    /// </summary>
    void MoveStraight()
    {
        Vector2 dir = GetForward();
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 進みながら、進行方向に対して横方向へ往復する。
    /// （右から来ると上下に揺れ、上から来ると左右に揺れる）
    /// </summary>
    void MoveSideWays()
    {
        Vector2 forward = GetForward();
        // 進行方向に垂直なベクトル（横方向）
        Vector2 side = new Vector2(-forward.y, forward.x);

        // 敵が進む中心線を、通常の直進と同じように前へ動かす
        basePosition += (Vector3)(forward * moveSpeed * Time.deltaTime);

        // Sin は -1～1を繰り返すため、横方向へ滑らかに往復できる
        sideTimer += Time.deltaTime;
        float offset = Mathf.Sin(sideTimer * sideFrequency) * sideAmplitude;

        // 前進する中心位置に、横方向のずれを足して最終位置を決める
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

    void OnTriggerEnter2D(Collider2D other)
    {
        // 処理の流れ：
        // Bullet タグを確認 → 体力を減らす → 通常弾を消す → 体力0なら撃破
        if (!other.CompareTag("Bullet"))
        {
            return;
        }

        // 当たったオブジェクトから Bullet スクリプトを取得する
        // pierce が true なら貫通弾、false なら通常弾
        Bullet bullet = other.GetComponent<Bullet>();

        // すでに撃破済みなら、体力やスコアをもう一度変更しない
        if (isHit)
        {
            // 通常弾だけを削除する。貫通弾はそのまま先へ進ませる
            if (bullet == null || !bullet.isPierce)
            {
                Destroy(other.gameObject);
            }
            return;
        }

        // 弾が1発当たったので、現在の体力を1減らす
        currentHealth = currentHealth - 1;

        // 通常弾は敵に当たった時点で削除する
        if (bullet == null || !bullet.isPierce)
        {
            Destroy(other.gameObject);
        }

        // 体力が1以上残っている場合は、ここで処理を終える
        if (currentHealth > 0)
        {
            // return すると、この下にある Defeat は実行されない
            return;
        }

        // ここまで進んだ場合は体力が0以下なので、敵を撃破する
        Defeat();
    }

    /// <summary>
    /// 体力が0になったときの撃破処理。
    /// </summary>
    void Defeat()
    {
        // スコアを加算する
        scoreText.AddScore(scoreValue);

        // 撃破時の効果音を鳴らす
        if (se != null)
        {
            se.PlaySE(hitSound);
        }

        // 撃破された見た目に変更する
        if (hitSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = hitSprite;
            transform.localScale = hitSpriteSize;
        }

        // すぐに削除せず、timeHit 秒だけ撃破スプライトを表示する
        hitTimer = timeHit;
        isHit = true;

        // 撃破後にほかの弾やプレイヤーと当たらないよう、当たり判定を止める
        collider2D.enabled = false;

        // 設定された確率でアイテムを出す
        TryDropItem();
    }

    /// <summary>
    /// 撃破時に確率でアイテムを1つ出す。
    /// </summary>
    void TryDropItem()
    {
        // 候補が登録されていない場合は、アイテムを生成できない
        if (dropItemPrefabs == null || dropItemPrefabs.Length == 0)
        {
            return;
        }

        // Random.value は 0～1 の乱数。dropChance より大きければドロップしない
        if (Random.value > dropChance)
        {
            return;
        }

        // null でない候補からランダムに1つ
        GameObject prefab = null;
        int guard = 0;

        // 配列に未設定の要素があっても、最大10回まで別の候補を探す
        // guard を使うことで、すべて未設定でも無限ループにならない
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
