using UnityEngine;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// スコアを表示するテキストを管理するスクリプト。
/// </summary>
public class ScoreText : MonoBehaviour
{
    [Header("スコアを表示するテキスト")]
    TextMeshProUGUI scoreText;

    // 実際のスコア値。Enemy が撃破されたときに増え、結果画面のランク判定にも使う
    public int currentScore = 0;

    // TextMeshPro に表示するための文字列
    string scoreTextString = "0";

    void Start()
    {
        // スコアテキストを取得
        scoreText = GetComponent<TextMeshProUGUI>();

        // 000 の書式を使い、ゲーム開始時は「Score:000」と表示する
        scoreTextString = "Score:" + (int.Parse(scoreTextString) + 0).ToString("000");
        scoreText.text = scoreTextString;
    }

    /// <summary>
    /// Enemy の撃破処理から呼ばれ、スコアの値と画面表示を更新する。
    /// </summary>
    public void AddScore(int score)
    {
        // スコアを増やす
        currentScore += score;

        // 数値を3桁の文字列に直し、最新のスコアを画面へ表示する
        scoreTextString = "Score:" + currentScore.ToString("000");
        scoreText.text = scoreTextString;
    }
}
