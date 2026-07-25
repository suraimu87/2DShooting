using UnityEngine;

/// <summary>
/// Wave 制で敵を出すスクリプト。
/// Inspector の waves 配列を上から順に消化します。
/// </summary>
public class EnemyCreater : MonoBehaviour
{
    /// <summary>
    /// 1つの Wave に必要な設定をまとめた構造体。
    /// </summary>
    [System.Serializable]
    public struct WaveInfo
    {
        [Header("名前")]
        public string waveName;

        [Header("この Wave で生成する敵のプレハブ")]
        public GameObject enemyPrefab;

        [Header("この Wave で出す敵の数")]
        public int enemyCount;

        [Header("敵を1体ずつ出す間隔（秒）")]
        public float spawnInterval;

        [Header("動き・出現 Straight=直進 / SideWays=左右に揺れながら前進")]
        public Enemy.EnemyMoveType moveType;

        [Header("出現方向 0=左 1=右 2=上 3=下 / -1=ランダム")]
        public int spawnSide;

        [Header("Wave の敵を出し切ったあと、次の Wave まで待つ秒数")]
        public float waitAfterSpawn;

        [Header("オン：画面上の敵がいなくなってから waitAfterSpawn を数える")]
        public bool waitUntilCleared;
    }

    [Header("Wave 一覧（上から順に実行）")]
    public WaveInfo[] waves;

    [Header("ゲームクリア表示")]
    public GameObject gameClearObject;

    [Header("プレイヤー")]
    public Player player;

    [Header("状態（実行中に変化・確認用）")]
    [SerializeField] int currentWaveIndex;
    [SerializeField] int spawnedInCurrentWave;
    [SerializeField] string currentWaveName;
    [SerializeField] bool allWavesFinished;
    [SerializeField] bool waitingForGameClear;

    // 次の敵を1体出すまでの待ち時間を数えるタイマー
    float spawnTimer;

    // 敵を出し切ったあと、次の Wave へ進むまでの時間を数えるタイマー
    float waitTimer;

    // 現在の Wave の敵をすべて生成し、次の Wave を待っているか
    bool waitingForNextWave;

    void Start()
    {
        currentWaveIndex = 0;
        spawnedInCurrentWave = 0;
        spawnTimer = 0f;
        waitTimer = 0f;
        waitingForNextWave = false;
        waitingForGameClear = false;
        allWavesFinished = false;
        UpdateCurrentWaveName();

        // 開始時は非表示にしておく
        if (gameClearObject != null)
        {
            gameClearObject.SetActive(false);
        }
    }

