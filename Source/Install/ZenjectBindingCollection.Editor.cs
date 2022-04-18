using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

namespace Zenject
{
    public partial class ZenjectBindingCollection
    {
        [Button("Collect In Scene", ButtonSizes.Medium)]
        void Editor_CollectInScene()
        {
            var output = new List<ZenjectBindingBase>();
            foreach (var rootObj in SceneManager.GetActiveScene().GetRootGameObjects())
                output.AddRange(rootObj.GetComponentsInChildren<ZenjectBindingBase>(true));
            Bindings = output.ToArray();
        }

        [Button("Collect Under GameObject", ButtonSizes.Medium)]
        void Editor_CollectUnderGameObject()
        {
            Bindings = gameObject.GetComponentsInChildren<ZenjectBindingBase>(true);
        }
    }
}