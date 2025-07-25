using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTest : NetworkBehaviour, IPoolItem
{
    [Header("敌人设置")]
    public float moveSpeed = 2f; // 敌人移动速度
    public float detectionRange = 5f; // 敌人检测范围
    public float updateTargetInterval = 0.5f; // 更新间隔时间
     
    [Header("Network Settings")]
    [Tooltip("是否使用网络功能")]
    public bool useNetwork = true;
    [SyncVar] private NetworkIdentity targetPlayerNet;

    // 添加敌人状态同步变量，类似子弹的 isActive
    [SyncVar(hook = nameof(OnEnemyActiveStateChanged))]
    private bool isEnemyActive = false;

    // 添加位置同步变量
    [SyncVar(hook = nameof(OnPositionChanged))]
    private Vector3 syncPosition = Vector3.zero;

    private Transform targetPlayer;
    private Rigidbody2D rb;
    private float lastUpdateTime;
    public HPBar hPBar;

    // 组件初始化标志
    private bool isComponentsInitialized = false;

    private void Awake()
    {
        // 在Awake中初始化组件引用，确保早于OnSpawn调用
        InitializeComponents();
    }

    void Start()
    {
        InitializeComponents();

        // 自动检测是否使用网络功能
        if (useNetwork && NetworkState.IsOffline)
        {
            useNetwork = false;
            Debug.Log("EnemyTest: NetworkManager not found, switching to offline mode");
        }
    }

    void FixedUpdate()
    {
        // 如果敌人未激活，不执行AI逻辑
        if (useNetwork && !isEnemyActive) return;

        // 服务器端或离线模式执行AI逻辑
        if (useNetwork && !isServer) return;

        // 限制目标更新频率
        if (Time.time - lastUpdateTime > updateTargetInterval)
        {
            FindClosestPlayer();
            lastUpdateTime = Time.time;
        }

        // 移动逻辑
        if (GetCurrentTarget() != null && rb != null)
        {
            Vector2 direction = (GetCurrentTarget().position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            // 服务器端同步位置
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

                // 网络模式下同时设置SyncVar
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

    // 网络同步钩子方法 - 类似子弹的 OnActiveStateChanged
    private void OnEnemyActiveStateChanged(bool oldValue, bool newValue)
    {
        if (oldValue != newValue)
        {
            gameObject.SetActive(newValue);
            Debug.Log($"Enemy {gameObject.name} active state changed: {oldValue} -> {newValue}");
        }
    }

    // 位置同步钩子方法
    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (!isServer && useNetwork)
        {
            transform.position = newValue;
        }
    }

    // 类似子弹的 RpcSetActive 方法
    [ClientRpc]
    private void RpcSetEnemyActive(bool state)
    {
        if (!state)
        {
            // 移动到屏幕外，类似子弹的处理
            transform.position = new Vector3(0, -1000, 0);
        }
        gameObject.SetActive(state);
        Debug.Log($"Enemy {gameObject.name} RPC set active: {state}");
    }

    public void OnSpawn()
    {
        // 确保组件已初始化
        InitializeComponents();

        // 重置血条到满血状态
        if (hPBar != null)
        {
            hPBar.ResetToFull();
        }
        else
        {
            Debug.LogError($"EnemyTest.OnSpawn: hPBar is null on {gameObject.name}");
        }

        // 重置AI状态
        targetPlayer = null;
        targetPlayerNet = null;
        lastUpdateTime = 0f;

        // 重置物理状态
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 网络同步激活状态 - 类似子弹的处理
        if (!NetworkClient.active)
        {
            // 离线模式：直接激活
            isEnemyActive = true;
        }
        else if (isServer)
        {
            // 在线模式的服务器端
            isEnemyActive = true;
            syncPosition = transform.position; // 同步位置
            RpcSetEnemyActive(true); // 在所有客户端上设置激活状态
        }

        Debug.Log($"Enemy {gameObject.name} spawned at {transform.position}");
    }

    public void OnReturn()
    {
        // 网络同步停用状态 - 类似子弹的处理
        if (!NetworkClient.active)
        {
            // 离线模式：直接处理
            isEnemyActive = false;
        }
        else if (isServer)
        {
            // 在线模式的服务器端
            isEnemyActive = false;
            RpcSetEnemyActive(false); // 在所有客户端上设置停用状态
        }

        // 重置血条状态
        if (hPBar != null)
        {
            hPBar.ResetState();
        }

        // 清理其他状态
        targetPlayer = null;
        targetPlayerNet = null;
        lastUpdateTime = 0f;

        // 停止移动
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Debug.Log($"Enemy {gameObject.name} returned to pool");
    }

    public void Reset()
    {
        // 重置所有状态
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

        // 验证必要组件
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

    // 可视化调试
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

    // 在对象被禁用时确保清理状态
    private void OnDisable()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
