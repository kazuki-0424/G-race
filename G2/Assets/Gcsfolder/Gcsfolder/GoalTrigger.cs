// ===== GoalTrigger.cs =====
using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// ゴール判定トリガー。
    /// "Player" タグの Collider2D が触れると RaceManager.OnGoalReached() を呼ぶ。
    ///
    /// 【Inspector 設定】
    ///   - BoxCollider2D を追加して IsTrigger = true
    ///   - Goal Layer または Default レイヤー（Ground には含めないこと）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalTrigger : MonoBehaviour
    {
        [Header("Goal Effect")]
        [SerializeField] private ParticleSystem goalParticles;

        private bool triggered = false;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;
            if (!other.CompareTag("Player")) return;

            triggered = true;

            if (goalParticles != null) goalParticles.Play();

            RaceManager.Instance?.OnGoalReached();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawWireCube(transform.position, transform.localScale);
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "GOAL");
            #endif
        }
    }
}