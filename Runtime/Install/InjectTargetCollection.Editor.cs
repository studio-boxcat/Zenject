#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    public partial class InjectTargetCollection : ISelfValidator
    {
        private void Reset() => Collect(dirty: false);
        private void OnValidate() => Collect(dirty: false);

        private static readonly List<MonoBehaviour> _collectBuf = new();

        private void Collect(bool dirty)
        {
            _collectBuf.Clear();

            // fails mostly due to unloaded scenes.
            if (DryRunCollect(_collectBuf) is false)
                return;
            // skip if not changed.
            Targets ??= Array.Empty<MonoBehaviour>();
            if (Targets.SequenceEqualRef(_collectBuf))
                return;

            Targets = _collectBuf.ToArray();
            if (dirty) EditorUtility.SetDirty(this);
        }

        // ReSharper disable once Unity.DuplicateShortcut
        [ContextMenu("Collect _c")]
        private void Collect() => Collect(dirty: true);

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            // When playing, we don't want to validate the targets.
            if (Editing.No(this)) return;

            if (this.NoComponent<SceneContext>() && this.NoComponent<GameObjectContext>())
                result.AddError("InjectTargetCollection must be used in a SceneContext or GameObjectContext.");

            _collectBuf.Clear();
            if (DryRunCollect(_collectBuf) is false) // Mostly due to unloaded scenes.
            {
                result.AddError("Cannot collect targets: " + name);
                return;
            }

            if (Targets.SequenceEqualRef(_collectBuf) is false)
                result.AddError("Targets must match the collected targets.");
        }

        private bool DryRunCollect(List<MonoBehaviour> targets)
        {
            Assert.IsTrue(_collectBuf.IsEmpty(), "Collect buffer must be empty before validating targets.");

            // First collect all injectables on this gameObject.
            CollectInjectableComponents(gameObject, targets);
            targets.Remove(this); // Remove this object from the list of targets so that we don't inject it twice.
            CollectInjectablesInChildren(gameObject.transform, targets);

            if (gameObject.HasComponent<SceneContext>())
            {
                // if this is a prefab asset, we only need to collect the injectables on this gameObject.
                if (gameObject.scene.isLoaded is false)
                    return PrefabUtility.IsPartOfPrefabAsset(gameObject);

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