using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GravityRacer
{
    /// <summary>
    /// ゲームの状態を管理するシングルトン。
    /// Waiting → Countdown → Racing → Finished の順に遷移する。
    /// PlayerPrefs でシーンごとのベストタイムをローカル保存する。
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        // ─── シングルトン ───────────────────────────────────────────
        public static RaceManager Instance { get; private set; }

        // ─── Inspector 設定 ─────────────────────────────────────────
        [Header("Race Settings")]
        [Tooltip("このステージで使用する PlayerPrefs キー名（例：Stage01_Best）")]
        [SerializeField] private string bestTimeKey = "Stage01_Best";

        [Tooltip("カウントダウン秒数（3 → 2 → 1 → GO!）")]
        [SerializeField] private float countdownDuration = 3f;

        [Tooltip("ステージ内のチェックポイント総数")]
        [SerializeField] private int totalCheckpoints = 3;

        [Header("Rank Thresholds（秒）")]
        [SerializeField] private float rankSThreshold = 60f;
        [SerializeField] private float rankAThreshold = 90f;
        [SerializeField] private float rankBThreshold = 120f;

        // ─── プロパティ ─────────────────────────────────────────────
        /// <summary>現在のゲーム状態</summary>
        public RaceState CurrentState { get; private set; } = RaceState.Waiting;

        /// <summary>現在の経過タイム（秒）</summary>
        public float CurrentTime { get; private set; } = 0f;

        /// <summary>このステージのベストタイム。未記録時は 0。</summary>
        public float BestTime { get; private set; } = 0f;

        /// <summary>通過済みチェックポイント数</summary>
        public int CheckpointsReached { get; private set; } = 0;

        /// <summary>チェックポイント総数（UIManager が参照）</summary>
        public int TotalCheckpoints => totalCheckpoints;

        /// <summary>カウントダウン残り秒数（UI表示用）</summary>
        public float CountdownRemaining { get; private set; } = 0f;

        // ─── 内部変数 ───────────────────────────────────────────────
        private UIManager uiManager;
        private InputHandler inputHandler;

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // ベストタイムを読み込む（未記録なら 0f）
            BestTime = PlayerPrefs.GetFloat(bestTimeKey, 0f);
        }

        private void Start()
        {
            uiManager = FindFirstObjectByType<UIManager>();
            inputHandler = InputHandler.Instance;

            if (inputHandler != null)
                inputHandler.OnRestart += RestartRace;

            StartCoroutine(CountdownRoutine());
        }

        private void OnDestroy()
        {
            if (inputHandler != null)
                inputHandler.OnRestart -= RestartRace;
        }

        private void Update()
        {
            // レース中のみタイムを加算
            if (CurrentState == RaceState.Racing)
                CurrentTime += Time.deltaTime;
        }

        // ─── カウントダウン ──────────────────────────────────────────
        private IEnumerator CountdownRoutine()
        {
            CurrentState = RaceState.Countdown;
            CountdownRemaining = countdownDuration;

            while (CountdownRemaining > 0f)
            {
                CountdownRemaining -= Time.deltaTime;
                yield return null;
            }

            CountdownRemaining = 0f;
            CurrentTime = 0f;
            CurrentState = RaceState.Racing;
            AudioManager.Instance?.PlayRaceBGM();
        }

        // ─── チェックポイント ────────────────────────────────────────
        /// <summary>Checkpoint.cs から呼び出す。</summary>
        public void OnCheckpointReached(int checkpointIndex)
        {
            if (CurrentState != RaceState.Racing) return;

            // 最大値を更新（逆走で再通過しても増えない）
            if (checkpointIndex + 1 > CheckpointsReached)
            {
                CheckpointsReached = checkpointIndex + 1;
                AudioManager.Instance?.PlayCheckpoint();
                uiManager?.UpdateCheckpointDisplay(CheckpointsReached, totalCheckpoints);
            }
        }

        // ─── ゴール ──────────────────────────────────────────────────
        /// <summary>GoalTrigger.cs から呼び出す。</summary>
        public void OnGoalReached()
        {
            if (CurrentState != RaceState.Racing) return;
            CurrentState = RaceState.Finished;

            AudioManager.Instance?.StopBGM();
            AudioManager.Instance?.PlayGoal();

            // ベストタイム更新
            bool isNewBest = BestTime <= 0f || CurrentTime < BestTime;
            if (isNewBest)
            {
                BestTime = CurrentTime;
                PlayerPrefs.SetFloat(bestTimeKey, BestTime);
                PlayerPrefs.Save();
            }

            string rank = CalcRank(CurrentTime);
            uiManager?.ShowResultPanel(CurrentTime, BestTime, rank, isNewBest);
        }

        // ─── ランク計算 ──────────────────────────────────────────────
        private string CalcRank(float time)
        {
            if (time <= rankSThreshold) return "S";
            if (time <= rankAThreshold) return "A";
            if (time <= rankBThreshold) return "B";
            return "C";
        }

        // ─── リスタート ──────────────────────────────────────────────
        /// <summary>InputHandler の OnRestart イベントで呼ばれる。</summary>
        public void RestartRace()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ─── ユーティリティ ──────────────────────────────────────────
        /// <summary>秒を "MM:SS.mmm" 形式にフォーマットする</summary>
        public static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            int ms = (int)((seconds * 1000f) % 1000f);
            return $"{m:00}:{s:00}.{ms:000}";
        }
    }

    /// <summary>レースの状態を表す列挙型</summary>
    public enum RaceState
    {
        Waiting,    // 初期状態（カウントダウン前）
        Countdown,  // カウントダウン中
        Racing,     // レース中
        Finished    // ゴール後
    }
}