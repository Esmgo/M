using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameApplication : MonoBehaviour
{
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

    private void Start()
    {
        DOTween.Init();
        UIManager.Instance.Init();
    }
}
