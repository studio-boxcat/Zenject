using System;
using System.Collections.Generic;
using System.Linq;
using ModestTree;
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

#if !NOT_UNITY3D
        public static void GetInjectableMonoBehavioursInScene(
            Scene scene, List<MonoBehaviour> monoBehaviours)
        {
            foreach (var rootObj in GetRootGameObjects(scene))
            {
                if (rootObj != null)
                {
                    GetInjectableMonoBehavioursUnderGameObjectInternal(rootObj, monoBehaviours);
                }
            }
        }

        // NOTE: This method will not return components that are within a GameObjectContext
        // It returns monobehaviours in a bottom-up order
        public static void GetInjectableMonoBehavioursUnderGameObject(
            GameObject gameObject, List<MonoBehaviour> injectableComponents)
        {
            GetInjectableMonoBehavioursUnderGameObjectInternal(gameObject, injectableComponents);
        }

        static void GetInjectableMonoBehavioursUnderGameObjectInternal(
            GameObject gameObject, List<MonoBehaviour> injectableComponents)
        {
            if (gameObject == null)
            {
                return;
            }

            var monoBehaviours = gameObject.GetComponents<MonoBehaviour>();

            // Recurse first so it adds components bottom up though it shouldn't really matter much
            // because it should always inject in the dependency order
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i);

                if (child != null)
                {
                    GetInjectableMonoBehavioursUnderGameObjectInternal(child.gameObject, injectableComponents);
                }
            }

            for (int i = 0; i < monoBehaviours.Length; i++)
            {
                var monoBehaviour = monoBehaviours[i];

                // Can be null for broken component references
                if (monoBehaviour != null
                    && IsInjectableMonoBehaviourType(monoBehaviour.GetType()))
                {
                    injectableComponents.Add(monoBehaviour);
                }
            }
        }

        public static bool IsInjectableMonoBehaviourType(Type type)
        {
            // Do not inject on installers since these are always injected before they are installed
            return type != null && !type.DerivesFrom<MonoInstaller>() && TypeAnalyzer.HasInfo(type);
        }

        public static IEnumerable<GameObject> GetRootGameObjects(Scene scene)
        {
            if (scene.isLoaded)
            {
                return scene.GetRootGameObjects()
                    .Where(x => x.GetComponent<ProjectContext>() == null);
            }

            // Note: We can't use scene.GetRootObjects() here because that apparently fails with an exception
            // about the scene not being loaded yet when executed in Awake
            // We also can't use GameObject.FindObjectsOfType<Transform>() because that does not include inactive game objects
            // So we use Resources.FindObjectsOfTypeAll, even though that may include prefabs.  However, our assumption here
            // is that prefabs do not have their "scene" property set correctly so this should work
            //
            // It's important here that we only inject into root objects that are part of our scene, to properly support
            // multi-scene editing features of Unity 5.x
            //
            // Also, even with older Unity versions, if there is an object that is marked with DontDestroyOnLoad, then it will
            // be injected multiple times when another scene is loaded
            //
            // We also make sure not to inject into the project root objects which are injected by ProjectContext.
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(x => x.transform.parent == null
                            && x.GetComponent<ProjectContext>() == null
                            && x.scene == scene);
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

#endif
    }
}
