using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 工具类，提供一些通用的工具方法
/// </summary>
public static class Tools 
{
    public static void SafeDisable<T>(GameObject obj) where T : MonoBehaviour
    {
        T component = obj.GetComponent<T>();
        if (component != null)
        {
            component.enabled = false;
        }
    }
}
