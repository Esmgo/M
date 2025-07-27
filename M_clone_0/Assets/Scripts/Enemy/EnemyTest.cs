using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTest : NetworkBehaviour, IPoolItem
{
    [Header("敌人设置")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f;
    public float updateTargetInterval = 0.5f;

    [Header("Network Settings")]
    [Tooltip("是否使用网络功能")]
    public bool useNetwork = true;
    [SyncVar] private NetworkIdentity targetPlayerNet;

    // 参考Bullet添加同步状态
    [SyncVar(hook = nameof(OnEnemyActiveStateChanged))]
    private bool isEnemyActive = false;

    private Transform targetPlayer;
    private Rigidbody2D rb;
    private float lastUpdateTime;
    public HPBar hPBar;
    private bool isComponentsInitialized = false;

    private void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        InitializeComponents();

        if (useNetwork && NetworkState.IsOffline)
        {
            useNetwork = false;
            Debug.Log("EnemyTest: NetworkManager not found, switching to offline mode");
        }
    }

    void FixedUpdate()
    {
        // 只有激活状态才执行AI
        if (useNetwork && !isEnemyActive) return;
        if (useNetwork && !isServer) return;

        if (Time.time - lastUpdateTime > updateTargetInterval)
        {
            FindClosestPlayer();
            lastUpdateTime = Time.time;
        }

        if (GetCurrentTarget() != null && rb != null)
        {
            Vector2 direction = (GetCurrentTarget().position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;
        }
    }

    // 参考Bullet的状态同步钩子
    private void OnEnemyActiveStateChanged(bool oldValue, bool newValue)
    {
        if (oldValue != newValue)
        {
            gameObject.SetActive(newValue);
            Debug.Log($"Enemy {gameObject.name} active state changed: {oldValue} -> {newValue}");
        }
    }

    // 参考Bullet的RPC方法
    [ClientRpc]
    private void RpcSetEnemyActive(bool state)
    {
        if (!state)
        {
            // 移动到屏幕外，避免闪烁
            transform.position = new Vector3(0, -1000, 0);
        }
        gameObject.SetActive(state);
    }

    public void OnSpawn()
    {
        InitializeComponents();

        if (hPBar != null)
        {
            hPBar.ResetToFull();
        }

        // 重置状态
        targetPlayer = null;
        targetPlayerNet = null;
        lastUpdateTime = 0f;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 参考Bullet的激活逻辑
        if (!NetworkClient.active)
        {
            // 离线模式
            isEnemyActive = true;
        }
        else if (isServer)
        {
            // 服务器端，同步激活状态
            isEnemyActive = true;
            RpcSetEnemyActive(true);
        }

        Debug.Log($"Enemy {gameObject.name} spawned at {transform.position}");
    }

    public void OnReturn()
    {
        // 参考Bullet的停用逻辑
        if (!NetworkClient.active)
        {
            isEnemyActive = false;
        }
        else if (isServer)
        {
            isEnemyActive = false;
            RpcSetEnemyActive(false);
        }

        if (hPBar != null)
        {
            hPBar.ResetState();
        }

        // 清理状态
        targetPlayer = null;
        targetPlayerNet = null;
        lastUpdateTime = 0f;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Debug.Log($"Enemy {gameObject.name} returned to pool");
    }

    public void Reset()
    {
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

    // 其他方法保持不变...
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

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}