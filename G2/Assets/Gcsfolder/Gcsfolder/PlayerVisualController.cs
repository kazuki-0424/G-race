using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// 機体のビジュアルエフェクト（トレイル・パーティクル・グロー）を管理する。
    ///
    /// 【外部アセット不要の方針】
    ///   Start() 時にコードで TrailRenderer と ParticleSystem を生成する。
    ///   これにより、仮素材でも即座に動作確認できる。
    ///   将来的には Inspector から本アセットを差し替えればよい。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerVisualController : MonoBehaviour
    {
        // ─── Inspector 設定 ─────────────────────────────────────────
        [Header("Body Colors")]
        [Tooltip("通常時の機体カラー（シアン系）")]
        [SerializeField] private Color normalBodyColor = new Color(0.0f, 0.85f, 1.0f, 1.0f);

        [Tooltip("ブースト時の機体カラー（マゼンタ系）")]
        [SerializeField] private Color boostBodyColor = new Color(1.0f, 0.2f, 0.8f, 1.0f);

        [Header("Trail（コードで自動生成。外部アセットに差し替え可）")]
        [Tooltip("自動生成せず外部の TrailRenderer を使う場合はここに設定")]
        [SerializeField] private TrailRenderer externalTrail;

        [Header("Particles（コードで自動生成）")]
        [Tooltip("外部の重力反転 ParticleSystem を使う場合はここに設定")]
        [SerializeField] private ParticleSystem externalGravityFlipParticles;
        [SerializeField] private ParticleSystem externalSpeedLinesParticles;

        // ─── 内部変数 ───────────────────────────────────────────────
        private SpriteRenderer bodyRenderer;
        private GravityController gravityController;
        private BoostController boostController;
        private PlayerMovement playerMovement;

        private TrailRenderer engineTrail;
        private ParticleSystem gravityFlipParticles;
        private ParticleSystem speedLinesParticles;

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Awake()
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            gravityController = GetComponent<GravityController>();
            boostController = GetComponent<BoostController>();
            playerMovement = GetComponent<PlayerMovement>();
        }

        private void Start()
        {
            // ── TrailRenderer を準備 ──
            engineTrail = externalTrail != null ? externalTrail : CreateTrailRenderer();

            // ── ParticleSystem を準備 ──
            gravityFlipParticles  = externalGravityFlipParticles  != null
                ? externalGravityFlipParticles
                : CreateGravityFlipParticles();

            speedLinesParticles   = externalSpeedLinesParticles != null
                ? externalSpeedLinesParticles
                : CreateSpeedLinesParticles();

            // ── イベント購読 ──
            if (gravityController != null)
                gravityController.OnGravityFlipped += OnGravityFlipped;
            if (boostController != null)
            {
                boostController.OnBoostActivated   += OnBoostActivated;
                boostController.OnBoostDeactivated += OnBoostDeactivated;
            }
        }

        private void OnDestroy()
        {
            if (gravityController != null)
                gravityController.OnGravityFlipped -= OnGravityFlipped;
            if (boostController != null)
            {
                boostController.OnBoostActivated   -= OnBoostActivated;
                boostController.OnBoostDeactivated -= OnBoostDeactivated;
            }
        }

        // ─── 毎フレーム更新 ─────────────────────────────────────────
        private void Update()
        {
            UpdateBodyGlow();
            UpdateTrail();
            UpdateSpeedLines();
        }

        private void UpdateBodyGlow()
        {
            if (bodyRenderer == null) return;
            bool boosting = boostController != null && boostController.IsBoosting;
            Color target = boosting ? boostBodyColor : normalBodyColor;
            bodyRenderer.color = Color.Lerp(bodyRenderer.color, target, 10f * Time.deltaTime);
        }

        private void UpdateTrail()
        {
            if (engineTrail == null) return;
            float speed = playerMovement != null ? playerMovement.CurrentSpeed : 0f;
            bool boosting = boostController != null && boostController.IsBoosting;

            engineTrail.emitting = speed > 0.5f;
            float targetTime = boosting ? 0.45f : Mathf.Lerp(0.05f, 0.25f, speed / 18f);
            engineTrail.time = Mathf.Lerp(engineTrail.time, targetTime, 5f * Time.deltaTime);
            engineTrail.startWidth = boosting ? 0.45f : 0.2f;
        }

        private void UpdateSpeedLines()
        {
            if (speedLinesParticles == null) return;
            float speed = playerMovement != null ? playerMovement.CurrentSpeed : 0f;
            var emission = speedLinesParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 60f, speed / 18f);
        }

        // ─── イベントハンドラ ────────────────────────────────────────
        private void OnGravityFlipped(int direction)
        {
            if (gravityFlipParticles != null)
            {
                gravityFlipParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                gravityFlipParticles.Play();
            }

            // 機体を一瞬フラッシュさせる
            if (bodyRenderer != null)
                StartCoroutine(FlashCoroutine());
        }

        private void OnBoostActivated()
        {
            // ブースト開始時にトレイルを太くする（UpdateTrail で継続更新）
        }

        private void OnBoostDeactivated()
        {
            // ブースト終了後にトレイルが細くなる（UpdateTrail で継続更新）
        }

        // ─── フラッシュ演出 ──────────────────────────────────────────
        private System.Collections.IEnumerator FlashCoroutine()
        {
            Color original = bodyRenderer.color;
            bodyRenderer.color = Color.white;
            yield return new WaitForSeconds(0.07f);
            bodyRenderer.color = original;
        }

        // ─── コードでエフェクト生成 ──────────────────────────────────
        /// <summary>
        /// エンジントレイルを生成する（外部アセット不要）。
        /// 機体の後方 (-0.6, 0, 0) の子 GameObject に TrailRenderer を追加する。
        /// </summary>
        private TrailRenderer CreateTrailRenderer()
        {
            GameObject trailGO = new GameObject("EngineTrail");
            trailGO.transform.SetParent(transform);
            trailGO.transform.localPosition = new Vector3(-0.6f, 0f, 0f);

            TrailRenderer trail = trailGO.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.startWidth = 0.3f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            // グラデーション設定（シアン → 紫 → 透明）
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0f, 0.9f, 1f), 0f),
                    new GradientColorKey(new Color(0.6f, 0f, 1f), 0.6f),
                    new GradientColorKey(new Color(0.3f, 0f, 0.5f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.5f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = gradient;

            // URP 環境用マテリアル。Sprites/Default は全 URP プロジェクトで利用可能
            trail.material = new Material(Shader.Find("Sprites/Default"));

            return trail;
        }

        /// <summary>
        /// 重力反転時のバーストパーティクルを生成する（外部アセット不要）。
        /// </summary>
        private ParticleSystem CreateGravityFlipParticles()
        {
            GameObject psGO = new GameObject("GravityFlipParticles");
            psGO.transform.SetParent(transform);
            psGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = psGO.AddComponent<ParticleSystem>();
            var renderer = psGO.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 5;

            var main = ps.main;
            main.loop = false;
            main.duration = 0.4f;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0f, 0.9f, 1f),
                new Color(1f, 0.2f, 0.9f)
            );
            main.maxParticles = 40;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 40)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.4f;

            ps.Stop();
            return ps;
        }

        /// <summary>
        /// 速度に応じたスピードライン（後流）パーティクルを生成する。
        /// </summary>
        private ParticleSystem CreateSpeedLinesParticles()
        {
            GameObject psGO = new GameObject("SpeedLinesParticles");
            psGO.transform.SetParent(transform);
            psGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = psGO.AddComponent<ParticleSystem>();
            var renderer = psGO.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 3;

            var main = ps.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(0.6f, 0.9f, 1f, 0.5f);
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f; // Update() で動的に変更

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-8f, -20f); // 後方へ
            vel.y = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

            ps.Play();
            return ps;
        }
    }
}