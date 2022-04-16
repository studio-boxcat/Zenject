#if !NOT_UNITY3D
using UnityEngine.SceneManagement;
using UnityEngine;
#endif

namespace Zenject.Internal
{
    public static class ZenUtilInternal
    {
#if UNITY_EDITOR
        static GameObject _disabledIndestructibleGameObject;
#endif

        // Due to the way that Unity overrides the Equals operator,
        // normal null checks such as (x == null) do not always work as
        // expected
        // In those cases you can use this function which will also
        // work with non-unity objects
        public static bool IsNull(System.Object obj)
        {
            return obj == null || obj.Equals(null);
        }

#if UNITY_EDITOR
        // Returns a Transform in the DontDestroyOnLoad scene (or, if we're not in play mode, within the current active scene)
        // whose GameObject is inactive, and whose hide flags are set to HideAndDontSave. We can instantiate prefabs in here
        // without any of their Awake() methods firing.
        public static Transform GetOrCreateInactivePrefabParent()
        {
            if(_disabledIndestructibleGameObject == null || (!Application.isPlaying && _disabledIndestructibleGameObject.scene != SceneManager.GetActiveScene()))
            {
                var go = new GameObject("ZenUtilInternal_PrefabParent");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.SetActive(false);

                if(Application.isPlaying)
                {
                    UnityEngine.Object.DontDestroyOnLoad(go);
                }

                _disabledIndestructibleGameObject = go;
            }

            return _disabledIndestructibleGameObject.transform;
        }
#endif
    }
}
