using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Result : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI rankText;
    public Button retryButton;

    public ScoreText scoreTextScript;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scoreText.text = scoreTextScript.currentScore.ToString("000");

        if(scoreTextScript.currentScore >= 100)
        {
            rankText.text = "S";
        }
        else if(scoreTextScript.currentScore >= 80)
        {
            rankText.text = "A";
        }
        else if(scoreTextScript.currentScore >= 60)
        {
            rankText.text = "B";
        }
        else if(scoreTextScript.currentScore >= 40)
        {
            rankText.text = "C";
        }
        else
        {
            rankText.text = "D";
        }

        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(() =>
        {
            // リトライボタンがクリックされたときの処理
            // ここでは、シーンをリロードしてゲームを再スタートさせる例を示します。
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
    }
}