    void Update()
    {
        // Wave は次の順番で進む
        // 敵を間隔ごとに生成 → 必要なら全滅待ち → Wave 間の待ち → 次の Wave
        // 最終 Wave のあとは、すべての敵がいなくなってから GameClear にする

        // すでにゲームクリア処理が終わっている場合は、これ以上 Wave を進めない
        if (allWavesFinished)
        {
            return;
        }

        // Wave が1つも設定されていない場合は、敵を生成できないので処理を終える
        if (waves == null || waves.Length == 0)
        {
            return;
        }

        // --- 最終 Wave の出し終わり：画面上の敵が0になってから Clear ---
        if (waitingForGameClear)
        {
            if (CountAliveEnemies() > 0)
            {
                return;
            }

            FinishAllWaves();
            return;
        }

        if (currentWaveIndex >= waves.Length)
        {
            // ここに来る場合も、敵が残っていれば Clear を遅らせる
            waitingForGameClear = true;
            return;
        }

        WaveInfo wave = waves[currentWaveIndex];
        if (wave.enemyPrefab == null)
        {
            Debug.LogWarning("EnemyCreater: Wave の enemyPrefab が未設定です。次の Wave へ進みます。");
            GoToNextWave();
            return;
        }

        // --- 出し切ったあと：次 Wave への待ち ---
        if (waitingForNextWave)
        {
            // 最終 Wave のあとへ進む直前は、必ず敵全滅を待つ
            bool isLastWave = currentWaveIndex >= waves.Length - 1;
            bool needClearEnemies = wave.waitUntilCleared || isLastWave;

            if (needClearEnemies && CountAliveEnemies() > 0)
            {
                return;
            }

            // 全滅待ちが終わってから、Wave 間の待ち時間を数える
            waitTimer += Time.deltaTime;
            if (waitTimer >= wave.waitAfterSpawn)
            {
                GoToNextWave();
            }
            return;
        }

        // --- まだ出す敵が残っている：間隔ごとに生成 ---
        if (spawnedInCurrentWave < wave.enemyCount)
        {
            // 毎フレーム時間を足し、spawnInterval 秒たつごとに敵を1体出す
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= wave.spawnInterval)
            {
                SpawnOneEnemy(wave);
                spawnedInCurrentWave++;
                spawnTimer = 0f;

                // 出し切ったら「次 Wave 待ち」へ
                if (spawnedInCurrentWave >= wave.enemyCount)
                {
                    waitingForNextWave = true;
                    waitTimer = 0f;
                }
            }
        }
    }

    void SpawnOneEnemy(WaveInfo wave)
    {
        // Wave に設定された敵プレハブをシーンに生成する
        GameObject go = Instantiate(wave.enemyPrefab);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy == null)
        {
            return;
        }

        // 出現方向
        if (wave.spawnSide < 0)
        {
            enemy.spawnSide = Random.Range(0, 4);
        }
        else
        {
            enemy.spawnSide = wave.spawnSide;
        }

        // 移動パターン（プレハブの設定より Wave の指定を優先）
        enemy.moveType = wave.moveType;
    }

    void GoToNextWave()
    {
        // 配列の次の要素へ進み、敵の生成数とタイマーを初期状態に戻す
        currentWaveIndex++;
        spawnedInCurrentWave = 0;
        spawnTimer = 0f;
        waitTimer = 0f;
        waitingForNextWave = false;
        UpdateCurrentWaveName();

        if (currentWaveIndex >= waves.Length)
        {
            // すぐ Clear せず、敵が残っていれば待つ
            waitingForGameClear = true;
        }
        else
        {
            Debug.Log("Wave 開始: " + currentWaveName);
        }
    }

    /// <summary>
    /// 全 Wave 終了かつ敵が0。プレイヤーが生き残っていれば GameClear を表示する。
    /// </summary>
    void FinishAllWaves()
    {
        if (allWavesFinished)
        {
            return;
        }

        allWavesFinished = true;
        waitingForGameClear = false;
        currentWaveName = "Clear";
        Debug.Log("全 Wave 終了（敵全滅）");

        // 被弾済みならクリアにしない（GameOver 優先）
        // GameOver と GameClear が同時に表示されることを防ぐ
        if (player != null && player.isHit)
        {
            return;
        }

        if (gameClearObject != null)
        {
            gameClearObject.SetActive(true);
        }
    }

    void UpdateCurrentWaveName()
    {
        // 名前が未入力の場合は「Wave 1」のような番号付きの名前を表示する
        if (waves != null && currentWaveIndex >= 0 && currentWaveIndex < waves.Length)
        {
            currentWaveName = waves[currentWaveIndex].waveName;
            if (string.IsNullOrEmpty(currentWaveName))
            {
                currentWaveName = "Wave " + (currentWaveIndex + 1);
            }
        }
        else
        {
            currentWaveName = "-";
        }
    }

    int CountAliveEnemies()
    {
        // 授業向けに、シーン内の Enemy タグを持つオブジェクトをすべて探して数える
        // 撃破演出中の敵も、Destroy されるまでは数に含まれる
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.Length;
    }

    /// <summary>UI などから今の Wave 番号（1始まり）を取るとき用。</summary>
    public int GetCurrentWaveNumber()
    {
        int waveCount = 0;
        if (waves != null)
        {
            waveCount = waves.Length;
        }

        if (allWavesFinished)
        {
            return waveCount;
        }

        return Mathf.Min(currentWaveIndex + 1, waveCount);
    }

    public bool IsAllWavesFinished()
    {
        return allWavesFinished;
    }
}
