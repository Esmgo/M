using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;

public class UpgradePanel : UIPanel
{
    [Header("升级选项UI")]
    public Button moveSpeedButton;
    public Button attackSpeedButton;
    public Button damageButton;
    public Button healthButton;
    
    [Header("显示文本")]
    public Text moveSpeedText;
    public Text attackSpeedText;
    public Text damageText;
    public Text healthText;

    private PlayerExperience playerExp;

    public override void OnOpen()
    {
        base.OnOpen();
        
        // 查找本地玩家的经验组件
        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer != null)
        {
            playerExp = localPlayer.GetComponent<PlayerExperience>();
        }

        SetupButtons();
        UpdateTexts();
    }

    private void SetupButtons()
    {
        moveSpeedButton.onClick.RemoveAllListeners();
        attackSpeedButton.onClick.RemoveAllListeners();
        damageButton.onClick.RemoveAllListeners();
        healthButton.onClick.RemoveAllListeners();

        moveSpeedButton.onClick.AddListener(() => ChooseUpgrade(UpgradeType.MoveSpeed));
        attackSpeedButton.onClick.AddListener(() => ChooseUpgrade(UpgradeType.AttackSpeed));
        damageButton.onClick.AddListener(() => ChooseUpgrade(UpgradeType.Damage));
        healthButton.onClick.AddListener(() => ChooseUpgrade(UpgradeType.Health));
    }

    private void UpdateTexts()
    {
        if (playerExp != null)
        {
            moveSpeedText.text = $"移动速度 +1\n(当前: +{playerExp.moveSpeedBonus})";
            attackSpeedText.text = $"攻击速度 +10%\n(当前: +{playerExp.attackSpeedBonus * 100:F0}%)";
            damageText.text = $"攻击力 +5\n(当前: +{playerExp.damageBonus})";
            healthText.text = $"生命值 +20\n(当前: +{playerExp.healthBonus})";
        }
    }

    private void ChooseUpgrade(UpgradeType upgradeType)
    {
        if (playerExp != null)
        {
            // 发送升级选择到服务器
            playerExp.CmdChooseUpgrade(upgradeType);
            
            // 通知波次管理器玩家已准备好
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.CmdPlayerReady();
            }
            
            // 关闭升级面板
            ClosePanel();
        }
    }

    private void ClosePanel()
    {
        UIManager.Instance.ClosePanel("UpgradePanel");
    }
}