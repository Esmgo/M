using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : NetworkBehaviour,IPoolItem
{
    public float speed = 10f; // �ӵ��ٶ�
    public float lifetime = 5f; // �ӵ���������
    public int damage = 10; // �ӵ��˺�
    private float spawnTime; // ��¼�ӵ�����ʱ��

    public Rigidbody2D rb; // �ӵ��ĸ������

    public float Speed => speed;  //�ṩ��������ٶ�����

    [SyncVar(hook = nameof(OnActiveStateChanged))]
    private bool isActive = false;

    private void Start()
    {
        if(rb == null) Debug.LogError("Rigidbody2D component is not assigned to the bullet.");

        
        // ����ģʽ��Ҳ������������
        //if (!NetworkServer.active)
        //{
        //    Destroy(gameObject, lifetime);
        //    Tools.SafeDisable<NetworkIdentity>(gameObject);
        //    Tools.SafeDisable<NetworkTransformReliable>(gameObject);
        //    Tools.SafeDisable<NetworkRigidbodyReliable2D>(gameObject);
        //}
    }

    private void Update()
    {
        if(Time.time >= spawnTime + lifetime && spawnTime != 0)
        {
            ReturnToPool(); // ��������������ڣ����ض����
        }
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if (collision.TryGetComponent<NetworkHealth>(out var health))
        //{
        //    health.TakeDamage(damage);
        //}
        //DestroyBullet();
    }

    public override void OnStartServer()
    {
        //Invoke(nameof(DestroyBullet), lifetime); // �����������ڽ���ʱ�����ӵ�
    }

    [Server]
    private void DestroyBullet()
    {
        NetworkServer.Destroy(gameObject);
    }

    public void OnSpawn()
    {
        spawnTime = Time.time; // ��¼�ӵ�����ʱ��
        if (isServer)
        {
            isActive = true;
            RpcSetActive(true); // �����пͻ��������ü���״̬
        }
    }

    public void OnReturn()
    {
        spawnTime = 0; // ��������ʱ��
        if (isServer)
        {
            isActive = false;
            RpcSetActive(false); // �����пͻ��������ü���״̬
        }
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.Return(gameObject);
    }


    private void OnActiveStateChanged(bool oldValue, bool newValue)
    {
        gameObject.SetActive(newValue);
    }

    [ClientRpc]
    private void RpcSetActive(bool state)
    {
        gameObject.SetActive(state);
    }
}
