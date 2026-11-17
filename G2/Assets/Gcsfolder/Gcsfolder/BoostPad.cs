using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// ブーストパネル（踏むとブーストゲージを即補充する床/天井アイテム）。
    ///
    /// 【Inspector 設定】
    ///   - BoxCollider2D を追加、IsTrigger = true
    ///   - padRenderer に SpriteRenderer を設定すると使用時に色が変わる
    ///   - Layer: Default（Ground レイヤーには含めない）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BoostPad : MonoBehaviour
    {
        [Header("Boost Pad Settings")]
        [Tooltip("踏んだ時に補充するブーストゲージ量")]
        [SerializeField] private float boostAmount = 60f;

        [Tooltip("再アクティブ化までのクールダウン（秒）")]
        [SerializeField] private float cooldownDuration = 3f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer padRenderer;
        [SerializeField] private Color activeColor   = new Color(1.0f, 0.8f, 0.0f, 0.9f); // ゴールド
        [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // グレー

        // ─── 内部変数 ───────────────────────────────────────────────
        private bool isActive = true;
        private float cooldownTimer = 0f;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (padRenderer != null) padRenderer.color = activeColor;
        }

        private void Update()
        {
            if (!isActive)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f) Reactivate();

                // パルスアニメーション（非アクティブ時に点滅）
                if (padRenderer != null)
                {
                    float alpha = Mathf.PingPong(Time.time * 2f, 0.4f);
                    Color c = inactiveColor;
                    c.a = alpha;
                    padRenderer.color = c;
                }
            }
            else
            {
                // アクティブ時：輝き波アニメーション
                if (padRenderer != null)
                {
                    float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * 4f);
                    Color c = activeColor;
                    c.a = alpha;
                    padRenderer.color = c;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;
            if (!other.CompareTag("Player")) return;

            BoostController bc = other.GetComponent<BoostController>();
            if (bc != null) bc.AddBoostFromPad(boostAmount);

            Deactivate();
        }

        private void Deactivate()
        {
            isActive = false;
            cooldownTimer = cooldownDuration;
        }

        private void Reactivate()
        {
            isActive = true;
            if (padRenderer != null) padRenderer.color = activeColor;
        }
    }
}