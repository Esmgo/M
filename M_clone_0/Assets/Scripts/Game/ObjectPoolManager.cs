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

        // ����һ������������֯���гض���
        poolParent = new GameObject("PooledObjects").transform;
        poolParent.SetParent(transform);
    }

    private void OnDestroy()
    {
        ClearAllPools();
    }

    /// <summary>
    /// �����µĶ����
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
    /// ��ָ�����л�ȡ����
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
    /// �����󷵻س���
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
    /// ���ָ�������
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
    /// ������ж����
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
    /// ��ȡ�ص�״̬��Ϣ
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
