using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;

public class UpgradePanel : UIPanel
{
    [Header("����ѡ��UI")]
    public Button moveSpeedButton;
    public Button attackSpeedButton;
    public Button damageButton;
    public Button healthButton;
    
    [Header("��ʾ�ı�")]
    public Text moveSpeedText;
    public Text attackSpeedText;
    public Text damageText;
    public Text healthText;

    private PlayerExperience playerExp;

    public override void OnOpen()
    {
        base.OnOpen();
        
        // ���ұ�����ҵľ������
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
            moveSpeedText.text = $"�ƶ��ٶ� +1\n(��ǰ: +{playerExp.moveSpeedBonus})";
            attackSpeedText.text = $"�����ٶ� +10%\n(��ǰ: +{playerExp.attackSpeedBonus * 100:F0}%)";
            damageText.text = $"������ +5\n(��ǰ: +{playerExp.damageBonus})";
            healthText.text = $"����ֵ +20\n(��ǰ: +{playerExp.healthBonus})";
        }
    }

    private void ChooseUpgrade(UpgradeType upgradeType)
    {
        if (playerExp != null)
        {
            // ��������ѡ�񵽷�����
            playerExp.CmdChooseUpgrade(upgradeType);
            
            // ֪ͨ���ι����������׼����
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.CmdPlayerReady();
            }
            
            // �ر��������
            ClosePanel();
        }
    }

    private void ClosePanel()
    {
        UIManager.Instance.ClosePanel("UpgradePanel");
    }
}