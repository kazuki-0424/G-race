using System;
using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// プレイヤー個別の重力を管理するコンポーネント。
    ///
    /// 【設計方針】
    ///   Physics2D.gravity は変更しない。
    ///   Rigidbody2D.gravityScale = 0 にした上で、
    ///   FixedUpdate 内で AddForce を使って仮想重力を手動適用する。
    ///   これにより、ステージ上の他オブジェクトに影響を与えない。
    ///
    /// 【重力方向の定義】
    ///   GravityDirection = -1 → 通常（下方向へ引力）
    ///   GravityDirection = +1 → 反転（上方向へ引力）
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class GravityController : MonoBehaviour
    {
        // ─── Inspector 設定 ─────────────────────────────────────────
        [Header("Gravity Settings")]
        [Tooltip("重力の強さ（Unity 標準 9.81 より強い値推奨：20〜30）")]
        [SerializeField] private float gravityStrength = 25f;

        [Tooltip("連続入力を防ぐクールタイム（秒）")]
        [SerializeField] private float cooldownTime = 0.3f;

        // ─── プロパティ ─────────────────────────────────────────────
        /// <summary>現在の重力方向。-1=下(通常), +1=上(反転)</summary>
        public int GravityDirection { get; private set; } = -1;

        /// <summary>重力の強さ（PlayerMovement が参照）</summary>
        public float GravityStrength => gravityStrength;

        /// <summary>重力反転が可能な状態か</summary>
        public bool CanFlip => cooldownTimer <= 0f && !gravityDisabled;

        // ─── イベント ───────────────────────────────────────────────
        /// <summary>重力が反転した際に発火。引数は新しい GravityDirection。</summary>
        public event Action<int> OnGravityFlipped;

        // ─── 内部変数 ───────────────────────────────────────────────
        private float cooldownTimer = 0f;
        private bool gravityDisabled = false; // GravityDisabledZone で使用
        private Rigidbody2D rb;
        private InputHandler inputHandler;

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            // Unity の組み込み重力を無効化し、手動で重力を制御する
            rb.gravityScale = 0f;
        }

        private void Start()
        {
            inputHandler = InputHandler.Instance;
            if (inputHandler != null)
                inputHandler.OnGravityFlip += TryFlipGravity;
        }

        private void OnDestroy()
        {
            if (inputHandler != null)
                inputHandler.OnGravityFlip -= TryFlipGravity;
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            ApplyCustomGravity();
        }

        // ─── 重力適用 ────────────────────────────────────────────────
        /// <summary>
        /// 毎フレーム AddForce で仮想重力を適用する。
        ///   GravityDirection = -1 → force.y = -25（下向き）
        ///   GravityDirection = +1 → force.y = +25（上向き）
        /// </summary>
        private void ApplyCustomGravity()
        {
            Vector2 gravityForce = new Vector2(0f, GravityDirection * gravityStrength);
            rb.AddForce(gravityForce, ForceMode2D.Force);
        }

        // ─── 重力反転 ────────────────────────────────────────────────
        /// <summary>
        /// InputHandler のイベントから呼ばれる。
        /// クールタイム・重力無効エリアを確認してから反転する。
        /// </summary>
        public void TryFlipGravity()
        {
            if (!CanFlip) return;
            FlipGravity();
        }

        private void FlipGravity()
        {
            GravityDirection *= -1;
            cooldownTimer = cooldownTime;

            // SE 再生
            AudioManager.Instance?.PlayGravityFlip();

            // 購読者へ通知（PlayerVisualController, UIManager など）
            OnGravityFlipped?.Invoke(GravityDirection);
        }

        // ─── 外部制御 ────────────────────────────────────────────────
        /// <summary>
        /// GravityDisabledZone（重力障害エリア）から呼ばれる。
        /// true のとき、重力反転を禁止する。
        /// </summary>
        public void SetGravityDisabled(bool disabled)
        {
            gravityDisabled = disabled;
        }

        // ─── 方向ヘルパー ────────────────────────────────────────────
        /// <summary>
        /// 現在の「落下方向」を返す。
        ///   通常重力 → Vector2.down (0, -1)
        ///   反転重力 → Vector2.up   (0, +1)
        /// CircleCast の方向指定に使用する。
        /// </summary>
        public Vector2 GetDownDirection()
        {
            return GravityDirection == -1 ? Vector2.down : Vector2.up;
        }

        /// <summary>
        /// 現在の「起き上がり方向」を返す（GetDownDirection の逆）。
        ///   通常重力 → Vector2.up   (0, +1)
        ///   反転重力 → Vector2.down (0, -1)
        /// 空中での機体傾き補正に使用する。
        /// </summary>
        public Vector2 GetUpDirection()
        {
            return GravityDirection == -1 ? Vector2.up : Vector2.down;
        }
    }
}