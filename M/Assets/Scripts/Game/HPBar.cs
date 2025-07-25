using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : NetworkBehaviour
{
    [Header("Health Settings")]
    public float maxHp = 100f;
    [SerializeField] private float _currentHp = 100f;

    [Header("UI References")]
    public Image yellow;
    public Image red;
    public TextMeshProUGUI hpText;

    [Header("Network Settings")]
    [Tooltip("是否使用网络功能")]
    public bool useNetwork = true;
    [SyncVar(hook = nameof(OnCurrentHpChanged))]
    private float syncCurrentHp;

    private bool isInitialized = false;

    private void Awake()
    {
        // 自动检测网络模式
        if (useNetwork && NetworkState.IsOffline)
        {
            useNetwork = false;
            Debug.Log("HPBar: NetworkManager not found, switching to offline mode");
        }
    }

    public void Initialize()
    {
        try
        {
            if (useNetwork)
            {
                if (isServer)
                {
                    syncCurrentHp = maxHp;
                }
                _currentHp = maxHp; // 同时设置本地值
            }
            else
            {
                _currentHp = maxHp;
            }
            
            // 立即更新血条显示
            UpdateBar(1f); // maxHp/maxHp = 1
            isInitialized = true;
            
            Debug.Log($"HPBar initialized on {gameObject.name}, HP: {_currentHp}/{maxHp}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HPBar.Initialize failed on {gameObject.name}: {e.Message}");
        }
    }

    /// <summary>
    /// 重置血条到满血状态 - 用于对象池重用
    /// </summary>
    public void ResetToFull()
    {
        try
        {
            // 重置血量值
            _currentHp = maxHp;
            
            if (useNetwork && isServer)
            {
                syncCurrentHp = maxHp;
            }
            
            // 立即更新血条显示（跳过动画）
            UpdateBarImmediate(1f);
            
            // 确保初始化状态
            isInitialized = true;
            
            Debug.Log($"HPBar reset to full on {gameObject.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HPBar.ResetToFull failed on {gameObject.name}: {e.Message}");
        }
    }

    /// <summary>
    /// 完全重置血条状态 - 用于对象池回收
    /// </summary>
    public void ResetState()
    {
        try
        {
            _currentHp = maxHp;
            if (useNetwork && isServer)
            {
                syncCurrentHp = maxHp;
            }
            
            // 停止所有DOTween动画
            if (yellow != null) yellow.DOKill();
            if (red != null) red.DOKill();
            
            // 立即重置UI显示
            if (yellow != null) yellow.fillAmount = 1f;
            if (red != null) red.fillAmount = 1f;
            if (hpText != null) hpText.text = "100%";
            
            isInitialized = false;
            
            Debug.Log($"HPBar state reset on {gameObject.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HPBar.ResetState failed on {gameObject.name}: {e.Message}");
        }
    }

    public void Hp(float value)
    {
        if (!isInitialized) 
        {
            Debug.LogWarning($"HPBar.Hp called before initialization on {gameObject.name}");
            return;
        }

        try
        {
            if (useNetwork)
            {
                if (isServer)
                {
                    ServerChangeHp(value);
                }
            }
            else
            {
                OfflineChangeHp(value);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HPBar.Hp failed on {gameObject.name}: {e.Message}");
        }
    }

    [Server]
    private void ServerChangeHp(float value)
    {
        float oldHp = syncCurrentHp;
        syncCurrentHp = Mathf.Clamp(syncCurrentHp + value, 0, maxHp);
        
        Debug.Log($"Server HP change: {oldHp} -> {syncCurrentHp} (change: {value})");

        if (syncCurrentHp <= 0)
        {
            // 在服务器端处理敌人死亡，确保网络同步
            if (ObjectPoolManager.Instance != null)
            {
                // 延迟一帧执行，确保HP同步完成
                StartCoroutine(DelayedReturn());
            }
            else
            {
                Debug.LogError("ObjectPoolManager.Instance is null when trying to return enemy");
            }
        }
    }

    private void OfflineChangeHp(float value)
    {
        float oldHp = _currentHp;
        _currentHp = Mathf.Clamp(_currentHp + value, 0, maxHp);
        
        Debug.Log($"Offline HP change: {oldHp} -> {_currentHp} (change: {value})");
        
        UpdateBar(_currentHp / maxHp);

        if (_currentHp <= 0)
        {
            // 在离线模式下直接回收
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Return(gameObject);
            }
            else
            {
                Debug.LogError("ObjectPoolManager.Instance is null when trying to return enemy");
            }
        }
    }

    // 添加延迟回收协程，确保网络同步完成
    private IEnumerator DelayedReturn()
    {
        yield return null; // 等待一帧
        ObjectPoolManager.Instance.Return(gameObject);
    }

    private void OnCurrentHpChanged(float oldValue, float newValue)
    {
        float fillAmount = newValue / maxHp;
        UpdateBar(fillAmount);
        Debug.Log($"SyncVar HP changed: {oldValue} -> {newValue}, fillAmount: {fillAmount}");
    }

    private void UpdateBar(float fillAmount)
    {
        try
        {
            if (yellow != null && red != null)
            {
                red.fillAmount = fillAmount;
                yellow.DOFillAmount(fillAmount, 0.2f).SetEase(Ease.Linear).SetDelay(0.2f);
            }
            else
            {
                Debug.LogWarning($"HPBar UI components missing on {gameObject.name}");
            }

            if (hpText != null)
            {
                hpText.text = $"{fillAmount * 100:F0}%";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HPBar.UpdateBar failed on {gameObject.name}: {e.Message}");
        }
    }

    /// <summary>
    /// 立即更新血条显示，不使用动画
    /// </summary>
    private void UpdateBarImmediate(float fillAmount)
    {
        try
        {
            // 停止所有正在进行的动画
            if (yellow != null) 
            {
                yellow.DOKill();
                yellow.fillAmount = fillAmount;
            }
            if (red != null) 
            {
                red.DOKill();
                red.fillAmount = fillAmount;
            }

            if (hpText != null)
            {
                hpText.text = $"{fillAmount * 100:F0}%";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HPBar.UpdateBarImmediate failed on {gameObject.name}: {e.Message}");
        }
    }

    /// <summary>
    /// 获取当前血量百分比
    /// </summary>
    public float GetHealthPercentage()
    {
        if (useNetwork)
        {
            return syncCurrentHp / maxHp;
        }
        else
        {
            return _currentHp / maxHp;
        }
    }

    /// <summary>
    /// 获取当前血量
    /// </summary>
    public float GetCurrentHealth()
    {
        if (useNetwork)
        {
            return syncCurrentHp;
        }
        else
        {
            return _currentHp;
        }
    }
}
