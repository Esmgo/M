using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkState.IsOnline)
        {
            SetOnline();
        }
        else
        {
            SetOffline();
        }
    }

    private void SetOnline()
    {
        Debug.Log("Setting up online mode...");
    }

    private void SetOffline()
    {
        Debug.Log("Setting up offline mode...");
        Tools.SafeDisable<NetworkIdentity>(gameObject); // ��������������
        Tools.SafeDisable<NetworkTransformReliable>(gameObject); // ��������任���
        Tools.SafeDisable<NetworkRigidbodyReliable2D>(gameObject); // ��������任���
        Tools.SafeDisable<NetworkAnimator>(gameObject); // �������綯�����
    }
}
