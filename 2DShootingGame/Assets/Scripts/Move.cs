using UnityEngine;

/// <summary>
/// 指定した方向に移動していくスクリプト。
/// </summary>
public class Move : MonoBehaviour
{
    [Header("移動スピード（1秒あたり）")]
    public float speed = 10f;

    [Header("移動する方向（X: 左右, Y: 上下）")]
    [Tooltip("例: (1,0)=右, (-1,0)=左, (0,1)=上")]
    public Vector2 direction = new Vector2(1f, 0f);

    [Header("生成位置からこの距離を超えたら削除")]
    public float destroyDistance = 15f;

    // 発射位置
    Vector3 startPosition;

    void Start()
    {
        // 生成位置を設定
        startPosition = transform.position;
    }

    void Update()
    {
        // 毎フレーム、設定した方向に移動させる（向きは正規化して使用）
        Vector2 dir = direction.normalized;
        // ゼロベクトル対策：向きが(0,0)やごく小さい値だと正規化しても(0,0)のまま動かないため、デフォルトで右向きにする
        if (dir.sqrMagnitude < 0.01f)
        {
            dir = new Vector2(1f, 0f);
        }
        // 設定した方向に移動させる
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        // 生成位置から一定距離以上離れたら削除する
        if (Vector3.Distance(transform.position, startPosition) > destroyDistance)
        {
            // 削除
            Destroy(gameObject);
        }
    }
}
