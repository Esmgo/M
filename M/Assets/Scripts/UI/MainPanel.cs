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
            }
            else
            {
                Debug.LogError("¼ÓÔØ½ÇÉ«Ê§°Ü£¡");
            }
            UIManager.Instance.ClosePanel("MainPanel");
        });

        RegisterButton("StartOnline", async () =>
        {
            await UIManager.Instance.OpenPanelAsync<NetGamePanel>("NetGamePanel");
            UIManager.Instance.ClosePanel("MainPanel");
        });
    }
}
