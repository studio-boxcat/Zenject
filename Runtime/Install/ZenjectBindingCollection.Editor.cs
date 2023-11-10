#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zenject
{
    public partial class ZenjectBindingCollection
    {
        void Reset()
        {
            Editor_Collect();
        }

        [Button("Collect", ButtonSizes.Medium)]
        public void Editor_Collect()
        {
            Bindings = Internal_Collect().ToArray();
        }

        [MenuItem("CONTEXT/ZenjectBindingCollection/Collect")]
        static void Editor_Collect(MenuCommand cmd)
        {
            var target = (ZenjectBindingCollection) cmd.context;
            Undo.RecordObject(target, "");
            target.Editor_Collect();
            EditorUtility.SetDirty(target);
        }

        [UsedImplicitly]
        bool Validate_Bindings(ref string errorMessage)
        {
            // When playing, we don't want to validate the targets.
            if (Bindings == null)
                return true;

            // Bindings must not contain self.
            if (Bindings.Contains(this))
            {
                errorMessage = "Bindings must not contain self.";
                return false;
            }

            if (Bindings.SequenceEqual(Internal_Collect()) == false)
            {
                errorMessage = "Bindings must match the collected bindings.";
                return false;
            }

            return true;
        }

        List<ZenjectBindingBase> Internal_Collect()
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

        static readonly List<ZenjectBindingBase> _bindingBuf = new();

        static void GetBindingsUnderGameObject(
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