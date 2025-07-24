using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
            Debug.LogError($"������Դ {address} ʧ��: {e.Message}");
            return null;
        }
    }
}
