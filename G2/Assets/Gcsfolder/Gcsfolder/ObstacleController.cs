using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// 障害物の動作を制御するコンポーネント。
    /// 3 種類の障害物タイプを 1 クラスで管理する。
    ///
    /// 【Static】
    ///   固定障害物。衝突するとプレイヤーの速度を減少させる。
    ///   Collider2D を通常の Collider（IsTrigger=false）として使用。
    ///
    /// 【Moving】
    ///   Sin 波で上下 or 左右に往復する障害物。
    ///
    /// 【LaserGate】
    ///   一定間隔で Collider と SpriteRenderer を ON/OFF する。
    ///   Collider は IsTrigger=false にして衝突を発生させる。
    ///
    /// 【Inspector 設定】
    ///   - obstacleType で種別を選ぶ
    ///   - Layer: Obstacle（Ground レイヤーとは分けることを推奨）
    ///   - Static/Moving: Collider2D (IsTrigger=false) を追加
    ///   - LaserGate: laserRenderer と laserCollider を設定
    /// </summary>
    public class ObstacleController : MonoBehaviour
    {
        // ─── 障害物タイプ ────────────────────────────────────────────
        public enum ObstacleType { Static, Moving, LaserGate }

        [Header("Type")]
        [SerializeField] private ObstacleType obstacleType = ObstacleType.Static;

        // ─── 衝突時ペナルティ ────────────────────────────────────────
        [Header("Collision Penalty（Static / Moving 共通）")]
        [Tooltip("衝突時に速度を何 % 低下させるか（0.5 = 50% 減）")]
        [SerializeField] private float speedPenaltyPercent = 0.5f;

        // ─── Moving 設定 ─────────────────────────────────────────────
        [Header("Moving Settings")]
        [Tooltip("往復する方向 (Vector2.up = 上下、Vector2.right = 左右)")]
        [SerializeField] private Vector2 moveAxis = Vector2.up;
        [Tooltip("往復速度（周波数 / 秒）")]
        [SerializeField] private float moveFrequency = 1f;
        [Tooltip("往復幅（片道距離）")]
        [SerializeField] private float moveAmplitude = 2f;

        // ─── LaserGate 設定 ──────────────────────────────────────────
        [Header("LaserGate Settings")]
        [Tooltip("レーザーが ON の時間（秒）")]
        [SerializeField] private float onDuration = 1.5f;
        [Tooltip("レーザーが OFF の時間（秒）")]
        [SerializeField] private float offDuration = 0.8f;
        [Tooltip("レーザーの SpriteRenderer（ON/OFF で可視化）")]
        [SerializeField] private SpriteRenderer laserRenderer;
        [Tooltip("レーザーの Collider2D（ON/OFF で有効化）")]
        [SerializeField] private Collider2D laserCollider;

        // ─── 内部変数 ───────────────────────────────────────────────
        private Vector2 originPosition;
        private float moveTimer = 0f;
        private float laserTimer = 0f;
        private bool laserIsOn = true;

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Start()
        {
            originPosition = transform.position;

            // LaserGate 初期状態を ON に設定
            if (obstacleType == ObstacleType.LaserGate)
                SetLaserState(true);
        }

        // ─── 更新 ────────────────────────────────────────────────────
        private void Update()
        {
            switch (obstacleType)
            {
                case ObstacleType.Moving:
                    UpdateMoving();
                    break;
                case ObstacleType.LaserGate:
                    UpdateLaserGate();
                    break;
            }
        }

        private void UpdateMoving()
        {
            moveTimer += Time.deltaTime * moveFrequency * Mathf.PI * 2f;
            float offset = Mathf.Sin(moveTimer) * moveAmplitude;
            transform.position = originPosition + moveAxis.normalized * offset;
        }

        private void UpdateLaserGate()
        {
            laserTimer += Time.deltaTime;
            float threshold = laserIsOn ? onDuration : offDuration;

            if (laserTimer >= threshold)
            {
                laserTimer = 0f;
                laserIsOn = !laserIsOn;
                SetLaserState(laserIsOn);
            }

            // レーザー点滅アニメーション（切り替わり直前にチラツキ）
            if (laserRenderer != null && laserIsOn)
            {
                float remaining = onDuration - laserTimer;
                if (remaining < 0.3f)
                {
                    float alpha = Mathf.PingPong(Time.time * 20f, 1f);
                    Color c = laserRenderer.color;
                    c.a = alpha;
                    laserRenderer.color = c;
                }
                else
                {
                    Color c = laserRenderer.color;
                    c.a = 1f;
                    laserRenderer.color = c;
                }
            }
        }

        private void SetLaserState(bool on)
        {
            if (laserRenderer != null) laserRenderer.enabled = on;
            if (laserCollider  != null) laserCollider.enabled = on;
        }

        // ─── 衝突時ペナルティ ────────────────────────────────────────
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.collider.CompareTag("Player")) return;

            PlayerMovement pm = collision.collider.GetComponent<PlayerMovement>();
            if (pm == null) return;

            Rigidbody2D rb = pm.GetRigidbody();
            if (rb != null)
            {
                // 速度を speedPenaltyPercent 分だけ低下させる
                rb.linearVelocity *= (1f - Mathf.Clamp01(speedPenaltyPercent));
            }

            AudioManager.Instance?.PlayCollision();
        }
    }

    // =========================================================================
    /// <summary>
    /// 重力障害エリア。このトリガーに入ると重力反転が使用不可になる。
    ///
    /// 【Inspector 設定】
    ///   - BoxCollider2D を追加して IsTrigger = true
    ///   - Layer: Default（Ground に含めない）
    ///   - SpriteRenderer で半透明の赤エリアとして可視化する
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GravityDisabledZone : MonoBehaviour
    {
        [Header("Zone Visual")]
        [SerializeField] private Color zoneColor = new Color(1f, 0.1f, 0.1f, 0.25f);

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = zoneColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            GravityController gc = other.GetComponent<GravityController>();
            gc?.SetGravityDisabled(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            GravityController gc = other.GetComponent<GravityController>();
            gc?.SetGravityDisabled(false);
        }
    }
}