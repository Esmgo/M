using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public bool summonController = false;
    public bool useNetwork = true; // �Ƿ�ʹ�����繦��


    private float summonInterval = 2f; // �ٻ����ʱ��
    private float lastSummonTime = 0f; // �ϴ��ٻ�ʱ��

    #region ����ʵ��
    private static EnemyManager _instance;
    public static EnemyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EnemyManager>();
                if (_instance == null)
                {
                    Debug.LogError("ȱ�١�EnemyManager��������");
                }
            }
            return _instance;
        }
    }
#endregion
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Update()
    {
        // ֻ�з������˻�����ģʽ�������ɵ���
        if (useNetwork && !NetworkServer.active) return;
        
        if(summonController && Time.time >= lastSummonTime + summonInterval)
        {
            TrySpawnEnemy();
            lastSummonTime = Time.time;
        }
    }

    private void TrySpawnEnemy()
    {
        try
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError("ObjectPoolManager.Instance is null, cannot spawn enemy");
                return;
            }

            string poolKey;
            if (NetworkServer.active)
            {
                poolKey = "EnemyOnline";
            }
            else
            {
                poolKey = "EnemyOffline";
            }

            // �������λ��
            Vector3 spawnPosition = transform.position + new Vector3(
                Random.Range(-5f, 5f), 
                Random.Range(-5f, 5f), 
                0f
            );

            GameObject enemy = ObjectPoolManager.Instance.Spawn(poolKey, spawnPosition, Quaternion.identity);
            
            if (enemy != null)
            {
                Debug.Log($"Successfully spawned enemy: {enemy.name} at {spawnPosition} (Server: {NetworkServer.active})");
            }
            else
            {
                Debug.LogError($"Failed to spawn enemy with pool key: {poolKey}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in TrySpawnEnemy: {e.Message}\n{e.StackTrace}");
        }
    }
}
