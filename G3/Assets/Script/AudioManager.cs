using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // シングルトン
    public static AudioManager Instance { get; private set; }

    [Header("AudioSource")]
    [SerializeField] private AudioSource SE_AudioSource;    // SEを再生するAudioSource

    [Header("SE - 移動")]
    [SerializeField] private AudioClip SE_Move;             // 移動キーを押したとき

    [Header("SE - 重力反転")]
    [SerializeField] private AudioClip SE_Gravity;          // 重力反転したとき

    [Header("SE - 衝突")]
    [SerializeField] private AudioClip SE_Collision;        // 障害物に衝突したとき

    void Awake()
    {
        // シングルトン設定（シーン間で重複しないように）
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 移動SE再生
    public void PlayMove()
    {
        PlaySE(SE_Move);
    }

    // 重力反転SE再生
    public void PlayGravity()
    {
        PlaySE(SE_Gravity);
    }

    // 衝突SE再生
    public void PlayCollision()
    {
        PlaySE(SE_Collision);
    }

    // 共通再生メソッド
    private void PlaySE(AudioClip clip)
    {
        if (SE_AudioSource == null || clip == null) return;
        SE_AudioSource.PlayOneShot(clip);
    }
}
