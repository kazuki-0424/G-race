using JetBrains.Annotations;
using System.Security.Cryptography;
using Unity.XR.Oculus.Input;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent (typeof(Rigidbody))]
public class PlayerMove_3D : MonoBehaviour
{

    public float moveSpeed = 5f;
    public float turnSpeed = 100f;
    private Rigidbody rb;
    private Transform tf;

    public float boost = 1f;
    public float speed = 1f;
    public float maxspeed = 5;

    //ドリフト系の変数
    bool isDriftingInput;
    public bool isDrifting = false;
    public float driftGrip = 0.3f;   // 小さいほど滑る
    public float normalGrip = 1.0f;
    public float driftTurnMultiplier = 1.5f;
    public float driftBoostPower = 1.2f;

    //ラップ・ゴール判定の変数
    public int rap = 1;
    public int rapmax = 0;
    public bool goal = false;
    //public Text raptext;
    //public GameObject GoalUI;
    public int checkpoint = 0;
    public int checkpointmax = 0;

    //リセット用の変数
    public Vector3 InitTrans;

    //HP系の変数
    public int HP = 100;
    public int MaxHP = 100;
    //public Slider HPSlider;

    //ギア系の変数
    //public Slider GearSlider;
    //public Text GearText;
    public int GEAR = 1;
    public float gearboost = 1.2f;
    public int gearenergy = 0;

    void Start()
    {
        tf = GetComponent<Transform>();
        rb = GetComponent<Rigidbody>();
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        isDriftingInput = Input.GetKey(KeyCode.Space);

    }

    void FixedUpdate()
    {

        // 入力
        float turn = Input.GetAxis("Horizontal");
        isDrifting = isDriftingInput;

        // 回転（ドリフトで強化）
        float turnPower = isDrifting ? turnSpeed * 1.5f : turnSpeed;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, turn * turnPower * Time.fixedDeltaTime, 0));

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            if (isDrifting)
            {
                maxspeed = 3.5f;
                if (speed > maxspeed) speed = speed * 0.96f;
            }
            else
            {
                maxspeed = 4f;
                if (speed > maxspeed) speed = speed * 0.98f;
            }
        }
        else maxspeed = 5;

        // ===== 速度計算 =====
        float driftBonus = isDrifting ? 1.2f : 1f;
        float currentSpeed = moveSpeed * speed * 2 * boost * gearboost * driftBonus;


        if (Input.GetKey(KeyCode.S))
        {
            currentSpeed = -moveSpeed * 4;
            if (speed > 1f) speed = speed * 0.75f;
        }
        else if(Input.GetKey(KeyCode.W))
        {
            if (speed < maxspeed)
            {
                speed = speed * 1.05f;
            }
        }
        else if (!Input.GetKey(KeyCode.W))
        {
            currentSpeed = 0f;
        }

        Vector3 forwardVel = transform.forward * currentSpeed;

        // 横滑り
        Vector3 sideVel = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);
        float grip = isDrifting ? 0.3f : 0f;

        // Y保持
        float yVel = rb.linearVelocity.y;

        Vector3 newVel = forwardVel + sideVel * grip;
        newVel.y = yVel;

        rb.linearVelocity = newVel;


        // ===== boost減衰 =====
        if (boost > 1f)
        {
            boost -= 1.5f * Time.fixedDeltaTime;
            if (boost < 1f) boost = 1f;
        }

        boost = Mathf.Clamp(boost, 1f, 3f);

        rb.angularVelocity = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.R))
        {
            GEAR = 1;
            gearenergy = 0;
            gearboost = 1+GEAR*0.075f;
            rb.position = InitTrans;
            tf.rotation = Quaternion.identity;
            //GearSlider.value = gearenergy/100f;
            //GearText.text = "G1";
            HP = MaxHP;
            //HPSlider.value = HP/100f;
            rap = 1;
            //raptext.text = rap + "/" + rapmax;
            goal = false;
            //GoalUI.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "boost")
        {
            boost += 1f;
        }
        else if (collision.gameObject.tag == "energy")
        {
            gearenergy += 18;
            if (gearenergy >= 100 && GEAR < 5)
            {
                boost += 0.5f;
                GEAR++;
                gearenergy -= 100;
                gearboost = 1+GEAR * 0.05f;
                //GearText.text = "G" + GEAR;
            }
            if (gearenergy >= 100) gearenergy = 100;
            //GearSlider.value = gearenergy/100f;
            Destroy(collision.gameObject);
        }
        else if (collision.gameObject.tag == "damage")
        {
            HP -= 5;
            speed = 2f;
            gearenergy -=10;
            if(gearenergy < 0 && GEAR==1)gearenergy = 0;
            if (gearenergy < 0)
            {
                gearenergy += 100;
                GEAR--;
                gearboost = 1+GEAR * 0.075f;
                //GearText.text = "G" + GEAR;
            }
            //GearSlider.value = gearenergy/100f;
            //HPSlider.value = HP/100f;
            Destroy(collision.gameObject);
        }
        else if (collision.gameObject.tag == "goal")
        {
            if (checkpoint == checkpointmax)
            {
                //if (rap < rapmax) { rap++; raptext.text = rap + "/" + rapmax; }
                //else goal = true;
            }
            checkpoint = 0;
            if (goal)
            {
                //GoalUI.SetActive(true);
            }
        }
        else if (collision.gameObject.tag == "checkpoint")
        {
            int c = collision.gameObject.layer-5;
            if (checkpoint >= c - 1)
            {
                checkpoint = c;
            }
        }
    }

    void UiDraw()
    {
        //GearText.text = "G" + GEAR;
        //GearSlider.value = gearenergy / 100f;
        //HPSlider.value = HP / 100f;
    }
}