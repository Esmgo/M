using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameApplication : MonoBehaviour
{
    public GameObject BulletTest;
    public GameObject EnemyTestt;
    public GameObject ExpBallPrefab; // 添加经验球预制体引用


    #region 单例实现
    private static GameApplication _instance;
    public static GameApplication Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameApplication>();
                if (_instance == null)
                {
                    Debug.LogError("缺少“GameApplication”！！！");
                }
            }
            return _instance;
        }
    }
    #endregion
    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private async void Start()
    {
        DOTween.Init();
        PoolInit();
        UIManager.Instance.Init();
        EnemyManager.Instance.summonController = false; 
        await UIManager.Instance.OpenPanelAsync<MainPanel>("MainPanel");    
    }


    private void PoolInit()
    {
        ObjectPoolManager.Instance.CreatePool(BulletTest, "BulletOnline", true, 0);
        ObjectPoolManager.Instance.CreatePool(BulletTest, "BulletOffline", false, 0);
        ObjectPoolManager.Instance.CreatePool(EnemyTestt, "EnemyOnline", true, 0);
        ObjectPoolManager.Instance.CreatePool(EnemyTestt, "EnemyOffline", false, 0);
        
        // 添加经验球对象池
        ObjectPoolManager.Instance.CreatePool(ExpBallPrefab, "ExpBallOnline", true, 0);
        ObjectPoolManager.Instance.CreatePool(ExpBallPrefab, "ExpBallOffline", false, 0);
        
        Debug.Log(ObjectPoolManager.Instance.GetPoolStatus());
    }
}
