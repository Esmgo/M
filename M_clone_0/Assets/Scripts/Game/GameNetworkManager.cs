using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameNetworkManager : NetworkManager
{
    public static GameNetworkManager Instance { get; private set; }

    public override void Awake()
    {
        base.Awake();
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void HostStart(string ipAddress = "localhost", ushort port = 7777)
    {
        if (NetworkClient.active || NetworkServer.active)
        {
            Debug.LogWarning("Cannot start host - already active as client or server");
            return;
        }
        networkAddress = ipAddress;
        SetPortIfNeeded(port);
        StartHost();
    }

    public void ClientStart(string ipAddress = "localhost", ushort port = 7777)
    {
        if (!NetworkClient.active)
        {
            networkAddress = ipAddress;
            SetPort(port);
            StartClient();
        }
    }

    // 当主机启动时
    public override void OnStartHost()
    {
        base.OnStartHost();
        StartNetworkGame();
    }

    // 当服务器启动时
    public override void OnStartServer()
    {
        base.OnStartServer();
        StartNetworkGame();
    }

    // 添加游戏开始的回调
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        StartNetworkGame();
    }
    
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        
        // 如果是主机模式，确保游戏逻辑启动
        if (NetworkServer.active && NetworkClient.active)
        {
            StartNetworkGame();
        }
    }
    
    private void StartNetworkGame()
    {
        // 启用敌人生成（只在服务器端）
        if (NetworkServer.active && EnemyManager.Instance != null)
        {
            EnemyManager.Instance.summonController = true;
            EnemyManager.Instance.useNetwork = true; // 确保网络模式
            Debug.Log("网络游戏：已启用敌人生成");
        }
    }

    public void HostStop()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            StopHost();
        }
    }

    public void ClientStop()
    {
        if (NetworkClient.active)
        {
            StopClient();
        }
    }

    #region 端口设置方法
    private void SetPortIfNeeded(ushort? port)
    {
        if (port.HasValue && Transport.active is PortTransport portTransport)
        {
            portTransport.Port = port.Value;
        }
    }

    private void SetPort(ushort port)
    {
        if (Transport.active is PortTransport portTransport)
        {
            portTransport.Port = port;
        }
        else
        {
            Debug.LogWarning("当前Transport不支持端口设置");
        }
    }
    #endregion
}
