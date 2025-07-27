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
                
                // ����������Ϸ�߼�
                StartOfflineGame();
            }
            else
            {
                Debug.LogError("���ؽ�ɫʧ�ܣ�");
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
        // ���õ�������
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.summonController = true;
            EnemyManager.Instance.useNetwork = false; // ȷ������ģʽ
            Debug.Log("������Ϸ�������õ�������");
        }
    }
}
