using UnityEngine;

namespace GravityRacer
{
    /// <summary>
    /// フェーズ 1 テスト用のコースをコードで自動生成するスクリプト。
    ///
    /// 【使い方】
    ///   1. 空の GameObject を作成し、このスクリプトを追加する
    ///   2. Inspector で各パラメータを確認し、Play ボタンを押す
    ///   3. コースが自動生成されて即プレイ可能になる
    ///
    /// 【生成されるコース概要（左→右、約110ユニット）】
    ///   [スタート] → 平坦区間 → 緩い上り坂 → 天井ルート（重力反転必須）
    ///              → 下り坂 → ブーストパネル区間 → ゴール
    ///
    /// 【必要な事前設定】
    ///   - "Ground" レイヤー（Layer 8 推奨）を Project Settings で作成する
    ///   - Player に "Player" タグを設定する
    ///   - PlayerMovement の groundMask に Ground レイヤーを設定する
    /// </summary>
    public class TestStageBuilder : MonoBehaviour
    {
        [Header("Build Settings")]
        [Tooltip("Play 時に自動ビルドするか")]
        [SerializeField] private bool buildOnStart = true;

        [Header("Course Dimensions")]
        [SerializeField] private float groundY   = -4f;  // 床の Y 座標
        [SerializeField] private float ceilingY  =  4f;  // 天井の Y 座標
        [SerializeField] private float wallThickness = 0.5f;

        [Header("Player")]
        [Tooltip("Player Prefab または Player GameObject（シーンに既存の場合は不要）")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector2 playerStartPos = new Vector2(2f, -3f);

        [Header("Camera")]
        [SerializeField] private Camera mainCamera;

        [Header("Physics Material")]
        [Tooltip("摩擦なし・弾性なしの PhysicsMaterial2D。未設定の場合はデフォルト。")]
        [SerializeField] private PhysicsMaterial2D frictionlessMaterial;

        // ─── Ground レイヤー番号（Project Settings と合わせる） ──────
        private const int GROUND_LAYER = 8; // "Ground" レイヤー

        // ─── ライフサイクル ─────────────────────────────────────────
        private void Start()
        {
            if (buildOnStart) BuildStage();
        }

        // ─── メインビルド関数 ────────────────────────────────────────
        public void BuildStage()
        {
            // ── 親 GameObject を作成（Hierarchy を整理） ──
            Transform courseRoot = new GameObject("Course_TestStage").transform;

            // ── 1. 床（Ground）を EdgeCollider2D で生成 ──
            // コース形状（X 座標の流れ）:
            //  0        30       45       65      85     110
            //  |平坦____|上り坂/|上り__|下り坂\|平坦__|GOAL
            CreateFloor(courseRoot);

            // ── 2. 天井を生成 ──
            CreateCeiling(courseRoot);

            // ── 3. 左壁 ──
            CreateWall(courseRoot, new Vector2(-1f, groundY), new Vector2(-1f, ceilingY), "LeftWall");

            // ── 4. 障害物を配置 ──
            CreateObstacles(courseRoot);

            // ── 5. ブーストパネルを配置 ──
            CreateBoostPads(courseRoot);

            // ── 6. チェックポイントを配置 ──
            CreateCheckpoints(courseRoot);

            // ── 7. ゴールゲートを配置 ──
            CreateGoal(courseRoot);

            // ── 8. プレイヤーを配置 ──
            SetupPlayer();

            // ── 9. カメラを設定 ──
            SetupCamera();

            // ── 10. 背景色を設定 ──
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
                mainCamera.backgroundColor = new Color(0.02f, 0.02f, 0.08f);

            Debug.Log("[TestStageBuilder] テストステージの生成が完了しました。");
        }

        // ─── 床生成 ──────────────────────────────────────────────────
        private void CreateFloor(Transform parent)
        {
            // EdgeCollider2D で折れ線状の床を定義
            // 形状: 平坦→上り坂→平坦（天井ルート下）→下り坂→平坦→ゴール付近
            Vector2[] points = new Vector2[]
            {
                new Vector2(-2f,  groundY),      // スタート左端
                new Vector2(30f,  groundY),      // 平坦区間終端
                new Vector2(45f,  groundY + 2f), // 上り坂頂点
                new Vector2(50f,  groundY + 2f), // 天井ルート下（プレイヤーは天井側を走る）
                new Vector2(65f,  groundY + 2f), // 天井ルート終端
                new Vector2(80f,  groundY),      // 下り坂後の平坦
                new Vector2(112f, groundY),      // ゴール付近
            };

            CreateEdgePath(parent, points, "Floor", GROUND_LAYER, new Color(0.1f, 0.4f, 0.8f));
        }

        // ─── 天井生成 ────────────────────────────────────────────────
        private void CreateCeiling(Transform parent)
        {
            // 天井形状：中間に「天井ルート」区間を設ける（重力反転でここを走る）
            // 天井ルート区間(45〜65)では天井の位置を下げて通路を作る
            Vector2[] points = new Vector2[]
            {
                new Vector2(-2f,  ceilingY),      // 天井左端
                new Vector2(40f,  ceilingY),      // 天井ルート手前
                // 天井ルート区間：天井が下がって「溝」を形成
                // 重力反転したプレイヤーが走る面 = ceilingY の内側
                new Vector2(40f,  ceilingY),
                new Vector2(68f,  ceilingY),      // 天井ルート終端
                new Vector2(112f, ceilingY),      // 天井右端
            };

            CreateEdgePath(parent, points, "Ceiling", GROUND_LAYER, new Color(0.1f, 0.4f, 0.8f));

            // 天井ルート区間の下部に「天井トンネル下壁」を追加
            // （重力反転プレイヤーが走る壁：y = ceilingY - 2 の区間）
            Vector2[] tunnelBottom = new Vector2[]
            {
                new Vector2(40f, ceilingY - 2.5f),
                new Vector2(68f, ceilingY - 2.5f),
            };
            CreateEdgePath(parent, tunnelBottom, "CeilingTunnelBottom", GROUND_LAYER,
                new Color(0.2f, 0.2f, 0.6f));
        }

        // ─── 壁生成 ──────────────────────────────────────────────────
        private void CreateWall(Transform parent, Vector2 bottom, Vector2 top, string name)
        {
            Vector2[] points = new Vector2[] { bottom, top };
            CreateEdgePath(parent, points, name, GROUND_LAYER, new Color(0.3f, 0.3f, 0.7f));
        }

        // ─── 障害物配置 ──────────────────────────────────────────────
        private void CreateObstacles(Transform parent)
        {
            // 固定障害物（平坦区間に配置）
            CreateStaticObstacle(parent, new Vector2(20f, groundY + 1f), new Vector2(1f, 2f), "Obstacle_01");
            CreateStaticObstacle(parent, new Vector2(55f, ceilingY - 3f), new Vector2(1f, 1.5f), "Obstacle_Ceiling_01");

            // 移動障害物（上下往復）
            CreateMovingObstacle(parent, new Vector2(72f, groundY + 2.5f), "MovingObstacle_01");
            CreateMovingObstacle(parent, new Vector2(90f, groundY + 2.5f), "MovingObstacle_02");

            // レーザーゲート
            CreateLaserGate(parent, new Vector2(35f, groundY + 1.5f), "LaserGate_01");
        }

        private void CreateStaticObstacle(Transform parent, Vector2 pos, Vector2 size, string objName)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(size, new Color(1f, 0.3f, 0.1f));
            sr.sortingOrder = 2;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            ObstacleController obs = go.AddComponent<ObstacleController>();
        }

