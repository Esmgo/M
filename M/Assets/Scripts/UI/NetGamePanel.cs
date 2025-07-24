using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NetGamePanel : UIPanel
{
    private TMP_InputField ipForJoin, portForJoin;

    private void Start()
    {
        ipForJoin = transform.Find("IPForJoin").GetComponent<TMP_InputField>();
        portForJoin = transform.Find("PortForJoin").GetComponent<TMP_InputField>();

        ipForJoin.text = "localhost";
        portForJoin.text = "7777";

        RegisterButton("Host", () =>
        {
            GameNetworkManager.Instance.HostStart();
            UIManager.Instance.ClosePanel("NetGamePanel");
        });

        RegisterButton("Join", () =>
        {
            GameNetworkManager.Instance.ClientStart(ipForJoin.text, ushort.Parse(portForJoin.text));
            UIManager.Instance.ClosePanel("NetGamePanel");
        });
    }
}
