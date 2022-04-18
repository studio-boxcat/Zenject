#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ModestTree;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject.Internal;
using Object = UnityEngine.Object;

namespace Zenject
{
    public partial class InjectTargetCollection
    {
        [Button("Collect In Scene", ButtonSizes.Medium)]
        void Editor_CollectInScene()
        {
            var output = new List<MonoBehaviour>();
            foreach (var rootObj in SceneManager.GetActiveScene().GetRootGameObjects())
                GetInjectableMonoBehavioursUnderGameObject(rootObj, output);
            Targets = output.Cast<Object>().ToArray();
        }

        [Button("Collect Under GameObject", ButtonSizes.Medium)]
        void Editor_CollectUnderGameObject()
        {
            var output = new List<MonoBehaviour>();
            GetInjectableMonoBehavioursUnderGameObject(gameObject, output);
            Targets = output.Cast<Object>().ToArray();
        }

        static readonly Dictionary<Type, bool> _requiresInjection = new();

        static void GetInjectableMonoBehavioursUnderGameObject(
            GameObject gameObject, List<MonoBehaviour> injectableComponents)
        {
            foreach (var monoBehaviour in gameObject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var type = monoBehaviour.GetType();

                if (_requiresInjection.TryGetValue(type, out var requiresInjection))
                {
                    if (requiresInjection)
                        injectableComponents.Add(monoBehaviour);
                    return;
                }

                // Do not inject on installers since these are always injected before they are installed
                if (type.DerivesFrom<MonoInstaller>())
                {
                    _requiresInjection.Add(type, false);
                    continue;
                }

                var typeInfo = ReflectionTypeAnalyzer.GetReflectionInfo(monoBehaviour.GetType());
                if (typeInfo.IsInjectionRequired())
                {
                    _requiresInjection.Add(type, true);
                    injectableComponents.Add(monoBehaviour);
                }
                else
                {
                    _requiresInjection.Add(type, false);
                }
            }
        }
    }
}
#endif