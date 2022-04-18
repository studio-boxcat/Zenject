#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Zenject
{
    public partial class ZenjectBindingCollection
    {
        [Button("Collect In Scene", ButtonSizes.Medium)]
        void Editor_CollectInScene()
        {
            Bindings = Internal_CollectInScene();
        }

        [Button("Collect Under GameObject", ButtonSizes.Medium)]
        void Editor_CollectUnderGameObject()
        {
            Bindings = Internal_CollectUnderGameObject(gameObject);
        }

        void Validate_Targets()
        {
            if (name == "SceneContext")
            {
                Assert.IsTrue(Bindings.SequenceEqual(Internal_CollectInScene()));
            }
            else
            {
                Assert.IsTrue(Bindings.SequenceEqual(Internal_CollectUnderGameObject(gameObject)));
            }
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