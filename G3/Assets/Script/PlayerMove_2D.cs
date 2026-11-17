using Unity.VisualScripting;
using UnityEngine;

public class PlayerMove_2D : MonoBehaviour
{
    [SerializeField] float Speed;
    [SerializeField] Transform MachinePos;
    [SerializeField] Rigidbody2D MachineRb;
    [SerializeField] SpriteRenderer MachineRenderer;
    [SerializeField] Color MachineColor;
    [SerializeField] Color GravityColor;
    [SerializeField] Color BoostColor;
    [SerializeField] Color DamageColor;
    [SerializeField] float SpeedBuf = 0.05f;
    [SerializeField] float SpeedMax = 5f;
    [SerializeField] float Boost = 0f;
    [SerializeField] Vector2 RespawnPos_First;
    [SerializeField] Vector2 RespawnPos;

    void Awake()
    {
        // VSyncをオフにする
        QualitySettings.vSyncCount = 0;

        // フレームレートを60に固定する
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        RespawnPos_First = new Vector2(MachinePos.position.x, MachinePos.position.y);
        RespawnPos = new Vector2(MachinePos.position.x, MachinePos.position.y);
    }

    void Update()
    {
        Move();//移動系まとめ


        //色を元に戻す
        if (MachineColor.g != 1f)
        {
            MachineColor.g += 0.02f;
            if (MachineColor.g > 1f)
                MachineColor.g = 1f;
        }
        if (MachineColor.r != 1f)
        {
            MachineColor.r += 0.02f;
            if (MachineColor.r > 1f)
                MachineColor.r = 1f;
        }
        if (MachineColor.b != 1f)
        {
            MachineColor.b += 0.02f;
            if (MachineColor.b > 1f)
                MachineColor.b = 1f;
        }
        MachineRenderer.color = MachineColor;
        //----色----

        if (Input.GetKeyDown(KeyCode.R))
            Respawn();

        if (Input.GetKeyDown(KeyCode.E))
            Reset();
    }

    private void Move()//移動や重力系
    {
        //前進・後退・減速など
        if (Input.GetKey(KeyCode.D))
        {
            if (Speed < 0)
                Speed = 0;
            if (Speed < SpeedMax)
                Speed += SpeedBuf;
            else
                Speed = SpeedMax;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (Speed > 0)
                Speed = 0;
            if (Speed > -SpeedMax * 0.5f)
                Speed -= SpeedBuf * 0.5f;
            else
                Speed = -SpeedMax * 0.5f;
        }
        else
        {
            if (Speed != 0f)
            {
                Speed = Speed * 0.95f;
                if (Speed <= SpeedBuf && -SpeedBuf * 0.5f <= Speed)
                    Speed = 0f;
            }
        }
        // Rigidbody2Dで移動
        MachineRb.linearVelocity = new Vector2(Speed + Boost, 0);
        //----移動系----

        if (Boost != 0)
        {
            Boost -= SpeedBuf;
            if (Boost < 0)
                Boost = 0;
        }

        //重力反転
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Debug.Log("重力反転！");
            MachineColor = GravityColor;
            MachinePos.localScale =
                new Vector3(MachinePos.localScale.x,
                -MachinePos.localScale.y, MachinePos.localScale.z);
            MachineRb.gravityScale = MachineRb.gravityScale * -1;
        }
        //----重力----
    }

    private void Respawn()
    {
        MachinePos.position = new Vector3(RespawnPos.x, RespawnPos.y, 0);
        MachinePos.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        if (MachineRb.gravityScale < 0)
            MachineRb.gravityScale = -MachineRb.gravityScale;
        Speed = 0f;
        Boost = 0f;
        MachineColor = Color.green;
    }
    private void Reset()
    {
        MachinePos.position = new Vector3(RespawnPos_First.x, RespawnPos_First.y, 0);
        RespawnPos = new Vector2(MachinePos.position.x, MachinePos.position.y);
        MachinePos.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        if (MachineRb.gravityScale < 0)
            MachineRb.gravityScale = -MachineRb.gravityScale;
        Speed = 0f;
        Boost = 0f;
        MachineColor = Color.green;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            Debug.Log("障害物激突");
            MachineColor = BoostColor;
            Speed -= SpeedMax * 0.5f;
            if (other.gameObject.layer == 3)
                Destroy(other.gameObject);
            MachineColor = DamageColor;
        }
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        // 触れた相手のタグが "Player" だった場合
        if (other.CompareTag("Boost"))
        {
            Debug.Log("ブースト！");
            MachineColor = BoostColor;
            Boost = SpeedMax * 0.5f;
        }
        // 触れた相手のタグが "Player" だった場合
        if (other.CompareTag("CheckPoint"))
        {
            Debug.Log("チェックポイント！");
            RespawnPos.x = MachinePos.position.x;
            MachineColor = Color.green;
        }
    }

}
