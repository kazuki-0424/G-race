using UnityEngine;
using UnityEditor;

/// <summary>
/// ステージエディタで配置した StageElementInfo の Inspector 表示を拡張する。
/// </summary>
[CustomEditor(typeof(StageElementInfo))]
public class StageElementInfoEditor : Editor
{
    // 要素ごとの色（StageEditorWindow と同じ値を使用）
    static readonly Color[] ElementColors =
    {
        new Color(0.55f, 0.37f, 0.19f), // Floor
        new Color(0.25f, 0.35f, 0.55f), // Ceiling
        new Color(0.80f, 0.20f, 0.20f), // Obstacle
        new Color(0.90f, 0.50f, 0.00f), // Enemy
        new Color(0.60f, 0.00f, 0.90f), // GravityGate
        new Color(0.00f, 0.85f, 0.85f), // Boost
        new Color(0.10f, 0.80f, 0.10f), // Checkpoint
        new Color(0.95f, 0.10f, 0.10f), // Death
    };

    static readonly string[] ElementJapanese =
    {
        "床", "天井", "障害物", "敵",
        "重力ゲート", "ブースト", "チェックポイント", "死亡ゾーン"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var info = (StageElementInfo)target;
        int idx = (int)info.elementType;

        // カラーバッジ
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = ElementColors[idx];
        EditorGUILayout.LabelField(
            $"  ■  {ElementJapanese[idx]}  ({info.elementType})",
            EditorStyles.boldLabel
        );
        GUI.backgroundColor = prev;

        EditorGUILayout.Space(4);
        DrawDefaultInspector();
        EditorGUILayout.Space(6);

        // ステージエディタを開くボタン
        if (GUILayout.Button("ステージエディタを開く"))
            StageEditorWindow.Open();
    }
}
