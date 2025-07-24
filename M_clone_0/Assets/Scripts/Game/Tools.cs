using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

    public static async Task<T> LoadAssetAsync<T>(string address) where T : Object
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;
            return handle.Result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载资源 {address} 失败: {e.Message}");
            return null;
        }
    }
}
