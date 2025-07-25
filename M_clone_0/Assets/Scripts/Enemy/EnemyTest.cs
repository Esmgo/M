using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTest : NetworkBehaviour, IPoolItem
{
    [Header("��������")]
    public float moveSpeed = 2f; // �����ƶ��ٶ�
    public float detectionRange = 5f; // ���˼�ⷶΧ
    public float updateTargetInterval = 0.5f; // ���¼��ʱ��
     
    [Header("Network Settings")]
    [Tooltip("�Ƿ�ʹ�����繦��")]
    public bool useNetwork = true;
    [SyncVar] private NetworkIdentity targetPlayerNet;

    // ��ӵ���״̬ͬ�������������ӵ��� isActive
    [SyncVar(hook = nameof(OnEnemyActiveStateChanged))]
    private bool isEnemyActive = false;

    // ���λ��ͬ������
    [SyncVar(hook = nameof(OnPositionChanged))]
    private Vector3 syncPosition = Vector3.zero;

    private Transform targetPlayer;
    private Rigidbody2D rb;
    private float lastUpdateTime;
    public HPBar hPBar;

    // �����ʼ����־
    private bool isComponentsInitialized = false;

    private void Awake()
    {
        // ��Awake�г�ʼ��������ã�ȷ������OnSpawn����
        InitializeComponents();
    }

    void Start()
    {
        InitializeComponents();

        // �Զ�����Ƿ�ʹ�����繦��
        if (useNetwork && NetworkState.IsOffline)
        {
            useNetwork = false;
            Debug.Log("EnemyTest: NetworkManager not found, switching to offline mode");
        }
    }

    void FixedUpdate()
    {
        // �������δ�����ִ��AI�߼�
        if (useNetwork && !isEnemyActive) return;

        // �������˻�����ģʽִ��AI�߼�
        if (useNetwork && !isServer) return;

        // ����Ŀ�����Ƶ��
        if (Time.time - lastUpdateTime > updateTargetInterval)
        {
            FindClosestPlayer();
            lastUpdateTime = Time.time;
        }

        // �ƶ��߼�
        if (GetCurrentTarget() != null && rb != null)
        {
            Vector2 direction = (GetCurrentTarget().position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            // ��������ͬ��λ��
            if (useNetwork && isServer)
            {
                syncPosition = transform.position;
            }
        }
    }

    void FindClosestPlayer()
    {
        GameObject[] players;

        if (useNetwork)
        {
            var _plyers = FindObjectsOfType<Move>();
            players = new GameObject[_plyers.Length];
            for (int i = 0; i < _plyers.Length; i++)
            {
                players[i] = _plyers[i].gameObject;
            }
        }
        else
        {
            players = GameObject.FindGameObjectsWithTag("Player");
        }

        float closestDistance = Mathf.Infinity;
        Transform closestPlayer = null;

        foreach (var player in players)
        {
            if (player == null || !player.activeInHierarchy) continue;

            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < detectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player.transform;

                // ����ģʽ��ͬʱ����SyncVar
                if (useNetwork && isServer)
                {
                    var netIdentity = player.GetComponent<NetworkIdentity>();
                    if (netIdentity != null)
                    {
                        targetPlayerNet = netIdentity;
                    }
                }
            }
        }

        targetPlayer = closestPlayer;
    }

    private Transform GetCurrentTarget()
    {
        if (useNetwork)
        {
            return targetPlayerNet != null ? targetPlayerNet.transform : null;
        }
        return targetPlayer;
    }

    // ����ͬ�����ӷ��� - �����ӵ��� OnActiveStateChanged
    private void OnEnemyActiveStateChanged(bool oldValue, bool newValue)
    {
        if (oldValue != newValue)
        {
            gameObject.SetActive(newValue);
            Debug.Log($"Enemy {gameObject.name} active state changed: {oldValue} -> {newValue}");
        }
    }

    // λ��ͬ�����ӷ���
    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (!isServer && useNetwork)
        {
            transform.position = newValue;
        }
    }

    // �����ӵ��� RpcSetActive ����
    [ClientRpc]
    private void RpcSetEnemyActive(bool state)
    {
        if (!state)
        {
            // �ƶ�����Ļ�⣬�����ӵ��Ĵ���
            transform.position = new Vector3(0, -1000, 0);
        }
        gameObject.SetActive(state);
        Debug.Log($"Enemy {gameObject.name} RPC set active: {state}");
    }

    public void OnSpawn()
    {
        // ȷ������ѳ�ʼ��
        InitializeComponents();

        // ����Ѫ������Ѫ״̬
        if (hPBar != null)
        {
            hPBar.ResetToFull();
        }
        else
        {
            Debug.LogError($"EnemyTest.OnSpawn: hPBar is null on {gameObject.name}");
        }

        // ����AI״̬
        targetPlayer = null;
        targetPlayerNet = null;
        lastUpdateTime = 0f;

        // ��������״̬
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // ����ͬ������״̬ - �����ӵ��Ĵ���
        if (!NetworkClient.active)
        {
            // ����ģʽ��ֱ�Ӽ���
            isEnemyActive = true;
        }
        else if (isServer)
        {
            // ����ģʽ�ķ�������
            isEnemyActive = true;
            syncPosition = transform.position; // ͬ��λ��
            RpcSetEnemyActive(true); // �����пͻ��������ü���״̬
        }

        Debug.Log($"Enemy {gameObject.name} spawned at {transform.position}");
    }

    public void OnReturn()
    {
        // ����ͬ��ͣ��״̬ - �����ӵ��Ĵ���
        if (!NetworkClient.active)
        {
            // ����ģʽ��ֱ�Ӵ���
            isEnemyActive = false;
        }
        else if (isServer)
        {
            // ����ģʽ�ķ�������
            isEnemyActive = false;
            RpcSetEnemyActive(false); // �����пͻ���������ͣ��״̬
        }

        // ����Ѫ��״̬
        if (hPBar != null)
        {
            hPBar.ResetState();
        }

        // ��������״̬
        targetPlayer = null;
        targetPlayerNet = null;
        lastUpdateTime = 0f;

        // ֹͣ�ƶ�
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Debug.Log($"Enemy {gameObject.name} returned to pool");
    }

    public void Reset()
    {
        // ��������״̬
        if (hPBar != null)
        {
            hPBar.ResetState();
        }

        targetPlayer = null;
        targetPlayerNet = null;
        lastUpdateTime = 0f;
        isEnemyActive = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void Init()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (isComponentsInitialized) return;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (hPBar == null) hPBar = GetComponent<HPBar>();

        // ��֤��Ҫ���
        if (rb == null)
        {
            Debug.LogError($"EnemyTest: Missing Rigidbody2D component on {gameObject.name}");
        }
        
        if (hPBar == null)
        {
            Debug.LogError($"EnemyTest: Missing HPBar component on {gameObject.name}");
        }

        isComponentsInitialized = true;
    }

    // ���ӻ�����
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        var currentTarget = GetCurrentTarget();
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }

    // �ڶ��󱻽���ʱȷ������״̬
    private void OnDisable()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
