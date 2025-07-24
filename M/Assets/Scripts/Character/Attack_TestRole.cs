using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack_TestRole : NetworkBehaviour
{
    public GameObject bulletPrefab; // �ӵ�Ԥ����
    public Transform firePoint; // �����λ��
    public float fireRate = 0.5f; // ������

    private float nextFireTime = 0f; // �´����ʱ��
    private Camera mainCamera;

    private void Start()
    {
        if (!isLocalPlayer && NetworkClient.active) return;
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (!isLocalPlayer && NetworkClient.active) return;

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            // ��ȡ��귽��
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePosition - (Vector2)firePoint.position).normalized;

            if (NetworkClient.active)
            {
                CmdFire(direction);
            }
            else
            {
                // ����ģʽֱ�������ӵ�
                FireBullet(direction);
            }
        }
    }

    [Command]
    private void CmdFire(Vector2 direction)
    {
        FireBullet(direction);
    }

    private void FireBullet(Vector2 direction)
    {
        // �����ӵ���ת�Ƕ�
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        GameObject bullet;
        // ���������ģʽ�������ӵ������пͻ���
        if (NetworkServer.active)
        {
            //NetworkServer.Spawn(bullet);
            bullet = ObjectPoolManager.Instance.Spawn("BulletOnline", firePoint.position, rotation);
        }
        else
        {
            // ʵ�����ӵ�
            //GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotation);
            bullet = ObjectPoolManager.Instance.Spawn("BulletOffline", firePoint.position, rotation);
        }

        // �����ӵ��ĳ�ʼ�ٶ�
        bullet.GetComponent<Rigidbody2D>().velocity = direction * bullet.GetComponent<Bullet>().Speed;
    }
}
