using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack_TestRole : NetworkBehaviour
{
    public GameObject bulletPrefab; // 子弹预制体
    public Transform firePoint; // 发射点位置
    public float fireRate = 0.5f; // 射击间隔

    private float nextFireTime = 0f; // 下次射击时间
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

            // 获取鼠标方向
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePosition - (Vector2)firePoint.position).normalized;

            if (NetworkClient.active)
            {
                CmdFire(direction);
            }
            else
            {
                // 离线模式直接生成子弹
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
        // 计算子弹旋转角度
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        GameObject bullet;
        // 如果是网络模式，生成子弹到所有客户端
        if (NetworkServer.active)
        {
            //NetworkServer.Spawn(bullet);
            bullet = ObjectPoolManager.Instance.Spawn("BulletOnline", firePoint.position, rotation);
        }
        else
        {
            // 实例化子弹
            //GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotation);
            bullet = ObjectPoolManager.Instance.Spawn("BulletOffline", firePoint.position, rotation);
        }

        // 设置子弹的初始速度
        bullet.GetComponent<Rigidbody2D>().velocity = direction * bullet.GetComponent<Bullet>().Speed;
    }
}
