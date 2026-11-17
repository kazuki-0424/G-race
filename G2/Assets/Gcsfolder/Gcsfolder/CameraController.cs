using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// プレイヤーを追従するカスタムカメラコントローラ。
    /// Cinemachine を使わずに実装しているため、追加パッケージが不要。
    ///
    /// 【機能一覧】
    ///   - 進行方向への動的ルックアヘッドオフセット
    ///   - X・Y 追従速度を分離した滑らかな追従
    ///   - 重力反転後の Y 揺れを抑制するダンパリング
    ///   - ブースト時の視野拡大（Orthographic Size 変更）
    ///   - 速度に応じたフォーカスシフト
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        // ─── Inspector 設定 ─────────────────────────────────────────
        [Header("Target")]
        [Tooltip("追従するターゲット（Player）。未設定時は 'Player' タグで自動検索。")]
        [SerializeField] private Transform target;

        [Header("Look Ahead")]
        [Tooltip("進行方向への先読みオフセット基準距離")]
        [SerializeField] private float lookAheadBase = 5f;

        [Tooltip("速度 maxSpeed 時の先読み最大距離")]
        [SerializeField] private float lookAheadMax = 10f;

        [Tooltip("先読みオフセットの追従速度")]
        [SerializeField] private float lookAheadSmoothSpeed = 3f;

        [Header("Follow Speed")]
        [Tooltip("X 追従速度（大きいほど機敏）")]
        [SerializeField] private float followSpeedX = 6f;

        [Tooltip("Y 追従速度（重力反転時の揺れを抑えるため低めに）")]
        [SerializeField] private float followSpeedY = 3f;

        [Tooltip("ブースト中の追従速度倍率")]
        [SerializeField] private float boostFollowMultiplier = 1.5f;

        [Header("Orthographic Size（視野）")]
        [Tooltip("通常時の Orthographic Size")]
        [SerializeField] private float normalSize = 10f;

        [Tooltip("ブースト時の Orthographic Size（広がって臨場感 UP）")]
        [SerializeField] private float boostSize = 13f;

        [Tooltip("ズーム変化速度")]
        [SerializeField] private float zoomSpeed = 3f;

        [Header("Camera Z")]
        [Tooltip("カメラの Z 位置（2D では -10 推奨）")]
        [SerializeField] private float cameraZ = -10f;

        // ─── 内部変数 ───────────────────────────────────────────────
        private Camera cam;
        private PlayerMovement playerMovement;
        private BoostController boostController;

        private float currentLookAheadX = 0f;
        private float targetLookAheadX = 0f;

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("[CameraController] Camera コンポーネントが見つかりません。Main Camera に追加してください。");
            }
        }

        private void Start()
        {
            // target が未設定なら 'Player' タグで検索
            if (target == null)
            {
                GameObject playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null) SetTarget(playerGO.transform);
            }
            else
            {
                SetTarget(target);
            }

            // 開始時にカメラを即座にターゲット位置に配置
            if (target != null)
            {
                Vector3 startPos = target.position;
                startPos.z = cameraZ;
                transform.position = startPos;
            }
        }

        // ─── カメラ更新（LateUpdate で描画後に実行） ──────────────
        private void LateUpdate()
        {
            if (target == null) return;

            float speed  = playerMovement != null ? playerMovement.CurrentSpeed : 0f;
            float velX   = playerMovement != null ? playerMovement.HorizontalVelocity : 0f;
            bool boosting = boostController != null && boostController.IsBoosting;

            // ── ルックアヘッドオフセットを計算 ──
            float speedRatio = Mathf.Clamp01(speed / 18f);
            float lookDir    = velX >= 0f ? 1f : -1f;
            targetLookAheadX = lookDir * Mathf.Lerp(lookAheadBase, lookAheadMax, speedRatio);
            currentLookAheadX = Mathf.Lerp(
                currentLookAheadX, targetLookAheadX,
                lookAheadSmoothSpeed * Time.deltaTime
            );

            // ── 追従速度を計算（ブースト中は速く） ──
            float multip = boosting ? boostFollowMultiplier : 1f;
            float spdX = followSpeedX * multip * Time.deltaTime;
            float spdY = followSpeedY * multip * Time.deltaTime;

            // ── 目標位置 ──
            Vector3 targetPos = new Vector3(
                target.position.x + currentLookAheadX,
                target.position.y,
                cameraZ
            );

            // ── 滑らかに移動 ──
            Vector3 current = transform.position;
            current.x = Mathf.Lerp(current.x, targetPos.x, spdX);
            current.y = Mathf.Lerp(current.y, targetPos.y, spdY);
            current.z = cameraZ;
            transform.position = current;

            // ── Orthographic Size（ズーム） ──
            if (cam != null)
            {
                float targetSize = boosting ? boostSize : normalSize;
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
            }
        }

        // ─── 外部アクセス ────────────────────────────────────────────
        /// <summary>
        /// ターゲットを切り替える（マルチプレイヤー展開時に使用）。
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                playerMovement = target.GetComponent<PlayerMovement>();
                boostController = target.GetComponent<BoostController>();
            }
        }

        /// <summary>
        /// カメラをターゲット位置に即座にワープさせる（リスタート後などに使用）。
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 pos = target.position;
            pos.z = cameraZ;
            transform.position = pos;
            currentLookAheadX = 0f;
        }
    }
}