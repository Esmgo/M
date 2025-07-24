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
        networkAddress = ipAddress; // 默认地址为本地
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

    /// <summary>
    /// 停止主机(服务器+客户端)
    /// </summary>
    public void HostStop()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            StopHost();
        }
    }

    /// <summary>
    /// 停止客户端
    /// </summary>
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
