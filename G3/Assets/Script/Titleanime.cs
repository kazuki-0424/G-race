using UnityEngine;

public class Titleanime : MonoBehaviour
{
    [Header("HoberRacerの移動設定")]
    [SerializeField] private Transform HoberRacer;  // インスペクターでHoberRacerをアタッチ
    [SerializeField] private float MoveSpeed = 3f;  // 移動速度

    // 移動ルートの定義
    // ルート1: X-10 Y-2  →  X10 Y-2  (下段・左→右)
    // ルート2: X10  Y0   →  X-10 Y0  (上段・右→左、画像を180度回転)

    private Vector3 _startPosBottom = new Vector3(-10f, -2f, 0f);
    private Vector3 _endPosBottom   = new Vector3( 10f, -2f, 0f);
    private Vector3 _startPosTop    = new Vector3( 10f,  0f, 0f);
    private Vector3 _endPosTop      = new Vector3(-10f,  0f, 0f);

    // フェーズ管理
    // 0: 下段を左→右に移動中 (Y=-2, X: -10 → 10)
    // 1: 上段を右→左に移動中 (Y=0,  X: 10 → -10, 180度回転)
    private int _phase = 0;

    void Start()
    {
        if (HoberRacer == null)
        {
            Debug.LogError("[Titleanime] HoberRacerがアタッチされていません！");
            return;
        }
        // 下段スタート位置に配置・向きをリセット
        HoberRacer.position = _startPosBottom;
        HoberRacer.rotation = Quaternion.identity;
        _phase = 0;
    }

    void Update()
    {
        if (HoberRacer == null) return;

        switch (_phase)
        {
            case 0:
                // 下段: X-10 Y-2 → X10 Y-2 (向き: 通常)
                MoveTowards(_endPosBottom);
                if (Vector3.Distance(HoberRacer.position, _endPosBottom) < 0.05f)
                {
                    HoberRacer.position = _endPosBottom;
                    // 180度回転してから上段へ
                    HoberRacer.rotation = Quaternion.Euler(0f, 0f, 180f);
                    HoberRacer.position = _startPosTop;
                    _phase = 1;
                }
                break;

            case 1:
                // 上段: X10 Y0 → X-10 Y0 (向き: 180度回転中)
                MoveTowards(_endPosTop);
                if (Vector3.Distance(HoberRacer.position, _endPosTop) < 0.05f)
                {
                    HoberRacer.position = _endPosTop;
                    // 向きを元に戻して下段スタートへ
                    HoberRacer.rotation = Quaternion.identity;
                    HoberRacer.position = _startPosBottom;
                    _phase = 0;
                }
                break;
        }
    }

    private void MoveTowards(Vector3 target)
    {
        HoberRacer.position = Vector3.MoveTowards(
            HoberRacer.position,
            target,
            MoveSpeed * Time.deltaTime
        );
    }
}
