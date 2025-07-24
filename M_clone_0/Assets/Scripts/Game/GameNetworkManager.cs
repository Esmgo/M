using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameNetworkManager : NetworkManager
{
    public bool isOnline { get; private set; }
}
