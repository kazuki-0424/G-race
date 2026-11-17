using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// プレイヤーの移動・接地判定・法線回転を管理するコンポーネント。
    ///
    /// 【移動設計】
    ///   A/D キーで左右に加速・減速する。
    ///   接地中は接触面の接線方向（Tangent）に AddForce する。
    ///   空中は X 軸方向に制限して制御性を維持する。
    ///
    /// 【接地判定】
    ///   Physics2D.CircleCast で重力方向に向けてキャストし、
    ///   HitInfo.normal から接触面の法線を取得する。
    ///   Wall レイヤーは GroundMask から除外して壁張り付きを防ぐ。
    ///
    /// 【法線回転】
    ///   接地中 → 接触面法線に Slerp で機体を整列させる。
    ///   空中中 → 重力の「上方向」に緩やかに戻す。
    ///
    /// 【Unity 6 API 対応】
    ///   velocity   → linearVelocity
    ///   drag       → linearDamping
    ///   angularDrag → angularDamping
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(GravityController))]
    public class PlayerMovement : MonoBehaviour
    {
        // ─── Inspector 設定：移動 ────────────────────────────────────
        [Header("Movement")]
        [Tooltip("加速力（大きいほど素早く加速）")]
        [SerializeField] private float acceleration = 25f;

        [Tooltip("入力なし時の減速力（大きいほど素早く止まる）")]
        [SerializeField] private float decelerationForce = 20f;

        [Tooltip("通常時の最高速度 (m/s)")]
        [SerializeField] private float maxSpeed = 18f;

        [Tooltip("空中での横入力の効き具合（0〜1）")]
        [SerializeField] private float airControlMultiplier = 0.4f;

        // ─── Inspector 設定：接地判定 ────────────────────────────────
        [Header("Ground Detection")]
        [Tooltip("CircleCast のキャスト半径（Collider 半径の 0.9〜0.95 倍が目安）")]
        [SerializeField] private float groundCheckRadius = 0.28f;

        [Tooltip("CircleCast の距離（0.05〜0.2 程度）")]
        [SerializeField] private float groundCheckDistance = 0.12f;

        [Tooltip("接地判定に使うレイヤー（Ground レイヤーを設定）")]
        [SerializeField] private LayerMask groundMask;

        // ─── Inspector 設定：回転 ────────────────────────────────────
        [Header("Rotation")]
        [Tooltip("接地時の法線追従速度（大きいほど機敏）")]
        [SerializeField] private float rotationSpeed = 12f;

        [Tooltip("空中での重力上方向への補正速度")]
        [SerializeField] private float airRotationSpeed = 4f;

        // ─── プロパティ ─────────────────────────────────────────────
        /// <summary>接地中か</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>現在の横方向速度（絶対値）</summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>現在の Velocity.x（符号付き）</summary>
        public float HorizontalVelocity => rb.linearVelocity.x;

        /// <summary>接触面の法線（空中時は GravityUp 方向）</summary>
        public Vector2 SurfaceNormal { get; private set; } = Vector2.up;

        // ─── 内部変数 ───────────────────────────────────────────────
        private Rigidbody2D rb;
        private GravityController gravityController;
        private BoostController boostController; // null でも動作可
        private InputHandler inputHandler;
        private float currentRotationAngle = 0f;
        private Vector2 targetNormal = Vector2.up;

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            gravityController = GetComponent<GravityController>();
            boostController = GetComponent<BoostController>();

            // Unity 6 プロパティ名で設定
            rb.gravityScale = 0f;                                           // 個別重力を使用
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 高速すり抜け防止
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;        // 滑らか描画
            rb.angularDamping = 20f;                                        // 回転の揺れ抑制
            rb.linearDamping = 1f;                                          // 空気抵抗
            rb.constraints = RigidbodyConstraints2D.None;                   // 回転は許可（スクリプト制御）
        }

        private void Start()
        {
            inputHandler = InputHandler.Instance;
        }

        // ─── 物理更新 ────────────────────────────────────────────────
        private void FixedUpdate()
        {
            CheckGround();
            HandleMovement();
            LimitSpeed();
            UpdateRotation();

            CurrentSpeed = Mathf.Abs(rb.linearVelocity.x);
        }

        // ─── 接地判定 ────────────────────────────────────────────────
        private void CheckGround()
        {
            Vector2 downDir = gravityController.GetDownDirection();
            Vector2 origin = (Vector2)transform.position;

            RaycastHit2D hit = Physics2D.CircleCast(
                origin,
                groundCheckRadius,
                downDir,
                groundCheckDistance,
                groundMask
            );

            if (hit.collider != null)
            {
                IsGrounded = true;
                SurfaceNormal = hit.normal;
                targetNormal = hit.normal;
            }
            else
            {
                IsGrounded = false;
                // 空中では重力の「上方向」を目標法線にする
                targetNormal = gravityController.GetUpDirection();
                SurfaceNormal = targetNormal;
            }
        }

        // ─── 移動処理 ────────────────────────────────────────────────
        private void HandleMovement()
        {
            float input = inputHandler != null ? inputHandler.HorizontalInput : 0f;
            float controlMult = IsGrounded ? 1f : airControlMultiplier;

            if (Mathf.Abs(input) > 0.01f)
            {
                // 移動方向を決定（接地中は接線方向、空中は水平）
                Vector2 moveDir = IsGrounded
                    ? GetSurfaceTangent(input)
                    : new Vector2(input, 0f);

                rb.AddForce(moveDir * acceleration * controlMult, ForceMode2D.Force);
            }
            else if (IsGrounded)
            {
                // 入力なし＋接地中：水平方向に減速力を加える
                float vx = rb.linearVelocity.x;
                rb.AddForce(new Vector2(-vx * decelerationForce, 0f), ForceMode2D.Force);
            }
            // 空中・入力なし：linearDamping が自然減速を担う
        }

        /// <summary>
        /// 接地面の「右向き接線ベクトル」を返す。
        /// 天井（法線が下向き）など全てのケースで、
        /// input の符号と一致する水平方向を保証する。
        ///
        ///   法線 (0, +1)  → tangent = (+1,  0) ✓ 床
        ///   法線 (0, -1)  → tangent = (+1,  0) ✓ 天井（符号反転補正）
        ///   法線 (-√2, +√2) → tangent = (+√2, +√2) ✓ 右上がり坂
        /// </summary>
        private Vector2 GetSurfaceTangent(float input)
        {
            // 法線の右向き垂直ベクトル
            Vector2 tangent = new Vector2(SurfaceNormal.y, -SurfaceNormal.x);

            // 接線の水平成分が input と逆向きになる場合は反転する
            // （天井面などで進行方向が逆になるのを防ぐ）
            if (tangent.x * input < -0.01f)
                tangent = -tangent;

            return tangent.normalized * Mathf.Abs(input);
        }

        // ─── 速度制限 ────────────────────────────────────────────────
        private void LimitSpeed()
        {
            float limit = GetCurrentMaxSpeed();
            Vector2 vel = rb.linearVelocity;

            if (Mathf.Abs(vel.x) > limit)
            {
                vel.x = Mathf.Sign(vel.x) * limit;
                rb.linearVelocity = vel;
            }
        }

        private float GetCurrentMaxSpeed()
        {
            if (boostController != null && boostController.IsBoosting)
                return maxSpeed * boostController.BoostSpeedMultiplier;
            return maxSpeed;
        }

        // ─── 回転（法線整列） ────────────────────────────────────────
        private void UpdateRotation()
        {
            // 目標角度：Vector2.up（機体のローカル上方向）を targetNormal に合わせる
            float targetAngle = Vector2.SignedAngle(Vector2.up, targetNormal);

            float speed = IsGrounded ? rotationSpeed : airRotationSpeed;

            // LerpAngle で 180°/-180° の折り返しを正しく処理する
            currentRotationAngle = Mathf.LerpAngle(
                currentRotationAngle,
                targetAngle,
                speed * Time.fixedDeltaTime
            );

            rb.MoveRotation(currentRotationAngle);
        }

        // ─── 外部アクセス ────────────────────────────────────────────
        /// <summary>
        /// BoostController や外部から最高速度を上書きする。
        /// </summary>
        public void SetMaxSpeed(float newMax) => maxSpeed = newMax;

        /// <summary>
        /// Rigidbody2D への直接アクセス（ObstacleController の衝突処理で使用）。
        /// </summary>
        public Rigidbody2D GetRigidbody() => rb;

        // ─── デバッグ描画 ────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            if (gravityController == null) return;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, groundCheckRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, gravityController.GetDownDirection() * (groundCheckRadius + groundCheckDistance));
        }
    }
}