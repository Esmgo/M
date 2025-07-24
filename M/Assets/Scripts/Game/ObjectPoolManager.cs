using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : NetworkBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private readonly Dictionary<string, ObjectPool> pools = new();
    private Transform poolParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 创建一个父对象来组织所有池对象
        poolParent = new GameObject("PooledObjects").transform;
        poolParent.SetParent(transform);
    }

    private void OnDestroy()
    {
        ClearAllPools();
    }

    /// <summary>
    /// 创建新的对象池
    /// </summary>
    public void CreatePool(GameObject prefab, string customKey = null, bool networkSynced = false,  int initialSize = 0)
    {
        string poolKey = customKey ?? prefab.name;

        if (pools.ContainsKey(poolKey))
        {
            Debug.LogWarning($"Pool with key '{poolKey}' already exists.");
            return;
        }

        var newPool = new ObjectPool(prefab, customKey,  poolParent, networkSynced, initialSize);
        pools.Add(poolKey, newPool);
    }

    /// <summary>
    /// 从指定池中获取对象
    /// </summary>
    public GameObject Spawn(string poolKey, Vector3 position, Quaternion rotation)
    {
        if (!pools.TryGetValue(poolKey, out var pool))
        {
            Debug.LogError($"Pool with key '{poolKey}' not found.");
            return null;
        }

        return pool.Spawn(position, rotation);
    }

    /// <summary>
    /// 将对象返回池中
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        var poolObj = obj.GetComponent<PoolObject>();
        if (poolObj == null)
        {
            Debug.LogError("Object doesn't belong to any pool - destroying it.");
            Destroy(obj);
            return;
        }

        if (!pools.TryGetValue(poolObj.poolKey, out var pool))
        {
            Debug.LogError($"Pool with key '{poolObj.poolKey}' not found - destroying object.");
            Destroy(obj);
            return;
        }

        pool.Return(obj);
    }

    /// <summary>
    /// 清除指定对象池
    /// </summary>
    public void ClearPool(string poolKey)
    {
        if (pools.TryGetValue(poolKey, out var pool))
        {
            pool.Clear();
            pools.Remove(poolKey);
        }
    }

    /// <summary>
    /// 清除所有对象池
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in pools.Values)
        {
            pool.Clear();
        }
        pools.Clear();
    }

    /// <summary>
    /// 获取池的状态信息
    /// </summary>
    public string GetPoolStatus()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Object Pool Status ===");
        foreach (var kvp in pools)
        {
            sb.AppendLine($"{kvp.Key}: Available={kvp.Value.Count}, TotalCreated={kvp.Value.TotalCreated}");
        }
        return sb.ToString();
    }
}
