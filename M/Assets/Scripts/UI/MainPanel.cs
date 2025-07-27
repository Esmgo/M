using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainPanel : UIPanel
{
    GameObject loadedObject;
    private void Start()
    {
        RegisterButton("StartOffline", async () =>
        {
            loadedObject = await Tools.LoadAssetAsync<GameObject>("TestRole");
            if (loadedObject != null)
            {
                Instantiate(loadedObject, Vector3.zero, Quaternion.identity);
                
                // 启动离线游戏逻辑
                StartOfflineGame();
            }
            else
            {
                Debug.LogError("加载角色失败！");
            }
            UIManager.Instance.ClosePanel("MainPanel");
        });

        RegisterButton("StartOnline", async () =>
        {
            await UIManager.Instance.OpenPanelAsync<NetGamePanel>("NetGamePanel");
            UIManager.Instance.ClosePanel("MainPanel");
        });
    }
    
    private void StartOfflineGame()
    {
        // 启用敌人生成
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.summonController = true;
            EnemyManager.Instance.useNetwork = false; // 确保离线模式
            Debug.Log("离线游戏：已启用敌人生成");
        }
    }
}
