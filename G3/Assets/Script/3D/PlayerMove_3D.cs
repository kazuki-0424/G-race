using JetBrains.Annotations;
using System.Security.Cryptography;
using Unity.XR.Oculus.Input;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent (typeof(Rigidbody))]
public class PlayerMove_3D : MonoBehaviour
{

    [Header("移動系")]
    [SerializeField] bool OnMove = true;
    [SerializeField] float speedBuf = 0.02f;
    public float moveSpeed = 3f;
    public float turnSpeed = 100f;
    private Rigidbody rb;
    private Transform tf;

    [Space]
    public float boost = 0f;
    public float speed = 1f;
    public float maxspeed = 5;

    [Header("ドリフト系の変数")]
    bool isDriftingInput;
    public bool isDrifting = false;
    [SerializeField] float DriftSpeedDebuf = 0;
    [SerializeField] float DriftSpeedMax= 0;
    [SerializeField] int DriftCount = 0;
    [SerializeField] int DriftCount_Low = 60;

    [Header("リセット用の変数")]
    public Vector3 InitTrans;

    [Header("HP系の変数")]
    public int HP = 100;
    public int MaxHP = 100;

    void Start()
    {
        tf = GetComponent<Transform>();
        rb = GetComponent<Rigidbody>();
        Application.targetFrameRate = 60;
    }

    private void Update()
    {

    }

    void FixedUpdate()
    {
        if (OnMove) Move();
    }

    private void Move()
    {
        isDriftingInput = Input.GetKey(KeyCode.LeftShift);
        // 入力
        float turn = Input.GetAxis("Horizontal");
        isDrifting = isDriftingInput;

        // 回転（ドリフトで強化）
        float turnPower = isDrifting ? turnSpeed * 1.5f : turnSpeed;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, turn * turnPower * Time.fixedDeltaTime, 0));

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            if (speed > maxspeed * 0.4f)
            {
                if (isDrifting)
                {
                    DriftSpeedMax = maxspeed * 0.3f;
                }
                else
                {
                    DriftSpeedMax = maxspeed * 0.2f;
                }
            }
            if(isDrifting)DriftCount++;
            if (DriftSpeedDebuf < DriftSpeedMax)
                DriftSpeedDebuf += speedBuf;
            if (DriftSpeedDebuf > DriftSpeedMax)
                DriftSpeedDebuf = DriftSpeedMax;
        }
        else
        {
            if (DriftCount!=0)
            {
                if (DriftCount > DriftCount_Low)
                {
                    boost = (float)DriftCount / DriftCount_Low * 2;
                }
                DriftCount = 0;
            }
            if (DriftSpeedDebuf != 0f)
                DriftSpeedDebuf = DriftSpeedDebuf * 0.8f;
            if (DriftSpeedDebuf < 0.001)
                DriftSpeedDebuf = 0f;
        }

        if (boost > 0f)
        {
            boost -= speedBuf * 10;
            if (boost < 0.0001f)
                boost = 0;
        }

        // ===== 速度計算 =====
        float currentSpeed = moveSpeed * (speed + boost - DriftSpeedDebuf);

        if (Input.GetKey(KeyCode.S))
        {
            if(speed>0)
                speed = 0;
            if (speed > -maxspeed * 0.5f) speed -= speedBuf * 0.5f;
        }
        else if(Input.GetKey(KeyCode.W))
        {
            if(speed<0)
                speed = 0;
            if (speed < maxspeed)
            {
                speed += speedBuf;
            }
        }
        else
        {
            
            if (speed != 0f)
            {
                speed = speed * 0.95f;
                if (speed <= speedBuf && -speedBuf * 0.5f <= speed)
                    speed = 0f;
            }
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

        rb.angularVelocity = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.R))
        {
            rb.position = InitTrans;
            tf.rotation = Quaternion.identity;
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.tag == "Boost")
        {
            boost += maxspeed * 1.5f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "wall")
        {
            speed -= maxspeed * 0.5f;
            HP -= 30;
        }
    }

}