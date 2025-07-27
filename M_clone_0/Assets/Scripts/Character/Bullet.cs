using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : NetworkBehaviour, IPoolItem
{
    public float speed = 10f; // �ӵ��ٶ�
    public float lifetime = 5f; // �ӵ���������
    public int damage = 10; // �ӵ��˺�
    private float spawnTime; // ��¼�ӵ�����ʱ��

    public Rigidbody2D rb; // �ӵ��ĸ������

    public float Speed => speed;  //�ṩ��������ٶ�����

    [Header("Collision Settings")]
    public LayerMask enemyLayer; // ���õ������ڵĲ㼶
    public string enemyTag = "Enemy"; // ���˱�ǩ

    [SyncVar(hook = nameof(OnActiveStateChanged))]
    private bool isActive = false;

    private void Start()
    {
        if (rb == null) Debug.LogError("Rigidbody2D component is not assigned to the bullet.");

        if (isActive) { }
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
        if (Time.time >= spawnTime + lifetime && spawnTime != 0)
        {
            ReturnToPool(); // ��������������ڣ����ض����
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // �����ײ�����Ƿ��ǵ���
        if (IsEnemy(collision))
        {

            // �ڷ������ϻ���������ģʽ�´�����ײ
            if (isServer || !NetworkClient.active)
            {
                // �Ե�������˺�
                DealDamageToEnemy(collision.gameObject);

                // �����ӵ�
                ReturnToPool();
            }
        }
    }

    private bool IsEnemy(Collider2D collision)
    {
        // ͨ���㼶�ͱ�ǩ˫�ؼ��
        return ((1 << collision.gameObject.layer) & enemyLayer) != 0 ||
               collision.CompareTag(enemyTag);
    }


    public override void OnStartServer()
    {

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

        if (isServer)
        {
            isActive = false;
            RpcSetActive(false); // �����пͻ��������ü���״̬
        }
        spawnTime = 0; // ��������ʱ��
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
        if (!state)
        {
            transform.position = new Vector3(1000, 1000, 0); // ���ӵ��Ƴ���Ұ
        }
        gameObject.SetActive(state);
        
    }

    // �� [Server] �����޸�Ϊ����������ģʽ��ʹ��
    private void DealDamageToEnemy(GameObject enemy)
    {
        enemy.GetComponent<HPBar>().Hp(-damage); // ���õ��˵�����ֵ�������������ֵ
        //Debug.Log("�����ˣ�������������������");
        // ��ȡ���˵Ľ������
        //var health = enemy.GetComponent<NetworkHealth>();
        //if (health != null)
        //{
        //    health.TakeDamage(damage);
        //}
    }
}
