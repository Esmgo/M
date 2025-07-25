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
    [Tooltip("�Ƿ�ʹ�����繦��")]
    public bool useNetwork = true;
    [SyncVar(hook = nameof(OnCurrentHpChanged))]
    private float syncCurrentHp;

    private bool isInitialized = false;

    private void Awake()
    {
        // �Զ��������ģʽ
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
                _currentHp = maxHp; // ͬʱ���ñ���ֵ
            }
            else
            {
                _currentHp = maxHp;
            }
            
            // ��������Ѫ����ʾ
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
    /// ����Ѫ������Ѫ״̬ - ���ڶ��������
    /// </summary>
    public void ResetToFull()
    {
        try
        {
            // ����Ѫ��ֵ
            _currentHp = maxHp;
            
            if (useNetwork && isServer)
            {
                syncCurrentHp = maxHp;
            }
            
            // ��������Ѫ����ʾ������������
            UpdateBarImmediate(1f);
            
            // ȷ����ʼ��״̬
            isInitialized = true;
            
            Debug.Log($"HPBar reset to full on {gameObject.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HPBar.ResetToFull failed on {gameObject.name}: {e.Message}");
        }
    }

    /// <summary>
    /// ��ȫ����Ѫ��״̬ - ���ڶ���ػ���
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
            
            // ֹͣ����DOTween����
            if (yellow != null) yellow.DOKill();
            if (red != null) red.DOKill();
            
            // ��������UI��ʾ
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
            // �ڷ������˴������������ȷ������ͬ��
            if (ObjectPoolManager.Instance != null)
            {
                // �ӳ�һִ֡�У�ȷ��HPͬ�����
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
            // ������ģʽ��ֱ�ӻ���
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

    // ����ӳٻ���Э�̣�ȷ������ͬ�����
    private IEnumerator DelayedReturn()
    {
        yield return null; // �ȴ�һ֡
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
    /// ��������Ѫ����ʾ����ʹ�ö���
    /// </summary>
    private void UpdateBarImmediate(float fillAmount)
    {
        try
        {
            // ֹͣ�������ڽ��еĶ���
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
    /// ��ȡ��ǰѪ���ٷֱ�
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
    /// ��ȡ��ǰѪ��
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
