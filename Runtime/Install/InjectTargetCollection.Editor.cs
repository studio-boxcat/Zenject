#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public partial class InjectTargetCollection
    {
        private void Reset() => Editor_Collect();
        private void OnValidate() => Editor_Collect();

        private static readonly List<MonoBehaviour> _collectBuf = new();

        [ContextMenu("Collect _c")]
        public void Editor_Collect()
        {
            _collectBuf.Clear();

            if (Internal_Collect(_collectBuf) is false)
                return; // mostly due to unloaded scenes.

            if (Targets.SequenceEqualRef(_collectBuf) is false)
            {
                Targets = _collectBuf.ToArray();
                EditorUtility.SetDirty(this);
            }
        }

        private bool Validate_Targets(ref string errorMessage)
        {
            // When playing, we don't want to validate the targets.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Targets == null) return true;

            _collectBuf.Clear();
            if (Internal_Collect(_collectBuf) is false)
            {
                errorMessage = "Cannot collect targets: " + name;
                return false;
            }

            return Targets.SequenceEqualRef(_collectBuf);
        }

        private bool Internal_Collect(List<MonoBehaviour> targets)
        {
            Assert.IsTrue(_collectBuf.IsEmpty(), "Collect buffer must be empty before validating targets.");

            // First collect all injectables on this gameObject.
            CollectInjectableComponents(gameObject, targets);
            targets.Remove(this); // Remove this object from the list of targets so that we don't inject it twice.
            CollectInjectablesInChildren(gameObject.transform, targets);

            if (gameObject.HasComponent<SceneContext>())
            {
                // rare-case. return in the middle of processing.
                if (gameObject.scene.isLoaded is false)
                    return false;

                foreach (var rootObj in gameObject.scene.GetRootGameObjects())
                {
                    if (rootObj.RefEq(gameObject)) continue; // we already collected this gameObject
                    CollectInjectablesFromNonRootGameObject(rootObj, targets);
                }
            }
            else if (gameObject.TryGetComponent<GameObjectContext>(out var ctx))
            {
                targets.Remove(ctx);
            }

            return true;
        }

        private static MonoBehaviour? TryGetInjectionIntermediary(GameObject target)
        {
            // If the gameObject has a GameObjectContext component then let it handle its own children.
            if (target.TryGetComponent(out GameObjectContext context))
                return context;

            // If the gameObject has an InjectTargetCollection component then let it handle its own children.
            if (target.TryGetComponent(out InjectTargetCollection injectTargets))
                return injectTargets;

            return null;
        }

        private static readonly List<MonoBehaviour> _injectableCompBuf = new();

        private static void CollectInjectableComponents(GameObject gameObject, List<MonoBehaviour> output)
        {
            gameObject.GetComponents(_injectableCompBuf); // no need to clear.
            foreach (var mb in _injectableCompBuf)
            {
                if (!mb) continue; // This can happen if the component is no longer inherited from MonoBehaviour.
                if (RequiresInject(mb.GetType()))
                    output.Add(mb);
            }
        }

        private static void CollectInjectablesInChildren(Transform transform, List<MonoBehaviour> output)
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
        private static void CollectInjectablesFromNonRootGameObject(GameObject gameObject, List<MonoBehaviour> output)
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
            // See InstallerCollection.
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