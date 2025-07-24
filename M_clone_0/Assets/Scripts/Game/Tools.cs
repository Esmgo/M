using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// �����࣬�ṩһЩͨ�õĹ��߷���
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
