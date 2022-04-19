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
            Bindings = gameObject.TryGetComponent<SceneContext>(out _)
                ? Internal_CollectInScene()
                : Internal_CollectUnderGameObject(gameObject);
        }

        bool Validate_Targets()
        {
            var compare = gameObject.TryGetComponent<SceneContext>(out _)
                ? Internal_CollectInScene()
                : Internal_CollectUnderGameObject(gameObject);
            return Bindings.SequenceEqual(compare);
        }

        static ZenjectBindingBase[] Internal_CollectInScene()
        {
            var output = new List<ZenjectBindingBase>();
            foreach (var rootObj in SceneManager.GetActiveScene().GetRootGameObjects())
                output.AddRange(rootObj.GetComponentsInChildren<ZenjectBindingBase>(true));
            return output.ToArray();
        }

        static ZenjectBindingBase[] Internal_CollectUnderGameObject(GameObject gameObject)
        {
            return gameObject.GetComponentsInChildren<ZenjectBindingBase>(true);
        }
    }
}
#endif