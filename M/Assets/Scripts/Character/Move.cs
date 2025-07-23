using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : NetworkBehaviour
{
    [Header("�ƶ�����")]
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
    private bool facingRight = true; // ��ʼ����
    private Animator animator;

    [SyncVar]
    private float syncedSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        animator = GetComponent<Animator>();
        // ��ֵ���ý�����Inspector������
    }

    private void Start()
    {
        //����
        //cam.GetComponent<CameraFollow>().target = transform;
        cam.GetComponent<CameraFollow>().SetTarget(); // ��������ͷ����Ŀ��Ϊ�������
    }

    void Update()
    {
        if (!isLocalPlayer) return; // ֻ��������ҵ�����
        // ��ȡ�ƶ�����
        moveInput.x = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
        moveInput.y = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        moveInput = moveInput.normalized;

        // �������
        if (Input.GetKeyDown(KeyCode.Space) && !isDashing && Time.time >= lastDashTime + dashCooldown && moveInput != Vector2.zero)
        {
            isDashing = true;
            dashTimeLeft = dashDuration;
            lastDashTime = Time.time;
        }

        // ���λ�÷�ת
        FlipByMouse();

        // �����ٶȲ����ö�������
        float speed = rb.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        // ͬ���ٶȵ�������
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

        // �ڿͻ��˸��¶�������
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

        // ������Ҳ࣬���ﳯ�ң��������࣬���ﳯ��
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
