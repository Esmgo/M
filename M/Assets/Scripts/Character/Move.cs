using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : NetworkBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;
    public float dashSpeed = 12f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isDashing = false;
    private float dashTimeLeft;
    private float lastDashTime = -999f;

    private Camera cam;
    private bool facingRight = true; // 初始朝右
    private Animator animator;

    [SyncVar]
    private float syncedSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        animator = GetComponent<Animator>();
        // 插值设置建议在Inspector中设置
    }

    private void Start()
    {
        //测试
        //cam.GetComponent<CameraFollow>().target = transform;
        cam.GetComponent<CameraFollow>().SetTarget(); // 设置摄像头跟随目标为本地玩家
    }

    void Update()
    {
        if (!isLocalPlayer) return; // 只处理本地玩家的输入
        // 获取移动输入
        moveInput.x = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
        moveInput.y = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        moveInput = moveInput.normalized;

        // 冲刺输入
        if (Input.GetKeyDown(KeyCode.Space) && !isDashing && Time.time >= lastDashTime + dashCooldown && moveInput != Vector2.zero)
        {
            isDashing = true;
            dashTimeLeft = dashDuration;
            lastDashTime = Time.time;
        }

        // 鼠标位置反转
        FlipByMouse();

        // 计算速度并设置动画参数
        float speed = rb.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        // 同步速度到服务器
        if (isServer)
        {
            syncedSpeed = speed;
        }
        else
        {
            CmdSyncSpeed(speed);
        }
    }

    [Command]
    private void CmdSyncSpeed(float speed)
    {
        syncedSpeed = speed;
    }

    void FixedUpdate()
    {
        Vector2 targetVelocity = isDashing ? moveInput * dashSpeed : moveInput * moveSpeed;
        rb.velocity = targetVelocity;

        if (isDashing)
        {
            dashTimeLeft -= Time.fixedDeltaTime;
            if (dashTimeLeft <= 0)
            {
                isDashing = false;
            }
        }

        // 在客户端更新动画参数
        if (!isLocalPlayer)
        {
            animator.SetFloat("Speed", syncedSpeed);
        }
    }

    void FlipByMouse()
    {
        if (cam == null) return;
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        float mouseX = mouseWorldPos.x;
        float playerX = transform.position.x;

        // 鼠标在右侧，人物朝右；鼠标在左侧，人物朝左
        if (mouseX > playerX && !facingRight)
        {
            Flip();
        }
        else if (mouseX < playerX && facingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
