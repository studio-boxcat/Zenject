#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zenject
{
    internal partial class ZenjectBindingCollection
    {
        private void Reset()
        {
            Editor_Collect();
        }

        [ContextMenu("Collect _c")]
        public void Editor_Collect()
        {
            _bindings = Internal_Collect().ToArray();
            EditorUtility.SetDirty(this);
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

            if (_bindings.SequenceEqual(Internal_Collect()) == false)
            {
                errorMessage = "Bindings must match the collected bindings.";
                return false;
            }

            return true;
        }

        private List<ZenjectBindingBase> Internal_Collect()
        {
            var targets = new List<ZenjectBindingBase>();

            if (gameObject.TryGetComponent<SceneContext>(out _))
            {
                foreach (var rootObj in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    GetBindingsUnderGameObject(rootObj.transform, rootObj == gameObject, targets);
                }
            }
            else
            {
                GetBindingsUnderGameObject(gameObject.transform, true, targets);
            }

            targets.Remove(this);

            return targets;
        }

        private static readonly List<ZenjectBindingBase> _bindingBuf = new();

        private static void GetBindingsUnderGameObject(
            Transform transform, bool self, List<ZenjectBindingBase> output)
        {
            // If the transform has a ZenjectBindingCollection component then let it handle its own children.
            if (self == false && transform.TryGetComponent<ZenjectBindingCollection>(out var bindings))
            {
                output.Add(bindings);
                return;
            }

            // Find all bindings on this game object.
            transform.GetComponents(_bindingBuf);
            foreach (var binding in _bindingBuf)
            {
                if (binding is not ZenjectBindingCollection)
                    output.Add(binding);
            }

            // Visit all children.
            var childCount = transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                GetBindingsUnderGameObject(child, false, output);
            }
        }
    }
}
#endif