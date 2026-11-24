using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMove_2D : MonoBehaviour
{
    [Header("移動系")]
    [SerializeField] bool MoveOn = true;
    [SerializeField] float SpeedBuf = 0.05f;
    [SerializeField] float SpeedMax = 5f;
    [SerializeField] float Speed;
    [SerializeField] float Boost = 0f;
    [SerializeField] int Boost_Max = 1200;
    [SerializeField] int Boost_Cost = 240;
    [SerializeField] int Boost_Gage = 0;
    [SerializeField] Transform MachinePos;
    [SerializeField] Rigidbody2D MachineRb;
    [Header("エフェクト(色系)")]
    [SerializeField] SpriteRenderer MachineRenderer;
    [SerializeField] Color MachineColor;
    [SerializeField] Color GravityColor;
    [SerializeField] Color BoostColor;
    [SerializeField] Color DamageColor;
    [Header("ジャンプ用")]
    [SerializeField] bool JUMP = true;
    [SerializeField] int jump_move = 0;
    [SerializeField] int jump_count = 0;
    [SerializeField] float jump_y = 0;
    [Header("HP・ダメージ用")]
    [SerializeField] int hp = 0;
    [SerializeField] int hpmax = 100;
    [SerializeField] int damage = 40;
    [Header("リスポーン・リセット用")]
    [SerializeField] Vector2 RespawnPos_First;
    [SerializeField] Vector2 RespawnPos;
    [SerializeField] int RespawnCount = 0;
    [SerializeField] int RespawnCT = 120;
    [Header("重力用")]
    [SerializeField] bool RespawnGravity = false;
    [SerializeField] int Gravity_y = 1;
    [SerializeField] bool GRAVITY_CTR;
    [Header("UI")]
    [SerializeField] bool UION;
    [SerializeField] GameObject GravityUI;
    [SerializeField] Slider HPGage;
    [SerializeField] Slider BoostGage;
    // 移動SEの再生管理（押しっぱなしで連続再生しないようにするフラグ）
    private bool _isMoveSEPlaying = false;
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

    private void FixedUpdate()
    {
        if (MoveOn) Move();//移動系まとめ
    }

    void Update()
    {
        if (MachineRb.gravityScale < 0)
            Gravity_y = -1;
        else
            Gravity_y = 1;

        if (Boost_Gage < Boost_Max)
            Boost_Gage++;

        if (UION)
        {
            GravityUI.SetActive(GRAVITY_CTR);
            HPGage.value = (float)hp / hpmax;
            BoostGage.value = (float)Boost_Gage / Boost_Max;
        }

        if (hp <= 0 && MoveOn)
        {
            hp = 0;
            MachineColor = new Color(0.5f, 0.5f, 0.5f);
            MoveOn = false;
        }
        if (MoveOn == false)
            RespawnCount++;
        if (RespawnCount == RespawnCT)
            Respawn();

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
            // 移動SE: 押し始めた瞬間だけ再生（押しっぱなしで連続再生しない）
            if (!_isMoveSEPlaying)
            {
                AudioManager.Instance?.PlayMove();
                _isMoveSEPlaying = true;
            }
        }
        else if (Input.GetKey(KeyCode.A))
        {
            if (Speed > 0)
                Speed = 0;
            if (Speed > -SpeedMax * 0.5f)
                Speed -= SpeedBuf * 0.5f;
            else
                Speed = -SpeedMax * 0.5f;
            // 移動SE: 押し始めた瞬間だけ再生（押しっぱなしで連続再生しない）
            if (!_isMoveSEPlaying)
            {
                AudioManager.Instance?.PlayMove();
                _isMoveSEPlaying = true;
            }
        }
        else
        {
            // キーを離したらSEフラグをリセット（次に押したとき再度鳴る）
            _isMoveSEPlaying = false;
            
            if (Speed != 0f)
            {
                Speed = Speed * 0.95f;
                if (Speed <= SpeedBuf && -SpeedBuf * 0.5f <= Speed)
                    Speed = 0f;
            }
        }
        // Rigidbody2Dで移動
        MachineRb.linearVelocity = new Vector2(Speed + Boost, jump_y * Gravity_y);
        //----移動系----

        if (Boost != 0)
        {
            Boost -= SpeedBuf*2;
            if (Boost < 0)
                Boost = 0;
        }

        //手動ブースト
        if (Input.GetKeyDown(KeyCode.Q) && Boost_Gage >= Boost_Cost)
        {
            BOOST();
            Boost_Gage -= Boost_Cost;
            if (Boost_Gage < 0)
                Boost_Gage = 0;
        }
        //----ブースト----

        //重力反転・ジャンプ
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GRAVITY_CTR)
            {
                GRAVITY();
            }
            else if(JUMP)
            {
                MachineColor = BoostColor;
                //2段ジャンプ
                if (jump_move != 0)
                    JUMP = false;
                //1段ジャンプ
                //JUMP = false;
                jump_move = 1;
                jump_y = 7.5f;
            }
        }

        if (jump_move == 1)
        {
            jump_count++;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                jump_y += 0.4f;
            }
            if(Input.GetKeyUp(KeyCode.RightShift)||jump_count == 15)
            {
                jump_count = 0;
                jump_move = 2;
            }
        }
        if (jump_move !=1 && jump_y > 0)
        {
            jump_y -= 0.2f;
            if (jump_y < 0)
            {
                jump_y = 0;
            }
        }
        //----重力----

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
    }

    private void Respawn()//リスポーン
    {
        MoveOn = true;
        RespawnCount = 0;
        MachinePos.position = new Vector3(RespawnPos.x, RespawnPos_First.y, 0);
        MachinePos.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        // Rigidbody2Dの速度をリセット
        MachineRb.linearVelocity = Vector2.zero;
        MachineRb.angularVelocity = 0f;
        // ジャンプ関連の状態をリセット
        jump_y = 0f;
        jump_move = 0;
        jump_count = 0;
        JUMP = true;
        GRAVITY_CTR = RespawnGravity;
        if (MachineRb.gravityScale < 0)
            MachineRb.gravityScale = -MachineRb.gravityScale;
        Boost_Gage = Boost_Max;
        Speed = 0f;
        Boost = 0f;
        hp = hpmax;
        MachineColor = Color.green;
    }
    
    private void GRAVITY()//重力反転
    {
        Debug.Log("重力反転！");
        AudioManager.Instance?.PlayGravity();
        MachineColor = GravityColor;
        JUMP = false;
        MachinePos.localScale =
            new Vector3(MachinePos.localScale.x,
            -MachinePos.localScale.y, MachinePos.localScale.z);
        MachineRb.gravityScale = MachineRb.gravityScale * -1;
    }

    private void BOOST()//ブースト
    {
        Debug.Log("ブースト！");
        MachineColor = BoostColor;
        Boost = SpeedMax * 1f;
    }

    private void Reset()//リセット
    {
        MachinePos.position = new Vector3(RespawnPos_First.x, RespawnPos_First.y, 0);
        RespawnPos = new Vector2(MachinePos.position.x, MachinePos.position.y);
        RespawnGravity = false;
        Respawn();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (JUMP == false)
        {
            JUMP = true;
            jump_move = 0;
            jump_count = 0;
        }
        if(other.gameObject.CompareTag("Wall"))
        {
            AudioManager.Instance?.PlayCollision();
            Debug.Log("障害物激突");
            MachineColor = BoostColor;
            hp -= damage;
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
            float a = 0;
            if (Speed + Boost > 0)
                a = 5.4f;
            else
                a = -5.4f;
            MachinePos.position += new Vector3(a, 0, 0);
            GRAVITY_CTR = !GRAVITY_CTR;
            MachineColor = new Color(1, 1, 0, 1);
        }
        // 触れた相手のタグが "Player" だった場合
        else if (other.CompareTag("CheckPoint"))
        {
            Debug.Log("チェックポイント！");
            RespawnPos.x = MachinePos.position.x;
            MachineColor = Color.green;
            RespawnGravity = GRAVITY_CTR;
        }
        // 触れた相手のタグが "Player" だった場合
        else if (other.CompareTag("Death"))
        {
            Debug.Log("ﾀﾋ");
            hp = 0;
        }
    }
}
