using UnityEngine;

/// <summary>
/// 取得アイテム。
/// 種類ごとに Player へ効果を渡し、自分は消えます。
/// （enum + switch の授業向け構成。ScriptableObject は使わない）
/// </summary>
public class Item : MonoBehaviour
{
    [Header("種類")]
    public ItemType itemType = ItemType.MoveSpeedUp;

    [Header("移動速度アップ用")]
    public float moveSpeedAdd = 1.5f;

    [Header("連射力アップ用")]
    public float fireCoolTimeScaleReduce = 0.15f;

    [Header("武器チェンジ用 取得後に切り替える弾種")]
    public Bullet.BulletType weaponType = Bullet.BulletType.Triple;

    [Header("動くスピード")]
    public Vector2 driftSpeed = new Vector2(-2f, 0f);

    [Header("生成位置からこの距離を超えたら削除")]
    public float destroyDistance = 20f;

    /// <summary>
    /// アイテムの種類。
    /// Item が持ち、Player.ApplyItem の switch で効果を分ける。
    /// </summary>
    public enum ItemType
    {
        /// <summary>移動速度アップ</summary>
        MoveSpeedUp = 0,

        /// <summary>連射力アップ（クールタイム短縮）</summary>
        FireRateUp = 1,

        /// <summary>武器チェンジ（弾の種類を変更）</summary>
        WeaponChange = 2,
    }

    // 生成された場所を覚え、画面外へ流れたあとの削除判定に使う
    Vector3 startPosition;

    // 同じアイテムの効果が複数回適用されることを防ぐ
    bool isCollected;

    void Start()
    {
        // Start は生成後に1回だけ呼ばれるため、ここで最初の位置を記録する
        startPosition = transform.position;
    }

    void Update()
    {
        if (isCollected)
        {
            return;
        }

        // アイテムを一定方向へ流し、プレイヤーが取りに行けるようにする
        transform.position += (Vector3)(driftSpeed * Time.deltaTime);

        // 取得されずに遠くまで移動したアイテムは、シーンに残さず削除する
        if (Vector3.Distance(transform.position, startPosition) > destroyDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Trigger が続けて呼ばれても、アイテムの効果は1回だけにする
        if (isCollected)
        {
            return;
        }

        // Player タグ以外のオブジェクトに触れた場合は何もしない
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            return;
        }

        // 先に取得済みにしてから Player に効果を渡し、最後にアイテムを削除する
        isCollected = true;
        player.ApplyItem(this);
        Destroy(gameObject);
    }
}
