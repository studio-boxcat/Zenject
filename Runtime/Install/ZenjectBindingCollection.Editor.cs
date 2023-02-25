#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
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
        void Editor_Collect()
        {
            Bindings = Internal_Collect().ToArray();
        }

        bool Validate_Bindings()
        {
            // When playing, we don't want to validate the targets.
            if (Bindings == null) return true;

            return Bindings.SequenceEqual(Internal_Collect());
        }

        List<ZenjectBindingBase> Internal_Collect()
        {
            var targets = new List<ZenjectBindingBase>();

            if (gameObject.TryGetComponent<SceneContext>(out _))
            {
                foreach (var rootObj in SceneManager.GetActiveScene().GetRootGameObjects())
                    targets.AddRange(rootObj.GetComponentsInChildren<ZenjectBindingBase>(true));
            }
            else
            {
                GetBindingsUnderGameObject(gameObject.transform, targets);
            }

            return targets;
        }

        static readonly List<ZenjectBindingBase> _bindingBuf = new();

        static void GetBindingsUnderGameObject(
            Transform transform, List<ZenjectBindingBase> output)
        {
            // Find all bindings on this game object.
            transform.GetComponents(_bindingBuf);
            output.AddRange(_bindingBuf);

            // Visit all children.
            var childCount = transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);

                // If the child has a GameObjectContext component then let it handle its own children.
                if (child.TryGetComponent(out GameObjectContext _))
                    continue;

                // If the child has an ZenjectBindingCollection component then let it handle its own children.
                if (child.TryGetComponent(out ZenjectBindingCollection _))
                    continue;

                GetBindingsUnderGameObject(child, output);
            }
        }
    }
}
#endif