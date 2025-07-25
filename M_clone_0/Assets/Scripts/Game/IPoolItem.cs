    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolItem
{
    public void OnSpawn();
    public void OnReturn();
}
