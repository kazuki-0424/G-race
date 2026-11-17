/*using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// チェックポイントトリガー。
    /// "Player" タグの Collider2D が触れると RaceManager に通知し、表示を切り替える。
    ///
    /// 【Inspector 設定】
    ///   - checkpointIndex: 0 始まりの番号を通過順に振る
    ///   - このオブジェクトに BoxCollider2D (IsTrigger=true) を追加する
    ///   - Layer: Default または専用レイヤー（Ground には含めないこと）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        [Tooltip("このチェックポイントの通過順番号（0始まり）")]
        [SerializeField] private int checkpointIndex = 0;

        [Header("Visual Feedback")]
        [Tooltip("通過前の色（緑系推奨）")]
        [SerializeField] private Color activeColor = new Color(0f, 1f, 0.5f, 0.4f);
        [Tooltip("通過後の色（灰色推奨）")]
        [SerializeField] private Color passedColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        [Tooltip("SpriteRenderer でゲートを可視化する場合に設定")]
        [SerializeField] private SpriteRenderer gateRenderer;

        private bool hasPassed = false;

        private void Awake()
        {
            // IsTrigger を強制設定
            GetComponent<Collider2D>().isTrigger = true;

            if (gateRenderer != null)
                gateRenderer.color = activeColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasPassed) return;
            if (!other.CompareTag("Player")) return;

            hasPassed = true;

            // 色を通過済みに変更
            if (gateRenderer != null)
                gateRenderer.color = passedColor;

            // RaceManager に通知
            RaceManager.Instance?.OnCheckpointReached(checkpointIndex);
        }

        // エディタでの視覚的確認用
        private void OnDrawGizmos()
        {
            Gizmos.color = hasPassed ? passedColor : activeColor;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"CP[{checkpointIndex}]");
        }
    }
}*/