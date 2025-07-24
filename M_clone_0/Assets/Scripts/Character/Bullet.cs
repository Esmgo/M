using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : NetworkBehaviour,IPoolItem
{
    public float speed = 10f; // 子弹速度
    public float lifetime = 5f; // 子弹生命周期
    public int damage = 10; // 子弹伤害
    private float spawnTime; // 记录子弹生成时间

    public Rigidbody2D rb; // 子弹的刚体组件

    public float Speed => speed;  //提供单项访问速度属性

    [SyncVar(hook = nameof(OnActiveStateChanged))]
    private bool isActive = false;

    private void Start()
    {
        if(rb == null) Debug.LogError("Rigidbody2D component is not assigned to the bullet.");

        
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
        if(Time.time >= spawnTime + lifetime && spawnTime != 0)
        {
            ReturnToPool(); // 如果超过生命周期，返回对象池
        }
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if (collision.TryGetComponent<NetworkHealth>(out var health))
        //{
        //    health.TakeDamage(damage);
        //}
        //DestroyBullet();
    }

    public override void OnStartServer()
    {
        //Invoke(nameof(DestroyBullet), lifetime); // 设置生命周期结束时销毁子弹
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
        spawnTime = 0; // 重置生成时间
        if (isServer)
        {
            isActive = false;
            RpcSetActive(false); // 在所有客户端上设置激活状态
        }
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
        gameObject.SetActive(state);
    }
}
