#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zenject
{
    internal partial class ZenjectBindingCollection
    {
        private void Reset() => Editor_Collect();
        private void OnValidate() => Editor_Collect(verbose: false);

        private static readonly List<ZenjectBindingBase> _compBuf = new();

        [ContextMenu("Collect _c")]
        private void Editor_Collect(bool verbose = true)
        {
            _compBuf.Clear();

            if (Internal_Collect(_compBuf) is false)
            {
                L.W("[ZenjectBindingCollection] Cannot collect bindings: " + name);
                return;
            }

            if (_bindings.SequenceEqualRef(_compBuf) is false)
            {
                _bindings = _compBuf.ToArray();
                EditorUtility.SetDirty(this);
            }
        }

        private bool Validate_Bindings(ref string errorMessage)
        {
            // When playing, we don't want to validate the targets.
            if (_bindings == null)
                return true;

            // Bindings must not contain self.
            if (_bindings.Contains(this))
            {
                errorMessage = "Bindings must not contain self.";
                return false;
            }

            // Mostly due to unloaded scenes.
            _compBuf.Clear();
            if (Internal_Collect(_compBuf) is false)
            {
                errorMessage = "Cannot collect bindings: " + name;
                return false;
            }

            if (_bindings.SequenceEqualRef(_compBuf) is false)
            {
                errorMessage = "Bindings must match the collected bindings.";
                return false;
            }

            return true;
        }

        private bool Internal_Collect(List<ZenjectBindingBase> targets)
        {
            Assert.IsTrue(targets.IsEmpty(), "[ZenjectBindingCollection] The output list must be empty before calling Internal_Collect.");

            // If this is not a scene context, we can collect all bindings from this gameObject.
            if (gameObject.NoComponent<SceneContext>())
            {
                Collect(gameObject.transform, self: true, output: targets);
                return true;
            }

            // if this is a scene context, we need to collect all bindings from the scene.
            if (!gameObject.scene.isLoaded)
            {
                // if this is a prefab asset, we can only collect bindings from the root of the prefab.
                if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
                {
                    Collect(transform, self: true, output: targets);
                    return true;
                }

                return false;
            }

            foreach (var rootObj in gameObject.scene.GetRootGameObjects())
                Collect(rootObj.transform, self: rootObj.RefEq(gameObject), output: targets);

            return true;

            static void Collect(Transform transform, bool self, List<ZenjectBindingBase> output) =>
                ComponentSearch.CollectTree<ZenjectBindingBase, ZenjectBindingCollection>(transform, self, output);
        }
    }
}
#endif