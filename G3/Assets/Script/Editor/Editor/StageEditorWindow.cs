using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// G-race ステージエディタ
/// Tools メニュー → "ステージエディタ" または Ctrl+Shift+E で起動。
///
/// ■ 配置できる要素
///   床・天井・障害物・敵・重力ゲート・ブースト・チェックポイント・死亡ゾーン
///
/// ■ 使い方
///   1. ウィンドウを開き「配置モード ON」にする
///   2. パレットから置きたい要素を選ぶ
///   3. シーンビューでクリック or ドラッグして配置
///   4. 右クリックで選択中オブジェクトを削除
///   5. 後でプレハブを用意したら「プレハブ設定」に登録して一括差し替え
/// </summary>
public class StageEditorWindow : EditorWindow
{
    // =====================================================================
    // 定数・設定テーブル
    // =====================================================================

    static readonly string[] Names =
    {
        "床  Floor", "天井  Ceiling", "障害物  Obstacle",
        "敵  Enemy", "重力ゲート  GravityGate", "ブースト  Boost",
        "チェックポイント  CP", "死亡ゾーン  Death"
    };

    // PlayerMove_2D で使われているタグに合わせる
    static readonly string[] Tags =
    {
        "Untagged",        // Floor
        "Untagged",        // Ceiling
        "Wall",        // Obstacle
        "Enemy",       // Enemy   ← 要追加
        "GravityGate", // GravityGate
        "Boost",       // Boost
        "CheckPoint",  // Checkpoint
        "Death"        // Death
    };

    // Collider が Trigger かどうか
    static readonly bool[] IsTrigger =
    {
        false, // Floor
        false, // Ceiling
        false, // Obstacle
        false, // Enemy
        true,  // GravityGate
        true,  // Boost
        true,  // Checkpoint
        true   // Death
    };

    static readonly Color[] ElementColors =
    {
        new Color(0.55f, 0.37f, 0.19f), // Floor       茶色
        new Color(0.25f, 0.35f, 0.55f), // Ceiling     青系
        new Color(0.80f, 0.20f, 0.20f), // Obstacle    赤
        new Color(0.90f, 0.50f, 0.00f), // Enemy       オレンジ
        new Color(0.60f, 0.00f, 0.90f), // GravityGate 紫
        new Color(0.00f, 0.85f, 0.85f), // Boost       シアン
        new Color(0.10f, 0.80f, 0.10f), // Checkpoint  緑
        new Color(0.95f, 0.10f, 0.10f)  // Death       赤（明）
    };

    static readonly Vector2[] DefaultSizes =
    {
        new Vector2(10f, 1f),  // Floor
        new Vector2(10f, 1f),  // Ceiling
        new Vector2(1f,  2f),  // Obstacle
        new Vector2(1f,  1f),  // Enemy
        new Vector2(0.5f,5f),  // GravityGate
        new Vector2(2f,  1f),  // Boost
        new Vector2(1f,  3f),  // Checkpoint
        new Vector2(5f,  2f)   // Death
    };

    // 障害物を破壊可能(layer 3)にするかどうか（Obstacle 専用）
    static readonly string[] ShortNames =
    {
        "Floor","Ceiling","Obstacle","Enemy","GravityGate","Boost","CheckPoint","Death"
    };

    // =====================================================================
    // ウィンドウ状態
    // =====================================================================

    StageElementInfo.ElementType _selected = StageElementInfo.ElementType.Floor;
    GameObject[]  _prefabs     = new GameObject[8];
    Vector2[]     _sizes;                   // ユーザーが編集できるデフォルトサイズ
    bool          _destroyable  = false;    // 障害物を破壊可能(layer 3)にするか
    bool          _active       = true;     // 配置モード ON/OFF
    bool          _dragMode     = true;     // ドラッグで矩形を定義するか
    bool          _snapToGrid   = true;
    float         _gridSize     = 1f;
    bool          _showGrid     = true;     // シーンにグリッドを表示
    Transform     _stageParent;
    Vector2       _scrollPos;
    bool          _foldSize     = true;
    bool          _foldPrefab   = false;
    bool          _foldSettings = true;

    // ドラッグ状態
    bool    _isDragging;
    Vector3 _dragStart;
    Vector3 _dragEnd;

    // 白スプライトのキャッシュ
    static Sprite _whiteSprite;

