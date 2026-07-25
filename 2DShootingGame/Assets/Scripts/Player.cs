using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーのスクリプト
/// </summary>
public class Player : MonoBehaviour
{
    [Header("Input System（既存の InputSystem_Actions を割り当て）")]
    public InputActionAsset inputActions;

    [Header("移動スピード")]
    public float moveSpeed = 5f;

    [Header("アイテムで上がっても超えない上限")]
    public float maxMoveSpeed = 12f;

    [Header("画面端の制限（X, Y）")]
    public float limitX = 8.5f;
    public float limitY = 4.5f;

    [Header("弾の生成（同じオブジェクトの BulletCreater）")]
    public BulletCreater bulletCreater;

    [Header("今のショット種類（アイテムなどで切り替える）")]
    public Bullet.BulletType currentBulletType = Bullet.BulletType.Straight;

    [Header("連射（クールタイム倍率）")]
    public float fireCoolTimeScale = 1f;

    [Header("連射アイテムを取ってもこれより小さくしない")]
    public float minFireCoolTimeScale = 0.35f;

    [Header("弾を撃つときの効果音")]
    public AudioClip shootSound;

    [Header("壊れた（被弾）時の効果音")]
    public AudioClip brokenSound;

    [Header("被弾したときに変えるスプライト")]
    public Sprite hitSprite;
    SpriteRenderer spriteRenderer;

    [Header("ゲームオーバー表示")]
    public GameObject gameOverObject;

    // Input Actions の中から、移動と攻撃の操作だけを取り出して使う
    InputAction moveAction;
    InputAction attackAction;

    // 次に弾を撃てるようになるまでの残り時間
    float fireTimer = 0f;

    /// <summary>被弾して操作不能かどうか（Clear 判定などで使う）</summary>
    public bool isHit = false;
    SeAudioSource se;


    void Awake()
    {
        // Awake は Start より先に呼ばれるため、ここで入力を使う準備をする
        moveAction = inputActions.FindAction("Player/Move", throwIfNotFound: true);
        attackAction = inputActions.FindAction("Player/Attack", throwIfNotFound: true);

        // Inspector で未設定の場合は、同じ GameObject から自動で探す
        if (bulletCreater == null)
        {
            bulletCreater = GetComponent<BulletCreater>();
        }
    }

    void OnEnable()
    {
        // Player スクリプトが有効になったとき、入力を受け付ける
        moveAction.Enable();
        attackAction.Enable();
    }

    void OnDisable()
    {
        // GameOver / GameClear で Player が無効になったら入力も止める
        moveAction.Disable();
        attackAction.Disable();
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        se = FindObjectOfType<SeAudioSource>();

        // 開始時はゲームオーバー画面を非表示
        gameOverObject.SetActive(false);
    }

    void Update()
    {
        // 被弾後は、移動と射撃の処理を行わない
        if (isHit)
        {
            return;
        }

        // キーボードやゲームパッドから、X・Y方向の入力を受け取る
        Vector2 input = moveAction.ReadValue<Vector2>();

        // 斜め移動だけ速くならないよう、入力の長さを最大1にそろえる
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        // Time.deltaTime を掛け、フレームレートに関係なく同じ速さで動かす
        transform.position += (Vector3)(input * moveSpeed * Time.deltaTime);

        // プレイヤーが画面端の範囲を越えないように座標を制限する
        Vector3 currentPosition = transform.position;
        currentPosition.x = Mathf.Clamp(currentPosition.x, -limitX, limitX);
        currentPosition.y = Mathf.Clamp(currentPosition.y, -limitY, limitY);
        transform.position = currentPosition;

        // 毎フレーム残り時間を減らし、0以下になると次の弾を撃てる
        if (fireTimer > 0f)
        {
            fireTimer -= Time.deltaTime;
        }

        // IsPressed はボタンを押している間 true になるため、押しっぱなしで連射できる
        if (attackAction.IsPressed() && fireTimer <= 0f)
        {
            if (bulletCreater != null)
            {
                // 基本クールタイム × 連射倍率
                float baseCoolTime = bulletCreater.Shoot(currentBulletType);
                fireTimer = baseCoolTime * fireCoolTimeScale;
            }

            if (se != null)
            {
                se.PlaySE(shootSound);
            }
        }
    }

    /// <summary>
    /// アイテム取得時に呼ばれる。種類ごとに効果を分ける。
    /// </summary>
    public void ApplyItem(Item item)
    {
        if (item == null)
        {
            return;
        }

        switch (item.itemType)
        {
            case Item.ItemType.MoveSpeedUp:
                // 上限を越えない範囲で移動速度を上げる
                moveSpeed = Mathf.Min(moveSpeed + item.moveSpeedAdd, maxMoveSpeed);
                break;

            case Item.ItemType.FireRateUp:
                // クールタイム倍率を小さくして、次の弾を早く撃てるようにする
                fireCoolTimeScale = Mathf.Max(
                    fireCoolTimeScale - item.fireCoolTimeScaleReduce,
                    minFireCoolTimeScale
                );
                break;

            case Item.ItemType.WeaponChange:
                // これ以降に生成する弾の種類を切り替える
                currentBulletType = item.weaponType;
                break;
        }
    }

    /// <summary>
    /// アイテム取得などでショット種類を変えるときに呼ぶ。
    /// </summary>
    public void SetBulletType(Bullet.BulletType type)
    {
        currentBulletType = type;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 同じフレームに複数の敵と触れても、被弾処理は1回だけ行う
        if (isHit)
        {
            return;
        }

        // Enemy タグを持つオブジェクトに触れたときだけ被弾する
        if (other.CompareTag("Enemy"))
        {
            spriteRenderer.sprite = hitSprite;
            isHit = true;

            se.PlaySE(brokenSound);

            // ゲームオーバー画面を表示
            // 表示された GameOver 側で Player を無効にし、入力も停止する
            gameOverObject.SetActive(true);
        }
    }
}
