#if !NOT_UNITY3D

using System;
using UnityEngine;

namespace Zenject
{
    public interface IPrefabInstantiator
    {
        GameObject Instantiate(InjectableInfo context, out Action injectAction);

        UnityEngine.Object GetPrefab(InjectableInfo context);
    }
}

#endif
