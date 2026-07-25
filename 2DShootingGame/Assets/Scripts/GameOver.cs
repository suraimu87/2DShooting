using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ゲームオーバー画面。
/// </summary>
public class GameOver : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI rankText;

    [Header("リトライボタン")]
    public Button retryButton;

    [Header("プレイヤー")]
    public Player player;

    public ScoreText scoreTextScript;

    void Start()
    {
        // GameOver オブジェクトが非表示から表示に切り替わったとき、Start が1回だけ呼ばれる

        // Player を無効にすると Player.OnDisable も呼ばれ、移動・攻撃の入力が止まる
        player.enabled = false;

        if (scoreText != null && scoreTextScript != null)
        {
            // 000 の書式にすると、10点は「010」のように3桁で表示される
            scoreText.text = scoreTextScript.currentScore.ToString("000");

            // 最終スコアの範囲に応じて、結果画面に表示するランクを変える
            if (scoreTextScript.currentScore >= 100)
            {
                rankText.text = "すごい";
            }
            else if (scoreTextScript.currentScore >= 80)
            {
                rankText.text = "ふつう";
            }
            else if (scoreTextScript.currentScore >= 60)
            {
                rankText.text = "がんばろう";
            }
        }

        if (retryButton != null)
        {
            // コントローラーですぐ決定できるよう、RetryButton を最初から選択状態にする
            if (EventSystem.current != null)
            {
                // 一度選択を外してから設定し直すと、確実に選択状態を更新できる
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
            }

            // スクリプトから以前登録された処理を外してから、リトライ処理を登録する
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() =>{
                // 現在のシーンを読み直し、スコアや敵などを最初の状態に戻す
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });
        }
    }
}