    // =====================================================================
    // 開く
    // =====================================================================

    [MenuItem("Tools/ステージエディタ %#e")]
    public static void Open()
    {
        var w = GetWindow<StageEditorWindow>("ステージエディタ");
        w.minSize = new Vector2(290, 520);
        w.Show();
    }

    // =====================================================================
    // Unity コールバック
    // =====================================================================

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        _sizes = DefaultSizes.ToArray(); // コピー
        EnsureRequiredTags();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // =====================================================================
    // Inspector (ウィンドウ本体)
    // =====================================================================

    void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        // ── ヘッダー ──────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("ステージエディタ", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("タグ再確認", GUILayout.Width(80)))
                EnsureRequiredTags();
        }
        EditorGUILayout.Space(4);

        // ── 配置モードトグル ─────────────────────────────────────────
        var prevBG = GUI.backgroundColor;
        GUI.backgroundColor = _active ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
        string activeLabel = _active ? "✔  配置モード ON  (右クリック = 削除)" : "✖  配置モード OFF";
        if (GUILayout.Button(activeLabel, GUILayout.Height(32)))
            _active = !_active;
        GUI.backgroundColor = prevBG;

        EditorGUILayout.Space(6);

        // ── パレット ─────────────────────────────────────────────────
        GUILayout.Label("パレット  (1〜8 キーで切替)", EditorStyles.boldLabel);
        DrawPalette();

        // 障害物選択中のみ「破壊可能」オプション表示
        if (_selected == StageElementInfo.ElementType.Obstacle)
        {
            EditorGUI.indentLevel++;
            _destroyable = EditorGUILayout.Toggle("破壊可能 (Layer 3)", _destroyable);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(6);

        // ── 配置設定 ─────────────────────────────────────────────────
        _foldSettings = EditorGUILayout.Foldout(_foldSettings, "配置設定", true);
        if (_foldSettings)
        {
            EditorGUI.indentLevel++;
            _dragMode    = EditorGUILayout.Toggle("ドラッグで矩形を定義", _dragMode);
            _snapToGrid  = EditorGUILayout.Toggle("グリッドスナップ", _snapToGrid);
            if (_snapToGrid)
            {
                EditorGUI.indentLevel++;
                _gridSize = EditorGUILayout.FloatField("グリッドサイズ", _gridSize);
                _showGrid = EditorGUILayout.Toggle("グリッド表示", _showGrid);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ── 親オブジェクト ───────────────────────────────────────────
        GUILayout.Label("ステージ親オブジェクト", EditorStyles.boldLabel);
        _stageParent = (Transform)EditorGUILayout.ObjectField(
            "Parent", _stageParent, typeof(Transform), true);
        if (_stageParent == null)
        {
            if (GUILayout.Button("\"Stage\" を作成 / 検索"))
                _stageParent = GetOrCreateStageParent();
        }

        EditorGUILayout.Space(4);

        // ── サイズ設定 ───────────────────────────────────────────────
        _foldSize = EditorGUILayout.Foldout(_foldSize, "デフォルトサイズ設定", true);
        if (_foldSize)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < 8; i++)
            {
                var c = GUI.backgroundColor;
                GUI.backgroundColor = ElementColors[i] * 0.9f;
                _sizes[i] = EditorGUILayout.Vector2Field(ShortNames[i], _sizes[i]);
                GUI.backgroundColor = c;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ── プレハブ差し替え設定 ─────────────────────────────────────
        _foldPrefab = EditorGUILayout.Foldout(_foldPrefab, "プレハブ設定（将来のアセット差し替え用）", true);
        if (_foldPrefab)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < 8; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var c = GUI.backgroundColor;
                    GUI.backgroundColor = ElementColors[i];
                    GUILayout.Label(ShortNames[i], GUILayout.Width(130));
                    GUI.backgroundColor = c;
                    _prefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                        _prefabs[i], typeof(GameObject), false);
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.HelpBox(
                "プレハブを設定すると、以降の配置でそのプレハブが使われます。\n" +
                "未設定なら矩形(SpriteRenderer)で配置します。",
                MessageType.Info);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("シーン内の矩形を一括プレハブ差し替え"))
                ReplaceAllWithPrefabs();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    // ── パレット描画 ─────────────────────────────────────────────────

    void DrawPalette()
    {
        int cols = 2;
        for (int row = 0; row < 4; row++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int col = 0; col < cols; col++)
                {
                    int idx = row * cols + col;
                    bool isSelected = (int)_selected == idx;
                    var c = GUI.backgroundColor;
                    GUI.backgroundColor = isSelected ? ElementColors[idx] : ElementColors[idx] * 0.55f;
                    var style = new GUIStyle(GUI.skin.button)
                    {
                        fontStyle  = isSelected ? FontStyle.Bold : FontStyle.Normal,
                        fixedHeight = 38,
                        fontSize   = 11,
                        wordWrap   = true,
                        alignment  = TextAnchor.MiddleCenter
                    };
                    string label = $"[{idx + 1}]  {Names[idx].Split("  ")[0]}";
                    if (isSelected) label = "▶ " + label;
                    if (GUILayout.Button(label, style))
                        _selected = (StageElementInfo.ElementType)idx;
                    GUI.backgroundColor = c;
                }
            }
        }
    }

    // =====================================================================
    // シーンビュー
    // =====================================================================

    void OnSceneGUI(SceneView sv)
    {
        // 1〜8 キーでパレット切替
        HandleKeyboardShortcuts();

        if (!_active) return;

        // マウス位置をワールド座標へ
        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        var mouseWorld = new Vector3(ray.origin.x, ray.origin.y, 0f);
        mouseWorld = SnapToGrid(mouseWorld);

        // グリッドオーバーレイ
        if (_snapToGrid && _showGrid)
            DrawGridOverlay(sv, mouseWorld);

        // 要素名ラベルを右上に表示
        DrawSceneLabel(sv);

        int cid = GUIUtility.GetControlID(FocusType.Passive);

        if (_dragMode)
            HandleDragMode(sv, mouseWorld, cid);
        else
            HandleClickMode(sv, mouseWorld, cid);

        sv.Repaint();
    }

    // ── キーボードショートカット 1〜8 ──────────────────────────────

    void HandleKeyboardShortcuts()
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;
        int key = -1;
        if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha8)
            key = e.keyCode - KeyCode.Alpha1;
        if (e.keyCode >= KeyCode.Keypad1 && e.keyCode <= KeyCode.Keypad8)
            key = e.keyCode - KeyCode.Keypad1;
        if (key >= 0)
        {
            _selected = (StageElementInfo.ElementType)key;
            Repaint();
            e.Use();
        }
    }

    // ── ドラッグモード ───────────────────────────────────────────────

    void HandleDragMode(SceneView sv, Vector3 mouse, int cid)
    {
        var e = Event.current;
        int idx = (int)_selected;

        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
                _isDragging = true;
                _dragStart  = mouse;
                _dragEnd    = mouse;
                GUIUtility.hotControl = cid;
                e.Use();
                break;

            case EventType.MouseDrag when _isDragging && e.button == 0:
                _dragEnd = mouse;
                e.Use();
                break;

            case EventType.MouseUp when e.button == 0 && _isDragging:
                _isDragging = false;
                GUIUtility.hotControl = 0;
                var center = (_dragStart + _dragEnd) * 0.5f;
                var size = new Vector2(
                    Mathf.Max(Mathf.Abs(_dragEnd.x - _dragStart.x), 0.1f),
                    Mathf.Max(Mathf.Abs(_dragEnd.y - _dragStart.y), 0.1f)
                );
                PlaceElement(center, size);
                e.Use();
                break;

            case EventType.MouseDown when e.button == 1:
                DeleteUnderMouse(mouse);
                e.Use();
                break;
        }

        // プレビュー描画
        Color col = ElementColors[idx];
        if (_isDragging)
        {
            var c   = (_dragStart + _dragEnd) * 0.5f;
            var s   = new Vector2(
                Mathf.Max(Mathf.Abs(_dragEnd.x - _dragStart.x), 0.1f),
                Mathf.Max(Mathf.Abs(_dragEnd.y - _dragStart.y), 0.1f)
            );
            DrawRect(c, s, new Color(col.r, col.g, col.b, 0.35f), col);
            Handles.Label(_dragStart, $"{s.x:F1} x {s.y:F1}",
                new GUIStyle { normal = { textColor = col }, fontStyle = FontStyle.Bold });
        }
        else
        {
            DrawRect(mouse, _sizes[idx],
                new Color(col.r, col.g, col.b, 0.12f),
                new Color(col.r, col.g, col.b, 0.5f));
        }
    }

    // ── クリックモード ───────────────────────────────────────────────

    void HandleClickMode(SceneView sv, Vector3 mouse, int cid)
    {
        var e   = Event.current;
        int idx = (int)_selected;
        Color col = ElementColors[idx];

        // プレビュー
        DrawRect(mouse, _sizes[idx],
            new Color(col.r, col.g, col.b, 0.2f), col);

        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
                GUIUtility.hotControl = cid;
                PlaceElement(mouse, _sizes[idx]);
                e.Use();
                break;
            case EventType.MouseUp when e.button == 0:
                GUIUtility.hotControl = 0;
                e.Use();
                break;
            case EventType.MouseDown when e.button == 1:
                DeleteUnderMouse(mouse);
                e.Use();
                break;
        }
    }

    // =====================================================================
    // 要素の配置
    // =====================================================================

    void PlaceElement(Vector3 center, Vector2 size)
    {
        int idx = (int)_selected;

        Undo.SetCurrentGroupName($"配置: {ShortNames[idx]}");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject go;

        if (_prefabs[idx] != null)
        {
            // ── プレハブモード ──────────────────────────────────────
            go = (GameObject)PrefabUtility.InstantiatePrefab(_prefabs[idx]);
            Undo.RegisterCreatedObjectUndo(go, "place prefab");
            go.transform.position   = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
        else
        {
            // ── 矩形モード ─────────────────────────────────────────
            go = new GameObject(ShortNames[idx]);
            Undo.RegisterCreatedObjectUndo(go, "place rect");
            go.transform.position   = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            // SpriteRenderer（白スプライト + 色付け）
            var sr   = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateWhiteSprite();
            sr.color  = ElementColors[idx];

            // Collider2D
            var col2d = go.AddComponent<BoxCollider2D>();
            col2d.isTrigger = IsTrigger[idx];
        }

        // タグ設定
        TrySetTag(go, Tags[idx]);

        // 破壊可能障害物は Layer 3
        if (_selected == StageElementInfo.ElementType.Obstacle && _destroyable)
            go.layer = 3;

        // StageElementInfo をアタッチ
        var info = go.AddComponent<StageElementInfo>();
        info.elementType = _selected;

        // 親に格納
        if (_stageParent != null)
            Undo.SetTransformParent(go.transform, _stageParent, "re-parent");

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = go;
        EditorUtility.SetDirty(go);
    }

    // =====================================================================
    // 右クリック削除
    // =====================================================================

    void DeleteUnderMouse(Vector3 worldPos)
    {
        // マウス座標のコライダーを検索（2D）
        var cols = Physics2D.OverlapPointAll(new Vector2(worldPos.x, worldPos.y));
        foreach (var c in cols)
        {
            if (c.GetComponent<StageElementInfo>() != null)
            {
                Undo.DestroyObjectImmediate(c.gameObject);
                return;
            }
        }
    }

    // =====================================================================
    // プレハブ一括差し替え
    // =====================================================================

    void ReplaceAllWithPrefabs()
    {
        var elements = FindObjectsOfType<StageElementInfo>();
        int count    = 0;

        Undo.SetCurrentGroupName("一括プレハブ差し替え");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var elem in elements)
        {
            if (elem.excludeFromReplace) continue;
            int idx = (int)elem.elementType;
            if (_prefabs[idx] == null) continue;

            var t      = elem.transform;
            var pos    = t.position;
            var scale  = t.localScale;
            var parent = t.parent;

            Undo.DestroyObjectImmediate(elem.gameObject);

            var newGo = (GameObject)PrefabUtility.InstantiatePrefab(_prefabs[idx]);
            Undo.RegisterCreatedObjectUndo(newGo, "replace");
            newGo.transform.position   = pos;
            newGo.transform.localScale = scale;
            if (parent != null)
                Undo.SetTransformParent(newGo.transform, parent, "re-parent");
            count++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[StageEditor] {count} 個の要素をプレハブに差し替えました。");
    }

    // =====================================================================
    // グリッド描画
    // =====================================================================

    void DrawGridOverlay(SceneView sv, Vector3 center)
    {
        float camSize  = sv.camera.orthographicSize;
        float camAsp   = sv.camera.aspect;
        int   stepsX   = Mathf.CeilToInt(camSize * camAsp / _gridSize) + 2;
        int   stepsY   = Mathf.CeilToInt(camSize / _gridSize) + 2;

        float startX   = Mathf.Floor(sv.camera.transform.position.x / _gridSize) * _gridSize;
        float startY   = Mathf.Floor(sv.camera.transform.position.y / _gridSize) * _gridSize;

        Handles.color  = new Color(1f, 1f, 1f, 0.07f);

        for (int i = -stepsX; i <= stepsX; i++)
        {
            float x = startX + i * _gridSize;
            Handles.DrawLine(
                new Vector3(x, startY - stepsY * _gridSize, 0),
                new Vector3(x, startY + stepsY * _gridSize, 0));
        }
        for (int j = -stepsY; j <= stepsY; j++)
        {
            float y = startY + j * _gridSize;
            Handles.DrawLine(
                new Vector3(startX - stepsX * _gridSize, y, 0),
                new Vector3(startX + stepsX * _gridSize, y, 0));
        }
        Handles.color = Color.white;
    }

    // =====================================================================
    // ユーティリティ
    // =====================================================================

    void DrawSceneLabel(SceneView sv)
    {
        int idx   = (int)_selected;
        Color col = ElementColors[idx];
        Handles.BeginGUI();
        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize  = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = col }
        };
        string mode = _dragMode ? "ドラッグ" : "クリック";
        string text = $"[ステージエディタ]  {ShortNames[idx]}  |  {mode}配置  |  右クリック=削除";
        GUI.Box(new Rect(8, 8, 480, 24), text, style);
        Handles.EndGUI();
    }

    void DrawRect(Vector3 center, Vector2 size, Color fill, Color outline)
    {
        float hw = size.x * 0.5f, hh = size.y * 0.5f;
        var verts = new Vector3[]
        {
            center + new Vector3(-hw, -hh, 0),
            center + new Vector3(-hw,  hh, 0),
            center + new Vector3( hw,  hh, 0),
            center + new Vector3( hw, -hh, 0),
        };
        Handles.DrawSolidRectangleWithOutline(verts, fill, outline);
    }

    Vector3 SnapToGrid(Vector3 pos)
    {
        if (!_snapToGrid) return pos;
        return new Vector3(
            Mathf.Round(pos.x / _gridSize) * _gridSize,
            Mathf.Round(pos.y / _gridSize) * _gridSize,
            0f);
    }

    Transform GetOrCreateStageParent()
    {
        var found = GameObject.Find("Stage");
        if (found != null) return found.transform;
        var go = new GameObject("Stage");
        Undo.RegisterCreatedObjectUndo(go, "Create Stage Parent");
        return go.transform;
    }

    static void TrySetTag(GameObject go, string tag)
    {
        try   { go.tag = tag; }
        catch { Debug.LogWarning($"[StageEditor] タグ \"{tag}\" が存在しません。「タグ再確認」ボタンを押してください。"); }
    }

    // =====================================================================
    // 必要なタグを自動追加
    // =====================================================================

    static readonly string[] RequiredTags =
    {
        "Wall", "Enemy", "GravityGate", "Boost", "CheckPoint", "Death"
    };

    static void EnsureRequiredTags()
    {
        var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets == null || tagManagerAssets.Length == 0) return;

        var tagManager = new SerializedObject(tagManagerAssets[0]);
        var tagsProp   = tagManager.FindProperty("tags");

        bool changed = false;
        foreach (var required in RequiredTags)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == required)
                { found = true; break; }
            }
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = required;
                changed = true;
                Debug.Log($"[StageEditor] タグ \"{required}\" を追加しました。");
            }
        }

        if (changed) tagManager.ApplyModifiedProperties();
    }

    // =====================================================================
    // 白スプライト生成
    // =====================================================================

    static Sprite GetOrCreateWhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
        {
            name        = "StageEditor_WhiteSquare",
            filterMode  = FilterMode.Point,
            wrapMode    = TextureWrapMode.Clamp
        };
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        _whiteSprite = Sprite.Create(
            tex,
            new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f),
            4f);
        _whiteSprite.name = "StageEditor_WhiteSquare";
        return _whiteSprite;
    }
}
