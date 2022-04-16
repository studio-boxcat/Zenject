#if !NOT_UNITY3D

using System;
using UnityEngine;

namespace Zenject
{
    public interface IPrefabInstantiator
    {
        GameObject Instantiate(InjectContext context, out Action injectAction);

        UnityEngine.Object GetPrefab(InjectContext context);
    }
}

#endif
