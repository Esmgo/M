using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class UIPanel : MonoBehaviour
{
    // 打开时调用
    public virtual void OnOpen() { }
    // 关闭时调用
    public virtual void OnClose() { }

    /// <summary>
    /// 通用注册按钮事件（支持同步和异步Action）
    /// </summary>
    /// <param name="buttonName">按钮物体名</param>
    /// <param name="onClick">点击回调（支持async）</param>
    public void RegisterButton(string buttonName, Func<Task> onClick)
    {
        var btnTrans = transform.Find(buttonName);
        if (btnTrans == null)
        {
            Debug.LogError($"未找到按钮: {buttonName}");
            return;
        }
        var btn = btnTrans.GetComponent<UIButton>();
        if (btn == null)
        {
            Debug.LogError($"物体 {buttonName} 未挂载UIButton组件");
            return;
        }
        btn.onClick.AddListener(async () => await onClick());
    }

    /// <summary>
    /// 通用注册按钮事件（同步Action）
    /// </summary>
    /// <param name="buttonName">按钮物体名</param>
    /// <param name="onClick">点击回调</param>
    public void RegisterButton(string buttonName, Action onClick)
    {
        var btnTrans = transform.Find(buttonName);
        if (btnTrans == null)
        {
            Debug.LogError($"未找到按钮: {buttonName}");
            return;
        }
        var btn = btnTrans.GetComponent<UIButton>();
        if (btn == null)
        {
            Debug.LogError($"物体 {buttonName} 未挂载UIButton组件");
            return;
        }
        btn.onClick.AddListener(() => onClick());
    }
}
