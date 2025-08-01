#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    internal partial class ZenjectBindingCollection : ISelfValidator
    {
        private void Reset() => Collect();
        private void OnValidate() => Collect(verbose: false);

        // ReSharper disable once Unity.DuplicateShortcut
        [ContextMenu("Collect _c")]
        private void Collect() => Collect(verbose: true);

        private static readonly List<ZenjectBindingBase> _compBuf = new();

        private void Collect(bool verbose)
        {
            _compBuf.Clear();

            if (DryRunCollect(_compBuf) is false)
            {
                if (verbose)
                    L.W("[ZenjectBindingCollection] Cannot collect bindings: " + name);
                return;
            }

            if (_bindings.SequenceEqualRef(_compBuf) is false)
            {
                _bindings = _compBuf.ToArray();
                EditorUtility.SetDirty(this);
            }
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            // When playing, we don't want to validate the targets.
            if (Editing.No(this)) return;

            if (this.NoComponent<SceneContext>() && this.NoComponent<GameObjectContext>())
                result.AddError("ZenjectBindingCollection must be used in a SceneContext or GameObjectContext.");

            // Bindings must not contain self.
            if (_bindings.Contains(this))
                result.AddError("Bindings must not contain self.");

            _compBuf.Clear();
            if (DryRunCollect(_compBuf) is false) // Mostly due to unloaded scenes.
            {
                result.AddError("Cannot collect bindings: " + name);
                return;
            }

            if (_bindings.SequenceEqualRef(_compBuf) is false)
                result.AddError("Bindings must match the collected bindings.");
        }

        private bool DryRunCollect(List<ZenjectBindingBase> targets)
        {
            Assert.IsTrue(targets.IsEmpty(), "[ZenjectBindingCollection] The output list must be empty before calling DryRunCollect.");

            // If this is not a scene context, we can collect all bindings from this gameObject.
            if (gameObject.NoComponent<SceneContext>())
            {
                CollectTree(gameObject.transform, self: true, output: targets);
                return true;
            }

            // if this is a scene context, we need to collect all bindings from the scene.
            if (!gameObject.scene.isLoaded)
            {
                // if this is a prefab asset, we can only collect bindings from the root of the prefab.
                if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
                {
                    CollectTree(transform, self: true, output: targets);
                    return true;
                }

                return false;
            }

            foreach (var rootObj in gameObject.scene.GetRootGameObjects())
                CollectTree(rootObj.transform, self: rootObj.RefEq(gameObject), output: targets);

            return true;

            static void CollectTree(Transform transform, bool self, List<ZenjectBindingBase> output) =>
                ComponentSearch.CollectTree<ZenjectBindingBase, ZenjectBindingCollection>(transform, self, output);
        }
    }
}
#endif