using Mirror;
using System.Collections;
using UnityEngine;

public class ExpBall : NetworkBehaviour, IPoolItem
{
    [Header("经验球设置")]
    public int expValue = 10; // 经验值
    public float collectRadius = 1f; // 收集半径
    public float magnetSpeed = 5f; // 磁吸速度
    public float magnetRadius = 3f; // 磁吸半径
    public float lifetime = 30f; // 生命周期

    [Header("视觉效果")]
    public GameObject visualEffect; // 视觉效果
    public AnimationCurve scaleCurve; // 缩放曲线

    private float spawnTime;
    private Transform targetPlayer;
    private bool isBeingCollected = false;
    private Rigidbody2D rb;
    private Collider2D col;

    [SyncVar(hook = nameof(OnActiveStateChanged))]
    private bool isActive = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!isActive) return;

        // 检查生命周期
        if (Time.time >= spawnTime + lifetime && spawnTime != 0)
        {
            ReturnToPool();
            return;
        }

        // 磁吸逻辑
        if (!isBeingCollected)
        {
            FindNearestPlayer();
            if (targetPlayer != null)
            {
                float distance = Vector2.Distance(transform.position, targetPlayer.position);
                if (distance <= magnetRadius)
                {
                    // 开始被吸引
                    Vector2 direction = (targetPlayer.position - transform.position).normalized;
                    rb.velocity = direction * magnetSpeed;

                    // 检查收集距离
                    if (distance <= collectRadius)
                    {
                        CollectExp();
                    }
                }
            }
        }
    }

    private void FindNearestPlayer()
    {
        float closestDistance = magnetRadius;
        Transform closestPlayer = null;

        // 查找最近的玩家
        Move[] players = FindObjectsOfType<Move>();
        foreach (var player in players)
        {
            if (player.gameObject.activeInHierarchy)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player.transform;
                }
            }
        }

        targetPlayer = closestPlayer;
    }

    private void CollectExp()
    {
        if (isBeingCollected) return;
        isBeingCollected = true;

        // 只有服务器或离线模式处理经验收集
        if (isServer || !NetworkClient.active)
        {
            // 给玩家加经验
            var playerExp = targetPlayer.GetComponent<PlayerExperience>();
            if (playerExp != null)
            {
                playerExp.AddExperience(expValue);
            }

            // 回收经验球
            ReturnToPool();
        }
    }

    public void OnSpawn()
    {
        spawnTime = Time.time;
        isBeingCollected = false;
        targetPlayer = null;

        // 重置物理状态
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 启用碰撞器
        if (col != null) col.enabled = true;

        // 网络同步激活状态
        if (!NetworkClient.active)
        {
            // 离线模式
            isActive = true;
        }
        else if (isServer)
        {
            // 在线模式服务器端
            isActive = true;
            RpcSetActive(true);
        }

        // 播放生成效果
        if (visualEffect != null)
        {
            StartCoroutine(PlaySpawnEffect());
        }
    }

    public void OnReturn()
    {
        // 禁用碰撞器
        if (col != null) col.enabled = false;

        // 重置物理状态
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 重置状态
        isBeingCollected = false;
        targetPlayer = null;

        // 网络同步停用状态
        if (!NetworkClient.active)
        {
            isActive = false;
        }
        else if (isServer)
        {
            isActive = false;
            RpcSetActive(false);
        }

        Reset();
    }

    public void Reset()
    {
        spawnTime = 0;
        isBeingCollected = false;
        targetPlayer = null;
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void Init()
    {
        // 初始化逻辑
    }

    private void OnActiveStateChanged(bool oldValue, bool newValue)
    {
        gameObject.SetActive(newValue);
    }

    [ClientRpc]
    private void RpcSetActive(bool state)
    {
        if (!state)
        {
            transform.position = new Vector3(0, -1000, 0); // 移出视野
        }
        gameObject.SetActive(state);
    }

    private IEnumerator PlaySpawnEffect()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (scaleCurve != null && scaleCurve.length > 0)
            {
                float scale = scaleCurve.Evaluate(t);
                transform.localScale = originalScale * scale;
            }

            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制收集和磁吸半径
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.Return(gameObject);
    }
}