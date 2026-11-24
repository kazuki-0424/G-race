using UnityEngine;

/// <summary>
/// ステージエディタで配置した各要素に付与するコンポーネント。
/// 要素の種類を記録し、後でプレハブへ差し替えるときに使用する。
/// </summary>
public class StageElementInfo : MonoBehaviour
{
    public enum ElementType
    {
        Floor       = 0,
        Ceiling     = 1,
        Obstacle    = 2,
        Enemy       = 3,
        GravityGate = 4,
        Boost       = 5,
        Checkpoint  = 6,
        Death       = 7
    }

    [Tooltip("このオブジェクトの要素種別")]
    public ElementType elementType;

    [Tooltip("true のとき、プレハブ差し替え対象から除外される")]
    public bool excludeFromReplace = false;
}
