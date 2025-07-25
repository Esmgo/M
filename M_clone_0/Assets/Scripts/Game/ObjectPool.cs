using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectPool
{
    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private readonly GameObject prefab;
    private readonly bool networkSynced;
    private readonly Transform parentTransform;
    private readonly string poolKey;

    public int Count => pool.Count;
    public int TotalCreated { get; private set; } = 0;



    public ObjectPool(GameObject prefab, string poolKey,  Transform parentTransform = null, bool networkSynced = false, int initialSize = 0)
    {
        this.prefab = prefab;
        this.parentTransform = parentTransform;
        this.networkSynced = networkSynced;
        this.poolKey = poolKey;

        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    private GameObject CreateNewObject()
    {
        GameObject obj = Object.Instantiate(prefab, parentTransform);
        obj.SetActive(false);
        TotalCreated++;

        if (networkSynced && NetworkServer.active)
        {
            NetworkServer.Spawn(obj);
        }
        else if (!networkSynced)
        {
            Tools.SafeDisable<NetworkIdentity>(obj);
            Tools.SafeDisable<NetworkTransformReliable>(obj);
            Tools.SafeDisable<NetworkAnimator>(obj);
            Tools.SafeDisable<NetworkRigidbodyReliable2D>(obj);
        }
        var poolObject = obj.GetComponent<PoolObject>() ?? obj.AddComponent<PoolObject>();
        //poolObject.poolKey = prefab.name;
        poolObject.poolKey = poolKey;
        obj.name = poolKey;
        poolObject.isInPool = true;

        return obj;
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject obj;
        if (pool.Count == 0)
        {
            obj = CreateNewObject();
        }
        else
        {
            obj = pool.Dequeue();
        }

        var poolObj = obj.GetComponent<PoolObject>();
        poolObj.isInPool = false;

        // 先设置位置和旋转
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        
        // 重置物理状态
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.WakeUp();
        }
        
        // 重新启用碰撞器
        var colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = true;
        }
        
        // 调用IPoolable接口的OnSpawn（在激活前）
        IPoolItem[] poolables = obj.GetComponentsInChildren<IPoolItem>();
        foreach (var poolable in poolables)
        {
            poolable.OnSpawn();
        }
        
        // 最后激活对象
        obj.SetActive(true);

        // 网络对象需要额外处理
        if (networkSynced && NetworkServer.active)
        {
            var netIdentity = obj.GetComponent<NetworkIdentity>();
            if (netIdentity != null && !netIdentity.isServer)
            {
                // 确保网络对象正确同步
                NetworkServer.Spawn(obj);
            }
        }

        return obj;
    }

    public void Return(GameObject obj)
    {
        var poolObj = obj.GetComponent<PoolObject>();
        if (poolObj == null || poolObj.poolKey != obj.name)
        {
            Debug.LogError($"Attempting to return object to wrong pool. Expected pool: {prefab.name}, Object pool: {poolObj?.poolKey}");
            return;
        }
        
        // 先调用OnReturn进行清理
        IPoolItem[] poolables = obj.GetComponentsInChildren<IPoolItem>();
        foreach (var poolable in poolables)
        {
            poolable.OnReturn();
        }
        
        // 禁用所有碰撞器
        var colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        
        // 重置物理状态
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.Sleep();
        }
        
        // 移动到屏幕外
        obj.transform.position = new Vector3(0, -1000, 0);
        
        // 停用对象
        obj.SetActive(false);
        
        poolObj.isInPool = true;
        pool.Enqueue(obj);
        
        Debug.Log($"Object {obj.name} returned to pool at position {obj.transform.position}");
    }

    public void Clear()
    {
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                if (networkSynced && NetworkServer.active)
                {
                    NetworkServer.Destroy(obj);
                }
                else
                {
                    Object.Destroy(obj);
                }
            }
        }
        TotalCreated = 0;
    }
}