        private void CreateMovingObstacle(Transform parent, Vector2 pos, string objName)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(new Vector2(1f, 1f), new Color(1f, 0.5f, 0f));
            sr.sortingOrder = 2;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            ObstacleController obs = go.AddComponent<ObstacleController>();
        }

        private void CreateLaserGate(Transform parent, Vector2 pos, string objName)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(new Vector2(0.3f, 3f), new Color(1f, 0f, 0.5f, 0.8f));
            sr.sortingOrder = 3;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.3f, 3f);

            ObstacleController obs = go.AddComponent<ObstacleController>();
            // laserRenderer / laserCollider は Inspector でも設定可能。
            // ここでは SetField を使わずシリアライズフィールドは InspectorのみでOK
        }

        // ─── ブーストパネル配置 ──────────────────────────────────────
        private void CreateBoostPads(Transform parent)
        {
            float[] xPositions = { 10f, 25f, 82f, 95f };
            for (int i = 0; i < xPositions.Length; i++)
            {
                GameObject go = new GameObject($"BoostPad_{i:00}");
                go.transform.SetParent(parent);
                go.transform.position = new Vector3(xPositions[i], groundY + 0.3f, 0f);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateRectSprite(new Vector2(2.5f, 0.3f), new Color(1f, 0.85f, 0f));
                sr.sortingOrder = 2;

                BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(2.5f, 0.4f);

                BoostPad pad = go.AddComponent<BoostPad>();
            }
        }

        // ─── チェックポイント配置 ────────────────────────────────────
        private void CreateCheckpoints(Transform parent)
        {
            (float x, int idx)[] checkpoints =
            {
                (25f, 0),
                (60f, 1),
                (90f, 2),
            };

            foreach (var (x, idx) in checkpoints)
            {
                GameObject go = new GameObject($"Checkpoint_{idx}");
                go.transform.SetParent(parent);
                go.transform.position = new Vector3(x, 0f, 0f);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateRectSprite(new Vector2(0.5f, ceilingY - groundY),
                    new Color(0f, 1f, 0.5f, 0.3f));
                sr.sortingOrder = 1;

                BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.5f, ceilingY - groundY);

                //Checkpoint cp = go.AddComponent<Checkpoint>();
            }
        }

        // ─── ゴール配置 ──────────────────────────────────────────────
        private void CreateGoal(Transform parent)
        {
            GameObject go = new GameObject("GoalGate");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(108f, 0f, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(new Vector2(1f, ceilingY - groundY),
                new Color(1f, 1f, 0f, 0.5f));
            sr.sortingOrder = 2;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, ceilingY - groundY);

            go.AddComponent<GoalTrigger>();
        }

        // ─── プレイヤー設定 ──────────────────────────────────────────
        private void SetupPlayer()
        {
            // 既存の Player を検索
            GameObject player = GameObject.FindWithTag("Player");

            if (player == null)
            {
                if (playerPrefab != null)
                {
                    player = Instantiate(playerPrefab);
                }
                else
                {
                    // プロシージャルでプレイヤー機体を作成
                    player = CreateDefaultPlayer();
                }
            }

            // 開始位置に配置
            player.transform.position = new Vector3(playerStartPos.x, playerStartPos.y, 0f);

            // GameManager に RaceManager がなければ追加
            EnsureGameManagers();
        }

        private GameObject CreateDefaultPlayer()
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";

            // ── 機体スプライト（ひし形シェイプ） ──
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = CreateShipSprite();
            sr.color  = new Color(0f, 0.85f, 1f);
            sr.sortingOrder = 10;

            // ── CapsuleCollider2D ──
            CapsuleCollider2D cap = player.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Horizontal;
            cap.size = new Vector2(1.4f, 0.55f);
            PhysicsMaterial2D mat = new PhysicsMaterial2D("PlayerMat");
            mat.friction   = 0f;
            mat.bounciness = 0f;
            cap.sharedMaterial = mat;

            // ── Rigidbody2D ──
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.angularDamping = 20f;
            rb.linearDamping  = 1f;

            // ── スクリプト追加 ──
            player.AddComponent<GravityController>();
            player.AddComponent<BoostController>();
            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerVisualController>();

            // PlayerMovement の groundMask に Ground レイヤーを設定
            PlayerMovement pm = player.GetComponent<PlayerMovement>();

            return player;
        }

        private void EnsureGameManagers()
        {
            if (FindFirstObjectByType<InputHandler>() == null)
            {
                GameObject gm = new GameObject("_GameManagers");
                gm.AddComponent<InputHandler>();
                gm.AddComponent<AudioManager>();
                gm.AddComponent<RaceManager>();
                gm.AddComponent<UIManager>();
            }
        }

        // ─── カメラ設定 ──────────────────────────────────────────────
        private void SetupCamera()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            CameraController cc = mainCamera.GetComponent<CameraController>();
            if (cc == null) cc = mainCamera.gameObject.AddComponent<CameraController>();
        }

        // ─── スプライト生成ユーティリティ ──────────────────────────
        /// <summary>
        /// 矩形スプライトを生成する（タイル・障害物・ゴールなどに使用）。
        /// </summary>
        private Sprite CreateRectSprite(Vector2 size, Color color)
        {
            int w = Mathf.Max(1, Mathf.RoundToInt(size.x * 32));
            int h = Mathf.Max(1, Mathf.RoundToInt(size.y * 32));
            Texture2D tex = new Texture2D(w, h);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>
        /// 簡易的な宇宙船シルエットスプライトを生成する。
        /// </summary>
        private Sprite CreateShipSprite()
        {
            int w = 48, h = 24;
            Texture2D tex = new Texture2D(w, h);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[w * h];
            Color shipColor = new Color(0.1f, 0.9f, 1f, 1f);
            Color cockpitColor = new Color(0.8f, 0.95f, 1f, 0.9f);
            Color transparent = Color.clear;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;   // 0〜1
                float ny = (float)y / h;   // 0〜1
                float cy = ny - 0.5f;      // -0.5〜0.5

                // 船体：右半分が細くなる三角形
                float bodyWidth = 0.45f * (1f - nx * 0.8f);
                bool inBody = Mathf.Abs(cy) < bodyWidth;

                // コックピット：前方 1/4 付近の膨らみ
                float cxOff = nx - 0.75f;
                bool inCockpit = cxOff * cxOff / 0.02f + cy * cy / 0.04f < 1f;

                if (inBody)
                    pixels[y * w + x] = inCockpit ? cockpitColor : shipColor;
                else
                    pixels[y * w + x] = transparent;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 24f);
        }

        // ─── EdgeCollider2D パス生成 ──────────────────────────────────
        private GameObject CreateEdgePath(Transform parent, Vector2[] points, string objName,
            int layer, Color color)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent);
            go.transform.position = Vector3.zero;
            go.layer = layer;

            EdgeCollider2D edge = go.AddComponent<EdgeCollider2D>();
            edge.points = points;
            if (frictionlessMaterial != null) edge.sharedMaterial = frictionlessMaterial;

            // ライン描画（LineRenderer で可視化）
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = points.Length;
            for (int i = 0; i < points.Length; i++)
                lr.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
            lr.startWidth = wallThickness;
            lr.endWidth   = wallThickness;
            lr.material   = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor   = color;
            lr.sortingOrder = 5;

            return go;
        }
    }
}