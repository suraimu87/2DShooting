using UnityEngine;

/// <summary>
/// 弾のスクリプト
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("移動スピード（1秒あたり）")]
    public float speed = 10f;

    [Header("生成位置からこの距離を超えたら削除")]
    public float destroyDistance = 15f;

    [Header("貫通するかフラグ")]
    public bool isPierce = false;

    // 移動する方向
    public Vector2 direction = new Vector2(1f, 0f);

    // 発射位置
    Vector3 startPosition;

    /// <summary>
    /// ショットの種類（列挙型）。
    /// </summary>
    public enum BulletType
    {
        /// <summary>直進1発（クールタイムが短い）</summary>
        Straight = 0,

        /// <summary>直進3方向（正面・斜め上・斜め下）。クールタイムが少し長い</summary>
        Triple = 1,

        /// <summary>貫通弾</summary>
        Pierce = 2,
    }

    /// <summary>
    /// 弾の生成直後に BulletCreater から1回呼ばれ、飛ぶ向きを受け取る。
    /// </summary>
    public void Setup(Vector2 direction)
    {
        // 方向がゼロベクトルに近い場合は右方向にする sqrMagnitude:ベクトルの長さの二乗を返す
        if (direction.sqrMagnitude > 0.01f)
        {
            this.direction = direction.normalized;
        }
        else
        {
            this.direction = Vector2.right;
        }

        // 発射位置を記録
        startPosition = transform.position;
    }

    void Update()
    {
        // 設定した方向に移動
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // 生成位置から一定距離以上離れたら削除 Distance:二つのベクトル間の距離を返す
        if (Vector3.Distance(transform.position, startPosition) > destroyDistance)
        {
            // オブジェクトを削除する
            Destroy(gameObject);
        }
    }

    // 弾と敵の当たり判定は Enemy.OnTriggerEnter2D 側で行う
    // Enemy が pierce を確認し、通常弾だけを削除する
}
