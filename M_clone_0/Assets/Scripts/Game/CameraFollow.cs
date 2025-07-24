using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随参数")]
    public Transform target; // 跟随目标（角色）
    public float followSpeed = 5f;
    public float maxOffsetDistance = 3f; // 角色到屏幕中心最大距离（世界单位）

    private Camera cam;

    void Awake() 
    {
        cam = Camera.main;

        
        
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 获取鼠标在世界坐标的位置
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 计算角色与鼠标的中点
        Vector3 midPoint = (target.position + mouseWorldPos) / 2f;

        // 限制摄像头偏移，保证角色到摄像头中心距离不超过最大距离
        Vector3 camToTarget = target.position - midPoint;
        if (camToTarget.magnitude > maxOffsetDistance)
        {
            midPoint = target.position - camToTarget.normalized * maxOffsetDistance;
        }

        // 保持摄像头z轴不变
        midPoint.z = transform.position.z;

        // 使用插值平滑移动摄像头，避免DOTween每帧创建Tween导致卡顿
        transform.position = Vector3.Lerp(transform.position, midPoint, followSpeed * Time.deltaTime);
    }

    public void SetTarget()
    {
        // 只跟随本地玩家
        var localPlayer = FindObjectsOfType<Move>();
        if (NetworkState.IsOnline)
        {
            foreach (var player in localPlayer)
            {
                if (player.isLocalPlayer) // 确保是本地玩家
                {
                    target = player.transform;
                    break;
                }
            }
        }
        else
        {
            target = localPlayer[0].transform; // 离线模式下跟随第一个玩家
        }
    }
}
