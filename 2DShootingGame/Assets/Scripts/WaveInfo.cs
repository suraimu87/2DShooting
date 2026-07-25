using System;
using UnityEngine;

/// <summary>
/// 1つの Wave の設定（構造体）。
/// EnemyCreater の waves 配列の1要素として Inspector に表示されます。
/// </summary>
[Serializable]
public struct WaveInfo
{
    [Header("■ 名前")]
    [Tooltip("デバッグや UI 用の名前（例: Wave 1）")]
    public string waveName;

    [Header("■ 出す敵")]
    [Tooltip("この Wave で生成する敵のプレハブ")]
    public GameObject enemyPrefab;

    [Tooltip("この Wave で出す敵の数")]
    public int enemyCount;

    [Tooltip("敵を1体ずつ出す間隔（秒）")]
    public float spawnInterval;

    [Header("■ 動き・出現")]
    [Tooltip("Straight=直進 / SideWays=左右に揺れながら前進")]
    public EnemyMoveType moveType;

    [Tooltip("出現方向 0=左 1=右 2=上 3=下 / -1=ランダム")]
    public int spawnSide;

    [Header("■ 次の Wave への区切り")]
    [Tooltip("この Wave の敵を出し切ったあと、次の Wave まで待つ秒数")]
    public float waitAfterSpawn;

    [Tooltip("オン：画面上の敵がいなくなってから waitAfterSpawn を数える")]
    public bool waitUntilCleared;
}
