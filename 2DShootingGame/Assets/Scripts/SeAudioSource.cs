using UnityEngine;

/// <summary>
/// 効果音を再生するためのクラス。
/// シーンに1つ置き、Player や Enemy などから共通で使用します。
/// </summary>
public class SeAudioSource : MonoBehaviour
{
    AudioSource audioSource;

    void Start()
    {
        // 同じ GameObject に付いている AudioSource を取得する
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 効果音を再生するメソッド。
    /// 各クラスから FindObjectOfType などで取得したインスタンスに対して呼び出します。
    /// </summary>
    public void PlaySE(AudioClip clip)
    {
        if (clip == null)
        {
            // Inspector で AudioClip が設定されていないことを Console に知らせる
            Debug.LogError("効果音が設定されていません");
            return;
        }

        // PlayOneShot は、再生中の効果音を止めずに別の短い効果音を重ねて鳴らせる
        audioSource.PlayOneShot(clip);
    }
}
