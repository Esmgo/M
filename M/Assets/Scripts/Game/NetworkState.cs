using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 用来检测网络状态的类
/// </summary>
public static class NetworkState 
{
    public static bool IsOnline => NetworkClient.active || NetworkServer.active;
    public static bool IsHost => NetworkServer.active && NetworkClient.active;
    public static bool IsClient => NetworkClient.active && !IsHost;
    public static bool IsServer => NetworkServer.active && !NetworkClient.active;
    public static bool IsOffline => !IsOnline;

    public static string CurrentMode
    {
        get
        {
            if (IsHost) return "Host";
            if (IsServer) return "Server";
            if (IsClient) return "Client";
            return "Offline";
        }
    }
}
