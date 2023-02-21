#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Zenject
{
    public partial class InjectTargetCollection
    {
        void Reset()
        {
            Editor_Collect();
        }

        [Button("Collect", ButtonSizes.Medium)]
        void Editor_Collect()
        {
            Targets = gameObject.TryGetComponent<SceneContext>(out _)
                ? Internal_CollectInScene()
                : Internal_CollectUnderGameObject(gameObject);
        }

        bool Validate_Targets()
        {
            if (Application.isPlaying) return true;

            var compare = gameObject.TryGetComponent<SceneContext>(out _)
                ? Internal_CollectInScene()
                : Internal_CollectUnderGameObject(gameObject);
            return Targets.SequenceEqual(compare);
        }

        static Object[] Internal_CollectInScene()
        {
            var output = new List<Object>();
            foreach (var rootObj in SceneManager.GetActiveScene().GetRootGameObjects())
                GetInjectableMonoBehavioursUnderGameObject(rootObj.transform, output);
            return output.ToArray();
        }

        static Object[] Internal_CollectUnderGameObject(GameObject gameObject)
        {
            var output = new List<Object>();
            GetInjectableMonoBehavioursUnderGameObject(gameObject.transform, output);
            return output.ToArray();
        }

        static readonly List<MonoBehaviour> _monoBehaviourBuf = new();

        static void GetInjectableMonoBehavioursUnderGameObject(
            Transform transform, List<Object> injectableComponents)
        {
            // 대상에 붙어있는 컴포넌트 중 인젝션이 필요한 것을 찾음.
            transform.GetComponents(_monoBehaviourBuf);
            foreach (var monoBehaviour in _monoBehaviourBuf)
            {
                var type = monoBehaviour.GetType();
                if (RequiresInject(type))
                    injectableComponents.Add(monoBehaviour);
            }

            // 자식을 순회하면서 인젝션이 필요한 것을 찾음.
            var childCount = transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);

                // 자식에게 InjectTargetCollection 가 부착되어있는 경우, 인젝션 대상을 찾지 않음.
                if (child.TryGetComponent(out InjectTargetCollection injectTargetCollection))
                {
                    injectableComponents.Add(injectTargetCollection);
                    continue;
                }

                GetInjectableMonoBehavioursUnderGameObject(child, injectableComponents);
            }
        }

        static readonly Dictionary<Type, bool> _requiresInjectCache = new();

        static bool RequiresInject(Type type)
        {
            if (_requiresInjectCache.TryGetValue(type, out var requiresInjection))
                return requiresInjection;

            // Do not inject on installers since these are always injected before they are installed
            if (type.IsSubclassOf(typeof(MonoInstaller)))
            {
                _requiresInjectCache.Add(type, false);
                return false;
            }

            requiresInjection = Initializer.IsInjectionRequired(type);
            _requiresInjectCache.Add(type, requiresInjection);
            return requiresInjection;
        }
    }
}
#endif