using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// SE / BGM の再生を一元管理するシングルトン。
    /// AudioClip が未設定 (null) でも null チェックにより安全に動作する。
    /// Inspector で AudioSource × 2 (SFX / BGM) とクリップを設定する。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ─── シングルトン ───────────────────────────────────────────
        public static AudioManager Instance { get; private set; }

        // ─── Inspector 設定 ─────────────────────────────────────────
        [Header("Audio Sources")]
        [Tooltip("SE 用 AudioSource（Loop OFF）")]
        [SerializeField] private AudioSource sfxSource;

        [Tooltip("BGM 用 AudioSource（Loop ON、音量を低めに設定）")]
        [SerializeField] private AudioSource bgmSource;

        [Header("SFX Clips（未設定でも動作可）")]
        [SerializeField] private AudioClip gravityFlipClip;
        [SerializeField] private AudioClip boostStartClip;
        [SerializeField] private AudioClip checkpointClip;
        [SerializeField] private AudioClip goalClip;
        [SerializeField] private AudioClip collisionClip;

        [Header("BGM")]
        [SerializeField] private AudioClip titleBGM;
        [SerializeField] private AudioClip raceBGM;

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSource が Inspector 未設定の場合、自動で追加
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.volume = 0.5f;
            }
        }

        // ─── SE 再生メソッド ────────────────────────────────────────
        /// <summary>重力反転 SE を再生する</summary>
        public void PlayGravityFlip() => PlaySFX(gravityFlipClip);

        /// <summary>ブースト開始 SE を再生する</summary>
        public void PlayBoostStart() => PlaySFX(boostStartClip);

        /// <summary>チェックポイント通過 SE を再生する</summary>
        public void PlayCheckpoint() => PlaySFX(checkpointClip);

        /// <summary>ゴール SE を再生する</summary>
        public void PlayGoal() => PlaySFX(goalClip);

        /// <summary>障害物衝突 SE を再生する</summary>
        public void PlayCollision() => PlaySFX(collisionClip);

        // ─── BGM 再生メソッド ────────────────────────────────────────
        /// <summary>レース BGM を再生する</summary>
        public void PlayRaceBGM() => PlayBGM(raceBGM);

        /// <summary>タイトル BGM を再生する</summary>
        public void PlayTitleBGM() => PlayBGM(titleBGM);

        /// <summary>BGM を停止する</summary>
        public void StopBGM()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        /// <summary>BGM 音量を設定する (0 〜 1)</summary>
        public void SetBGMVolume(float volume)
        {
            if (bgmSource != null) bgmSource.volume = Mathf.Clamp01(volume);
        }

        // ─── 内部ヘルパー ────────────────────────────────────────────
        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

        private void PlayBGM(AudioClip clip)
        {
            if (clip == null || bgmSource == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }
}