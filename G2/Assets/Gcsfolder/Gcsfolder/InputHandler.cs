using System;
using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// 全キー入力を一元管理するシングルトン。
    /// 各システムはこのクラスのイベントを購読して入力を受け取る。
    /// DontDestroyOnLoad でシーンをまたいで維持される。
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        // ─── シングルトン ───────────────────────────────────────────
        public static InputHandler Instance { get; private set; }

        // ─── イベント ───────────────────────────────────────────────
        /// <summary>Space キー押下：重力反転要求</summary>
        public event Action OnGravityFlip;

        /// <summary>Left Shift 押下：ブースト開始</summary>
        public event Action OnBoostStart;

        /// <summary>Left Shift 離し：ブースト終了</summary>
        public event Action OnBoostEnd;

        /// <summary>R キー押下：リスタート要求</summary>
        public event Action OnRestart;

        /// <summary>Esc キー押下：ポーズ切り替え</summary>
        public event Action OnPause;

        // ─── プロパティ ─────────────────────────────────────────────
        /// <summary>水平入力値 (-1 〜 +1)。A/D または 十字キー左右。</summary>
        public float HorizontalInput { get; private set; }

        /// <summary>Left Shift を押し続けているか</summary>
        public bool IsBoostHeld { get; private set; }

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
        }

        private void Update()
        {
            // A/D または 矢印キー（GetAxisRaw で即応性を確保）
            HorizontalInput = Input.GetAxisRaw("Horizontal");

            // Space: 重力反転
            if (Input.GetKeyDown(KeyCode.Space))
                OnGravityFlip?.Invoke();

            // Left Shift: ブースト
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                IsBoostHeld = true;
                OnBoostStart?.Invoke();
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                IsBoostHeld = false;
                OnBoostEnd?.Invoke();
            }

            // R: リスタート
            if (Input.GetKeyDown(KeyCode.R))
                OnRestart?.Invoke();

            // Esc: ポーズ
            if (Input.GetKeyDown(KeyCode.Escape))
                OnPause?.Invoke();
        }
    }
}
