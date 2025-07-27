using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : NetworkBehaviour, IPoolItem
{
    public float speed = 10f; // 子弹速度
    public float lifetime = 5f; // 子弹生命周期
    public int damage = 10; // 子弹伤害
    private float spawnTime; // 记录子弹生成时间

    public Rigidbody2D rb; // 子弹的刚体组件

    public float Speed => speed;  //提供单项访问速度属性

    [Header("Collision Settings")]
    public LayerMask enemyLayer; // 设置敌人所在的层级
    public string enemyTag = "Enemy"; // 敌人标签

    [SyncVar(hook = nameof(OnActiveStateChanged))]
    private bool isActive = false;

    private void Start()
    {
        if (rb == null) Debug.LogError("Rigidbody2D component is not assigned to the bullet.");

        if (isActive) { }
        // 离线模式下也设置生命周期
        //if (!NetworkServer.active)
        //{
        //    Destroy(gameObject, lifetime);
        //    Tools.SafeDisable<NetworkIdentity>(gameObject);
        //    Tools.SafeDisable<NetworkTransformReliable>(gameObject);
        //    Tools.SafeDisable<NetworkRigidbodyReliable2D>(gameObject);
        //}
    }

    private void Update()
    {
        if (Time.time >= spawnTime + lifetime && spawnTime != 0)
        {
            ReturnToPool(); // 如果超过生命周期，返回对象池
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查碰撞对象是否是敌人
        if (IsEnemy(collision))
        {

            // 在服务器上或者在离线模式下处理碰撞
            if (isServer || !NetworkClient.active)
            {
                // 对敌人造成伤害
                DealDamageToEnemy(collision.gameObject);

                // 回收子弹
                ReturnToPool();
            }
        }
    }

    private bool IsEnemy(Collider2D collision)
    {
        // 通过层级和标签双重检查
        return ((1 << collision.gameObject.layer) & enemyLayer) != 0 ||
               collision.CompareTag(enemyTag);
    }


    public override void OnStartServer()
    {

    }

    [Server]
    private void DestroyBullet()
    {
        NetworkServer.Destroy(gameObject);
    }

    public void OnSpawn()
    {
        spawnTime = Time.time; // 记录子弹生成时间
        if (isServer)
        {
            isActive = true;
            RpcSetActive(true); // 在所有客户端上设置激活状态
        }
    }

    public void OnReturn()
    {

        if (isServer)
        {
            isActive = false;
            RpcSetActive(false); // 在所有客户端上设置激活状态
        }
        spawnTime = 0; // 重置生成时间
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.Return(gameObject);
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
            transform.position = new Vector3(1000, 1000, 0); // 将子弹移出视野
        }
        gameObject.SetActive(state);
        
    }

    // 从 [Server] 特性修改为可以在离线模式下使用
    private void DealDamageToEnemy(GameObject enemy)
    {
        enemy.GetComponent<HPBar>().Hp(-damage); // 调用敌人的生命值组件，减少生命值
        //Debug.Log("打中了！！！！！！！！！！");
        // 获取敌人的健康组件
        //var health = enemy.GetComponent<NetworkHealth>();
        //if (health != null)
        //{
        //    health.TakeDamage(damage);
        //}
    }
}
