using UnityEngine;
using UnityEngine.UI;

// TextMeshPro が利用可能な場合は using TMPro; を有効にする
// Unity 6 では Package Manager から "TextMeshPro" を追加してください
#if UNITY_EDITOR || !TMPRO_OPTIONAL
using TMPro;
#endif

namespace GravityRacer
{
    /// <summary>
    /// ゲーム中の HUD・ポーズ・リザルト画面を管理するコンポーネント。
    ///
    /// 【TMPro 未導入の場合】
    ///   TextMeshProUGUI の代わりに Unity 標準 Text を使うことも可能。
    ///   その際は以下の #if ブロックの型を Text に変更してください。
    ///
    /// 【必要な Canvas 構成】
    ///   Canvas (Screen Space - Overlay)
    ///     ├── HUD_Panel
    ///     │    ├── SpeedText         (TextMeshProUGUI)
    ///     │    ├── TimerText         (TextMeshProUGUI)
    ///     │    ├── BestTimeText      (TextMeshProUGUI)
    ///     │    ├── GravityText       (TextMeshProUGUI)
    ///     │    ├── CheckpointText    (TextMeshProUGUI)
    ///     │    └── BoostSlider       (Slider)
    ///     ├── CountdownPanel
    ///     │    └── CountdownText     (TextMeshProUGUI)
    ///     ├── PausePanel
    ///     └── ResultPanel
    ///          ├── ResultTimeText    (TextMeshProUGUI)
    ///          ├── ResultRankText    (TextMeshProUGUI)
    ///          ├── ResultBestText    (TextMeshProUGUI)
    ///          ├── NewBestLabel      (GameObject)
    ///          └── RetryButton       (Button)
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ─── Inspector 設定：HUD ─────────────────────────────────────
        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI bestTimeText;
        [SerializeField] private TextMeshProUGUI gravityText;
        [SerializeField] private TextMeshProUGUI checkpointText;
        [SerializeField] private Slider boostSlider;

        [Header("Countdown")]
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject resultPanel;

        [Header("Result")]
        [SerializeField] private TextMeshProUGUI resultTimeText;
        [SerializeField] private TextMeshProUGUI resultRankText;
        [SerializeField] private TextMeshProUGUI resultBestText;
        [SerializeField] private GameObject newBestLabel;
        [SerializeField] private Button retryButton;

        // ─── 内部変数 ───────────────────────────────────────────────
        private PlayerMovement playerMovement;
        private GravityController gravityController;
        private BoostController boostController;
        private RaceManager raceManager;
        private bool isPaused = false;

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Start()
        {
            // Player コンポーネントを自動取得
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                playerMovement    = playerGO.GetComponent<PlayerMovement>();
                gravityController = playerGO.GetComponent<GravityController>();
                boostController   = playerGO.GetComponent<BoostController>();
            }
            raceManager = RaceManager.Instance;

            // パネル初期状態
            if (pausePanel  != null) pausePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (countdownPanel != null) countdownPanel.SetActive(true);

            // ベストタイム表示
            UpdateBestTimeText();

            // ポーズイベント購読
            if (InputHandler.Instance != null)
                InputHandler.Instance.OnPause += TogglePause;

            // リトライボタン
            if (retryButton != null)
                retryButton.onClick.AddListener(() => raceManager?.RestartRace());
        }

        private void OnDestroy()
        {
            if (InputHandler.Instance != null)
                InputHandler.Instance.OnPause -= TogglePause;
        }

        // ─── 毎フレーム更新 ─────────────────────────────────────────
        private void Update()
        {
            UpdateHUD();
            UpdateCountdown();
        }

        private void UpdateHUD()
        {
            // 速度表示（km/h に換算：×10 は調整値）
            if (speedText != null && playerMovement != null)
            {
                int kmh = Mathf.RoundToInt(playerMovement.CurrentSpeed * 10f);
                speedText.text = $"{kmh} km/h";
            }

            // タイマー表示
            if (timerText != null && raceManager != null)
                timerText.text = RaceManager.FormatTime(raceManager.CurrentTime);

            // ブーストゲージ
            if (boostSlider != null && boostController != null)
                boostSlider.value = boostController.BoostNormalized;

            // 重力方向表示
            if (gravityText != null && gravityController != null)
            {
                gravityText.text = gravityController.GravityDirection == -1
                    ? "▼ GRAVITY"
                    : "▲ GRAVITY";
            }
        }

        private void UpdateCountdown()
        {
            if (raceManager == null || countdownPanel == null) return;

            bool isCountdown = raceManager.CurrentState == RaceState.Countdown;
            countdownPanel.SetActive(isCountdown);

            if (isCountdown && countdownText != null)
            {
                float remaining = raceManager.CountdownRemaining;
                countdownText.text = remaining > 0.5f
                    ? Mathf.CeilToInt(remaining).ToString()
                    : "GO!";
            }
        }

        // ─── 外部メソッド ────────────────────────────────────────────
        /// <summary>チェックポイント通過時に RaceManager から呼ばれる。</summary>
        public void UpdateCheckpointDisplay(int current, int total)
        {
            if (checkpointText != null)
                checkpointText.text = $"CP: {current} / {total}";
        }

        /// <summary>ゴール時に RaceManager から呼ばれる。</summary>
        public void ShowResultPanel(float time, float bestTime, string rank, bool isNewBest)
        {
            if (resultPanel == null) return;
            resultPanel.SetActive(true);

            if (resultTimeText != null) resultTimeText.text = $"TIME\n{RaceManager.FormatTime(time)}";
            if (resultRankText != null) resultRankText.text = $"RANK  {rank}";
            if (resultBestText != null) resultBestText.text = $"BEST\n{RaceManager.FormatTime(bestTime)}";
            if (newBestLabel   != null) newBestLabel.SetActive(isNewBest);
        }

        // ─── ポーズ制御 ──────────────────────────────────────────────
        private void TogglePause()
        {
            // ゴール後はポーズ不可
            if (raceManager != null && raceManager.CurrentState == RaceState.Finished) return;

            isPaused = !isPaused;
            if (pausePanel != null) pausePanel.SetActive(isPaused);
            Time.timeScale = isPaused ? 0f : 1f;
        }

        // ─── ベストタイム表示 ────────────────────────────────────────
        private void UpdateBestTimeText()
        {
            if (bestTimeText == null || raceManager == null) return;
            float best = raceManager.BestTime;
            bestTimeText.text = best > 0f
                ? $"BEST  {RaceManager.FormatTime(best)}"
                : "BEST  --:--.---";
        }
    }
}