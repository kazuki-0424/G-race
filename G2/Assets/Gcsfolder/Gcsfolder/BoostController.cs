using System;
using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// ブーストゲージ管理と速度補正値の提供を行うコンポーネント。
    ///
    /// 【設計方針】
    ///   PlayerMovement.LimitSpeed() が BoostSpeedMultiplier を参照して
    ///   最高速度を動的に変更する。BoostController 自身は速度を直接変えない。
    ///   これにより 2 クラスの責任を明確に分離する。
    ///
    /// 【ゲージの挙動】
    ///   LShift 押下中：ゲージ消費（0 になると強制終了）
    ///   LShift 離し時：ゲージ回復
    ///   BoostPad 通過：即座にゲージを加算
    /// </summary>
    public class BoostController : MonoBehaviour
    {
        // ─── Inspector 設定 ─────────────────────────────────────────
        [Header("Boost Gauge")]
        [Tooltip("最大ブーストゲージ量")]
        [SerializeField] private float maxBoostGauge = 100f;

        [Tooltip("ブースト中のゲージ消費量（/秒）")]
        [SerializeField] private float consumptionRate = 35f;

        [Tooltip("ブースト停止時のゲージ回復量（/秒）")]
        [SerializeField] private float rechargeRate = 20f;

        [Header("Boost Parameters")]
        [Tooltip("ブースト中の速度倍率（PlayerMovement の maxSpeed に掛ける）")]
        [SerializeField] private float boostSpeedMultiplier = 1.6f;

        [Tooltip("ブースト中の加速力倍率")]
        [SerializeField] private float boostAccelMultiplier = 2f;

        // ─── プロパティ ─────────────────────────────────────────────
        /// <summary>現在ブースト中か（PlayerVisualController, CameraController が参照）</summary>
        public bool IsBoosting { get; private set; } = false;

        /// <summary>ゲージ量（0 〜 maxBoostGauge）</summary>
        public float BoostGauge { get; private set; }

        /// <summary>ゲージの正規化値（0 〜 1）。UI のスライダーに使う。</summary>
        public float BoostNormalized => maxBoostGauge > 0f ? BoostGauge / maxBoostGauge : 0f;

        /// <summary>PlayerMovement.LimitSpeed が参照する速度倍率</summary>
        public float BoostSpeedMultiplier => boostSpeedMultiplier;

        /// <summary>PlayerMovement の加速力補正値</summary>
        public float BoostAccelMultiplier => boostAccelMultiplier;

        // ─── イベント ───────────────────────────────────────────────
        public event Action OnBoostActivated;
        public event Action OnBoostDeactivated;

        // ─── 内部変数 ───────────────────────────────────────────────
        private InputHandler inputHandler;

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Awake()
        {
            BoostGauge = maxBoostGauge; // 開始時はゲージ満タン
        }

        private void Start()
        {
            inputHandler = InputHandler.Instance;
            if (inputHandler != null)
            {
                inputHandler.OnBoostStart += TryStartBoost;
                inputHandler.OnBoostEnd   += StopBoost;
            }
        }

        private void OnDestroy()
        {
            if (inputHandler != null)
            {
                inputHandler.OnBoostStart -= TryStartBoost;
                inputHandler.OnBoostEnd   -= StopBoost;
            }
        }

        // ─── 更新 ────────────────────────────────────────────────────
        private void Update()
        {
            if (IsBoosting)
            {
                BoostGauge -= consumptionRate * Time.deltaTime;
                if (BoostGauge <= 0f)
                {
                    BoostGauge = 0f;
                    StopBoost(); // ゲージ切れで強制終了
                }
            }
            else
            {
                // ゲージ回復（最大値を超えない）
                BoostGauge = Mathf.Min(BoostGauge + rechargeRate * Time.deltaTime, maxBoostGauge);
            }
        }

        // ─── ブースト制御 ────────────────────────────────────────────
        private void TryStartBoost()
        {
            if (IsBoosting) return;
            if (BoostGauge <= 0f) return; // ゲージなしは発動不可

            IsBoosting = true;
            AudioManager.Instance?.PlayBoostStart();
            OnBoostActivated?.Invoke();
        }

        private void StopBoost()
        {
            if (!IsBoosting) return;
            IsBoosting = false;
            OnBoostDeactivated?.Invoke();
        }

        // ─── 外部アクセス ────────────────────────────────────────────
        /// <summary>
        /// BoostPad.cs から呼ばれる。ゲージを amount だけ即時補充する。
        /// </summary>
        public void AddBoostFromPad(float amount)
        {
            BoostGauge = Mathf.Min(BoostGauge + amount, maxBoostGauge);
        }

        /// <summary>
        /// デバッグ・チート用：ゲージを最大にする。
        /// </summary>
        public void RefillGauge()
        {
            BoostGauge = maxBoostGauge;
        }
    }
}