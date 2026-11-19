using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMove_2D : MonoBehaviour
{
    [Header("移動系")]
    [SerializeField] float SpeedBuf = 0.05f;
    [SerializeField] float SpeedMax = 5f;
    [SerializeField] float Speed;
    [SerializeField] Transform MachinePos;
    [SerializeField] Rigidbody2D MachineRb;
    [Header("エフェクト(色系)")]
    [SerializeField] SpriteRenderer MachineRenderer;
    [SerializeField] Color MachineColor;
    [SerializeField] Color GravityColor;
    [SerializeField] Color BoostColor;
    [SerializeField] Color DamageColor;
    [Header("ギミック用")]
    [SerializeField] int hp = 0;
    [SerializeField] int hpmax = 100;
    [SerializeField] float Boost = 0f;
    [SerializeField] int Boost_Max = 1200;
    [SerializeField] int Boost_Cost = 240;
    [SerializeField] int Boost_Gage = 0;
    [SerializeField] Vector2 RespawnPos_First;
    [SerializeField] Vector2 RespawnPos;
    [SerializeField] bool GRAVITY_CTR;
    [Header("UI")]
    [SerializeField] bool UION;
    [SerializeField] GameObject GravityUI;
    [SerializeField] Slider HPGage;
    [SerializeField] Slider BoostGage;

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
        Reset();
    }

    void Update()
    {
        Move();//移動系まとめ

        if (Boost_Gage < Boost_Max)
            Boost_Gage++;

        if (UION)
        {
            GravityUI.SetActive(GRAVITY_CTR);
            HPGage.value = (float)hp/ hpmax;
            BoostGage.value = (float)Boost_Gage/Boost_Max;
        }


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
        if (Input.GetKeyDown(KeyCode.Q) && Boost_Gage >= Boost_Cost)
        {
            BOOST();
            Boost_Gage -= Boost_Cost;
            if (Boost_Gage < 0)
                Boost_Gage = 0;
        }
        //----重力----
        //重力反転
        if (Input.GetKeyDown(KeyCode.LeftShift)&&GRAVITY_CTR)
        {
            GRAVITY();
        }
        //----重力----
    }

    private void Respawn()//リスポーン
    {
        MachinePos.position = new Vector3(RespawnPos.x, RespawnPos.y, 0);
        MachinePos.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        if (MachineRb.gravityScale < 0)
            MachineRb.gravityScale = -MachineRb.gravityScale;
        Speed = 0f;
        Boost = 0f;
        MachineColor = Color.green;
    }
    
    private void GRAVITY()//重力反転
    {
        Debug.Log("重力反転！");
        MachineColor = GravityColor;
        MachinePos.localScale =
            new Vector3(MachinePos.localScale.x,
            -MachinePos.localScale.y, MachinePos.localScale.z);
        MachineRb.gravityScale = MachineRb.gravityScale * -1;
    }

    private void BOOST()//ブースト
    {
        Debug.Log("ブースト！");
        MachineColor = BoostColor;
        Boost = SpeedMax * 0.5f;
    }

    private void Reset()//リセット
    {
        MachinePos.position = new Vector3(RespawnPos_First.x, RespawnPos_First.y, 0);
        RespawnPos = new Vector2(MachinePos.position.x, MachinePos.position.y);
        MachinePos.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        GRAVITY_CTR = false;
        if (MachineRb.gravityScale < 0)
            MachineRb.gravityScale = -MachineRb.gravityScale;
        Speed = 0f;
        Boost = 0f;
        Boost_Gage = Boost_Max;
        hp = hpmax;
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
            BOOST();
        }
        // 触れた相手のタグが "Player" だった場合
        else if (other.CompareTag("Gravity"))
        {
            GRAVITY();
        }
        // 触れた相手のタグが "Player" だった場合
        else if (other.CompareTag("GravityGate"))
        {
            Debug.Log("重力操作切替");
            GRAVITY_CTR = !GRAVITY_CTR;
            MachineColor = new Color(1, 1, 0, 1);
        }
        // 触れた相手のタグが "Player" だった場合
        else if (other.CompareTag("CheckPoint"))
        {
            Debug.Log("チェックポイント！");
            RespawnPos.x = MachinePos.position.x;
            MachineColor = Color.green;
        }
    }
}
