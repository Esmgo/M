using Mirror;
using System;
using UnityEngine;

public class PlayerExperience : NetworkBehaviour
{
    [Header("经验设置")]
    [SyncVar(hook = nameof(OnLevelChanged))] public int currentLevel = 1;
    [SyncVar(hook = nameof(OnExpChanged))] public int currentExp = 0;
    
    [Header("升级设置")]
    public int baseExpRequired = 100; // 基础经验需求
    public float expGrowthRate = 1.2f; // 经验增长率

    // 玩家属性
    [SyncVar] public float moveSpeedBonus = 0f;
    [SyncVar] public float attackSpeedBonus = 0f;
    [SyncVar] public float damageBonus = 0f;
    [SyncVar] public float healthBonus = 0f;

    // 事件
    public static event Action<int> OnPlayerLevelUp;
    public static event Action<int, int> OnPlayerExpUpdate; // currentExp, requiredExp

    private Move moveComponent;
    private Attack_TestRole attackComponent;

    private void Start()
    {
        moveComponent = GetComponent<Move>();
        attackComponent = GetComponent<Attack_TestRole>();
        
        // 应用已有的加成
        ApplyBonuses();
    }

    public void AddExperience(int amount)
    {
        if (NetworkServer.active)
        {
            // 网络模式：只有服务器处理
            ServerAddExperience(amount);
        }
        else if (!NetworkClient.active)
        {
            // 离线模式：直接处理
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
        OnExpChanged(currentExp - amount, currentExp); // 手动触发UI更新
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
            
            // 通知升级
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
            
            // 离线模式直接触发升级事件
            OnPlayerLevelUp?.Invoke(currentLevel);
            OnLevelChanged(currentLevel - 1, currentLevel); // 手动触发UI更新
            
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
        
        // 如果是本地玩家，显示升级选择界面
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
        // 离线模式直接应用升级
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
        
        // 网络模式下同步到客户端
        if (NetworkServer.active)
        {
            RpcApplyBonuses();
        }
    }

    private void ApplyBonuses()
    {
        if (moveComponent != null)
        {
            moveComponent.moveSpeed = 5f + moveSpeedBonus; // 基础速度 + 加成
        }
        
        // 应用其他加成...
    }

    [ClientRpc]
    private void RpcApplyBonuses()
    {
        ApplyBonuses();
    }

    public bool HasPendingUpgrade()
    {
        // 检查是否有待处理的升级
        // 这里可以根据实际需求实现逻辑
        return false; // 临时返回false
    }
}

public enum UpgradeType
{
    MoveSpeed,
    AttackSpeed, 
    Damage,
    Health
}