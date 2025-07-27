using Mirror;
using System.Collections;
using UnityEngine;

public class ExpBall : NetworkBehaviour, IPoolItem
{
    [Header("����������")]
    public int expValue = 10; // ����ֵ
    public float collectRadius = 1f; // �ռ��뾶
    public float magnetSpeed = 5f; // �����ٶ�
    public float magnetRadius = 3f; // �����뾶
    public float lifetime = 30f; // ��������

    [Header("�Ӿ�Ч��")]
    public GameObject visualEffect; // �Ӿ�Ч��
    public AnimationCurve scaleCurve; // ��������

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

        // �����������
        if (Time.time >= spawnTime + lifetime && spawnTime != 0)
        {
            ReturnToPool();
            return;
        }

        // �����߼�
        if (!isBeingCollected)
        {
            FindNearestPlayer();
            if (targetPlayer != null)
            {
                float distance = Vector2.Distance(transform.position, targetPlayer.position);
                if (distance <= magnetRadius)
                {
                    // ��ʼ������
                    Vector2 direction = (targetPlayer.position - transform.position).normalized;
                    rb.velocity = direction * magnetSpeed;

                    // ����ռ�����
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

        // ������������
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

        // ֻ�з�����������ģʽ�������ռ�
        if (isServer || !NetworkClient.active)
        {
            // ����ҼӾ���
            var playerExp = targetPlayer.GetComponent<PlayerExperience>();
            if (playerExp != null)
            {
                playerExp.AddExperience(expValue);
            }

            // ���վ�����
            ReturnToPool();
        }
    }

    public void OnSpawn()
    {
        spawnTime = Time.time;
        isBeingCollected = false;
        targetPlayer = null;

        // ��������״̬
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // ������ײ��
        if (col != null) col.enabled = true;

        // ����ͬ������״̬
        if (!NetworkClient.active)
        {
            // ����ģʽ
            isActive = true;
        }
        else if (isServer)
        {
            // ����ģʽ��������
            isActive = true;
            RpcSetActive(true);
        }

        // ��������Ч��
        if (visualEffect != null)
        {
            StartCoroutine(PlaySpawnEffect());
        }
    }

    public void OnReturn()
    {
        // ������ײ��
        if (col != null) col.enabled = false;

        // ��������״̬
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // ����״̬
        isBeingCollected = false;
        targetPlayer = null;

        // ����ͬ��ͣ��״̬
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
        // ��ʼ���߼�
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
            transform.position = new Vector3(0, -1000, 0); // �Ƴ���Ұ
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
        // �����ռ��ʹ����뾶
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