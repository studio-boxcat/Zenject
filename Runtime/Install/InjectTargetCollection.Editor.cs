#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Zenject
{
    public partial class InjectTargetCollection
    {
        private void Reset()
        {
            Editor_Collect();
        }

        [Button("Collect", ButtonSizes.Medium)]
        public void Editor_Collect()
        {
            Targets = Internal_Collect().ToArray();
        }

        [MenuItem("CONTEXT/InjectTargetCollection/Collect _c")]
        private static void Editor_Collect(MenuCommand cmd)
        {
            var target = (InjectTargetCollection) cmd.context;
            Undo.RecordObject(target, "");
            target.Editor_Collect();
            EditorUtility.SetDirty(target);
        }

        private bool Validate_Targets()
        {
            // When playing, we don't want to validate the targets.
            if (Targets == null) return true;

            return Targets.SequenceEqual(Internal_Collect());
        }

        private List<Object> Internal_Collect()
        {
            var targets = new List<Object>();

            // First collect all injectables on this gameObject.
            CollectInjectableComponents(gameObject, targets);
            targets.Remove(this); // Remove this object from the list of targets so that we don't inject it twice.
            CollectInjectablesInChildren(gameObject.transform, targets);

            if (gameObject.TryGetComponent<SceneContext>(out _))
            {
                foreach (var rootObj in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (ReferenceEquals(rootObj, gameObject)) continue;
                    CollectInjectablesFromNonRootGameObject(rootObj, targets);
                }
            }
            else if (gameObject.TryGetComponent<GameObjectContext>(out var gameObjectContext))
            {
                targets.Remove(gameObjectContext);
            }

            return targets;
        }

        private static Object TryGetInjectionIntermediary(GameObject target)
        {
            // If the gameObject has a GameObjectContext component then let it handle its own children.
            if (target.TryGetComponent(out GameObjectContext context))
                return context;

            // If the gameObject has an InjectTargetCollection component then let it handle its own children.
            if (target.TryGetComponent(out InjectTargetCollection injectTargets))
                return injectTargets;

            return null;
        }

        private static readonly List<MonoBehaviour> _monoBehaviourBuf = new();

        /// <summary>
        /// Collects all injectable components on the given gameObject.
        /// </summary>
        private static void CollectInjectableComponents(GameObject gameObject, List<Object> output)
        {
            gameObject.GetComponents(_monoBehaviourBuf);
            foreach (var monoBehaviour in _monoBehaviourBuf)
            {
#if UNITY_EDITOR
                if (monoBehaviour == null) // This can happen if the component is no longer inherited from MonoBehaviour.
                    continue;
#endif

                if (RequiresInject(monoBehaviour.GetType()))
                    output.Add(monoBehaviour);
            }
        }

        private static void CollectInjectablesInChildren(Transform transform, List<Object> output)
        {
            var childCount = transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                CollectInjectablesFromNonRootGameObject(child.gameObject, output);
            }
        }

        /// <summary>
        /// Collects all injectable components on the given gameObject and its children.
        /// Since the given gameObject is not a root gameObject, it will add only the InjectionIntermediary if it exists.
        /// </summary>
        private static void CollectInjectablesFromNonRootGameObject(GameObject gameObject, List<Object> output)
        {
            var injectionIntermediary = TryGetInjectionIntermediary(gameObject);
            if (injectionIntermediary is not null)
            {
                output.Add(injectionIntermediary);
                return;
            }

            CollectInjectableComponents(gameObject, output);
            CollectInjectablesInChildren(gameObject.transform, output);
        }

        private static readonly Dictionary<Type, bool> _requiresInjectCache = new();

        private static bool RequiresInject(Type type)
        {
            if (_requiresInjectCache.TryGetValue(type, out var requiresInjection))
                return requiresInjection;

            // Do not inject on installers since these are always injected just before they are installed.
            if (type.IsSubclassOf(typeof(MonoBehaviourInstaller)))
            {
                _requiresInjectCache.Add(type, false);
                return false;
            }

            requiresInjection = Injector.IsInjectionRequired(type);
            _requiresInjectCache.Add(type, requiresInjection);
            return requiresInjection;
        }
    }
}
#endif