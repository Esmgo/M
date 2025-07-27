using Mirror;
using System;
using UnityEngine;

public class PlayerExperience : NetworkBehaviour
{
    [Header("��������")]
    [SyncVar(hook = nameof(OnLevelChanged))] public int currentLevel = 1;
    [SyncVar(hook = nameof(OnExpChanged))] public int currentExp = 0;
    
    [Header("��������")]
    public int baseExpRequired = 100; // ������������
    public float expGrowthRate = 1.2f; // ����������

    // �������
    [SyncVar] public float moveSpeedBonus = 0f;
    [SyncVar] public float attackSpeedBonus = 0f;
    [SyncVar] public float damageBonus = 0f;
    [SyncVar] public float healthBonus = 0f;

    // �¼�
    public static event Action<int> OnPlayerLevelUp;
    public static event Action<int, int> OnPlayerExpUpdate; // currentExp, requiredExp

    private Move moveComponent;
    private Attack_TestRole attackComponent;

    private void Start()
    {
        moveComponent = GetComponent<Move>();
        attackComponent = GetComponent<Attack_TestRole>();
        
        // Ӧ�����еļӳ�
        ApplyBonuses();
    }

    public void AddExperience(int amount)
    {
        if (NetworkServer.active)
        {
            // ����ģʽ��ֻ�з���������
            ServerAddExperience(amount);
        }
        else if (!NetworkClient.active)
        {
            // ����ģʽ��ֱ�Ӵ���
            OfflineAddExperience(amount);
        }
    }

    [Server]
    private void ServerAddExperience(int amount)
    {
        currentExp += amount;
        CheckLevelUp();
        Debug.Log($"Server added {amount} exp, total: {currentExp}");
    }

    private void OfflineAddExperience(int amount)
    {
        currentExp += amount;
        OfflineCheckLevelUp();
        OnExpChanged(currentExp - amount, currentExp); // �ֶ�����UI����
        Debug.Log($"Offline added {amount} exp, total: {currentExp}");
    }

    [Server]
    private void CheckLevelUp()
    {
        int requiredExp = GetRequiredExpForLevel(currentLevel);
        
        while (currentExp >= requiredExp)
        {
            currentExp -= requiredExp;
            currentLevel++;
            
            // ֪ͨ����
            RpcOnLevelUp(currentLevel);
            
            requiredExp = GetRequiredExpForLevel(currentLevel);
        }
    }

    private void OfflineCheckLevelUp()
    {
        int requiredExp = GetRequiredExpForLevel(currentLevel);
        
        while (currentExp >= requiredExp)
        {
            currentExp -= requiredExp;
            currentLevel++;
            
            // ����ģʽֱ�Ӵ��������¼�
            OnPlayerLevelUp?.Invoke(currentLevel);
            OnLevelChanged(currentLevel - 1, currentLevel); // �ֶ�����UI����
            
            requiredExp = GetRequiredExpForLevel(currentLevel);
        }
    }

    public int GetRequiredExpForLevel(int level)
    {
        return Mathf.RoundToInt(baseExpRequired * Mathf.Pow(expGrowthRate, level - 1));
    }

    [ClientRpc]
    private void RpcOnLevelUp(int newLevel)
    {
        OnPlayerLevelUp?.Invoke(newLevel);
        
        // ����Ǳ�����ң���ʾ����ѡ�����
        if (isLocalPlayer)
        {
            WaveManager.Instance?.OnPlayerLevelUp();
        }
    }

    private void OnLevelChanged(int oldLevel, int newLevel)
    {
        Debug.Log($"Player level changed: {oldLevel} -> {newLevel}");
    }

    private void OnExpChanged(int oldExp, int newExp)
    {
        OnPlayerExpUpdate?.Invoke(newExp, GetRequiredExpForLevel(currentLevel));
    }

    [Command]
    public void CmdChooseUpgrade(UpgradeType upgradeType)
    {
        ApplyUpgrade(upgradeType);
    }

    public void ChooseUpgradeOffline(UpgradeType upgradeType)
    {
        // ����ģʽֱ��Ӧ������
        ApplyUpgrade(upgradeType);
    }

    private void ApplyUpgrade(UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case UpgradeType.MoveSpeed:
                moveSpeedBonus += 1f;
                break;
            case UpgradeType.AttackSpeed:
                attackSpeedBonus += 0.1f;
                break;
            case UpgradeType.Damage:
                damageBonus += 5f;
                break;
            case UpgradeType.Health:
                healthBonus += 20f;
                break;
        }
        
        ApplyBonuses();
        
        // ����ģʽ��ͬ�����ͻ���
        if (NetworkServer.active)
        {
            RpcApplyBonuses();
        }
    }

    private void ApplyBonuses()
    {
        if (moveComponent != null)
        {
            moveComponent.moveSpeed = 5f + moveSpeedBonus; // �����ٶ� + �ӳ�
        }
        
        // Ӧ�������ӳ�...
    }

    [ClientRpc]
    private void RpcApplyBonuses()
    {
        ApplyBonuses();
    }

    public bool HasPendingUpgrade()
    {
        // ����Ƿ��д����������
        // ������Ը���ʵ������ʵ���߼�
        return false; // ��ʱ����false
    }
}

public enum UpgradeType
{
    MoveSpeed,
    AttackSpeed, 
    Damage,
    Health
}