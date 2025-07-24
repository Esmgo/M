using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // Addressables面板key注册表
    private Dictionary<string, string> panelKeys = new Dictionary<string, string>();
    // 活动面板
    private Dictionary<string, UIPanel> activePanels = new Dictionary<string, UIPanel>();
    // UI父节点
    public Transform uiRoot;

    public void Init()
    {
        RegisterPanel("MainPanel", "MainPanel");
        RegisterPanel("NetGamePanel", "NetGamePanel");  
        //RegisterPanel("SelectRolePanel", "SelectRolePanel");
        //RegisterPanel("PausePanel", "PausePanel");
        //RegisterPanel("NetworkPanel", "NetworkPanel");
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (uiRoot == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            uiRoot = canvas != null ? canvas.transform : this.transform;
        }
    }

    /// <summary>
    /// 注册UI面板的Addressable key
    /// </summary>
    public void RegisterPanel(string panelName, string addressableKey)
    {
        if (!panelKeys.ContainsKey(panelName))
            panelKeys.Add(panelName, addressableKey);
    }

    /// <summary>
    /// 异步打开UI面板
    /// </summary>
    public async Task<T> OpenPanelAsync<T>(string panelName) where T : UIPanel
    {
        if (activePanels.ContainsKey(panelName))
        {
            activePanels[panelName].gameObject.SetActive(true);
            return activePanels[panelName] as T;
        }

        if (!panelKeys.ContainsKey(panelName))
        {
            Debug.LogError($"UIManager: 未注册面板 {panelName}");
            return null;
        }

        string addressableKey = panelKeys[panelName];

        // Addressables异步加载
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"UIManager: Addressables 加载失败 {addressableKey}");
            return null;
        }

        GameObject go = Instantiate(handle.Result, uiRoot);
        T panel = go.GetComponent<T>();
        if (panel == null)
            panel = go.AddComponent<T>();
        activePanels.Add(panelName, panel);
        panel.OnOpen();
        return panel;
    }

    // 关闭UI面板
    public void ClosePanel(string panelName)
    {
        if (activePanels.TryGetValue(panelName, out UIPanel panel))
        {
            panel.OnClose();
            panel.gameObject.SetActive(false);
        }
    }

    // 销毁UI面板
    public void DestroyPanel(string panelName)
    {
        if (activePanels.TryGetValue(panelName, out UIPanel panel))
        {
            Destroy(panel.gameObject);
            activePanels.Remove(panelName);
        }
    }
}
