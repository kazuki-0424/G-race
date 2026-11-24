
using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("追従対象")]
    [SerializeField] private Transform player;

    [Header("カメラ設定")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);
    [SerializeField] private float followSpeed = 5f;

    [Header("追従する座標")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = false;
    [SerializeField] private bool followZ = true;

    private Vector3 targetPosition;

    void LateUpdate()
    {
        if (player == null) return;

        // 現在のカメラ座標を基準にする
        targetPosition = transform.position;

        // 追従する座標だけ更新
        if (followX)
            targetPosition.x = player.position.x + offset.x;

        if (followY)
            targetPosition.y = player.position.y + offset.y;

        if (followZ)
            targetPosition.z = player.position.z + offset.z;

        // 滑らかに移動
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
    }
}
