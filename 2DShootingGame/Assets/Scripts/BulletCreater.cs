using UnityEngine;

/// <summary>
/// 弾の生成スクリプト
/// </summary>
public class BulletCreater : MonoBehaviour
{
    [Header("弾を出す位置（未設定なら自分の位置）")]
    public Transform firePoint;

    [Header("正面の向き（右(1, 0)）左(-1, 0) 上(0, 1)")]
    public Vector2 forward = new Vector2(1f, 0f);

    [Header("直進弾プレハブ")]
    public GameObject straightBulletPrefab;

    [Header("貫通弾プレハブ")]
    public GameObject pierceBulletPrefab;

    [Header("直進弾クールタイム（秒）")]
    public float straightCoolTime = 0.08f;
    [Header("3方向直進弾クールタイム（秒）")]
    public float tripleCoolTime = 0.35f;
    [Header("貫通弾クールタイム（秒）")]
    public float pierceCoolTime = 0.25f;

    [Header("3方向の角度（度）正面=0、上がプラス")]
    public float tripleUpAngle = 20f;
    public float tripleDownAngle = -20f;

    /// <summary>
    /// タイプに応じて弾を生成する。
    /// 戻り値は Player の fireTimer に入り、次に撃てるまでの待ち時間になる。
    /// </summary>
    public float Shoot(Bullet.BulletType type)
    {
        // タイプによって生成の仕方を変える
        switch (type)
        {
            // 直進弾
            case Bullet.BulletType.Straight:
                CreateStraightBullet();
                return straightCoolTime;

            // スプレット弾
            case Bullet.BulletType.Triple:
                CreateTripleBullets();
                return tripleCoolTime;

            // 貫通弾
            case Bullet.BulletType.Pierce:
                CreatePierceBullet();
                return pierceCoolTime;

            // 例外,未設定パターン
            default:
                Debug.Log("未定義の玉の種類");
                CreateStraightBullet();
                return straightCoolTime;
        }
    }

    /// <summary>
    /// 直進する弾を1発だけ生成する。
    /// </summary>
    void CreateStraightBullet()
    {
        SpawnBullet(straightBulletPrefab, 0f);
    }

    /// <summary>
    /// 直進プレハブを3方向に生成する（正面・斜め上・斜め下）。
    /// </summary>
    void CreateTripleBullets()
    {
        SpawnBullet(straightBulletPrefab, 0f);
        SpawnBullet(straightBulletPrefab, tripleUpAngle);
        SpawnBullet(straightBulletPrefab, tripleDownAngle);
    }

    /// <summary>
    /// 貫通する弾プレハブを1発生成する。
    /// </summary>
    void CreatePierceBullet()
    {
        SpawnBullet(pierceBulletPrefab, 0f);
    }

    /// <summary>
    /// 共通の生成処理。angleDeg は正面からの角度（度）。
    /// </summary>
    void SpawnBullet(GameObject prefab, float angleDeg)
    {
        // 現在位置に座標を設定
        Vector3 spawnPos = transform.position;

        // 弾を出す位置（未設定なら自分の位置）
        if (firePoint != null)
        {
            spawnPos = firePoint.position;
        }

        // 弾を飛ばす基準方向を決める
        // forward がほぼ (0, 0) の場合は、初期値の右方向を使う
        Vector2 baseForward = Vector2.right;
        if (forward.sqrMagnitude > 0.01f)
        {
            baseForward = forward.normalized;
        }

        // 基準方向を angleDeg 度だけ回転させ、実際に弾が飛ぶ方向を作る
        // 3方向弾では 0度・斜め上・斜め下の3つの方向が作られる
        Vector2 dir = Quaternion.Euler(0f, 0f, angleDeg) * (Vector3)baseForward;

        // 弾プレハブを生成
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 生成した弾から Bullet スクリプトを取得する
        Bullet bullet = go.GetComponent<Bullet>();

        // 生成直後の弾に向きだけを渡す
        // 速度・貫通するかどうかは、各弾プレハブの Inspector 設定を使う
        bullet.Setup(dir);
    }
}
