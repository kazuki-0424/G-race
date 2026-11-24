using JetBrains.Annotations;
using System.Security.Cryptography;
using Unity.XR.Oculus.Input;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent (typeof(Rigidbody))]
public class PlayerMove_3D : MonoBehaviour
{
    [SerializeField] float speedBuf = 0.02f;
    [SerializeField] float DriftSpeedDebuf;
    public float moveSpeed = 3f;
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

    //リセット用の変数
    public Vector3 InitTrans;

    //HP系の変数
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
                maxspeed = 1.5f;
            }
            else
            {
                maxspeed = 2f;
            }
            if (speed > maxspeed) speed = speed * 0.8f;
        }
        else maxspeed = 3;

        if (speed > maxspeed - speedBuf && speed < maxspeed + speedBuf)
            speed = maxspeed;

        // ===== 速度計算 =====
        float driftBonus = isDrifting ? 1.2f : 1f;
        float currentSpeed = moveSpeed * speed * 2 * boost * driftBonus;


        if (Input.GetKey(KeyCode.S))
        {
            currentSpeed = -moveSpeed * 4;
            if (speed > 1f) speed += speedBuf;
        }
        else if(Input.GetKey(KeyCode.W))
        {
            if (speed < maxspeed)
            {
                speed += speedBuf;
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
            rb.position = InitTrans;
            tf.rotation = Quaternion.identity;
        }
    }

    void OnTriggerEnter(Collider collision)
    {

    }
}