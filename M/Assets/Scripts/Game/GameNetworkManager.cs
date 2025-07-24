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
        networkAddress = ipAddress; // Ĭ�ϵ�ַΪ����
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
    /// ֹͣ����(������+�ͻ���)
    /// </summary>
    public void HostStop()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            StopHost();
        }
    }

    /// <summary>
    /// ֹͣ�ͻ���
    /// </summary>
    public void ClientStop()
    {
        if (NetworkClient.active)
        {
            StopClient();
        }
    }


    #region �˿����÷���
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
            Debug.LogWarning("��ǰTransport��֧�ֶ˿�����");
        }
    }
    #endregion
}
