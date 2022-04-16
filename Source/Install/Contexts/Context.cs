#if !NOT_UNITY3D

using System.Collections.Generic;
using System.Linq;
using ModestTree;
using UnityEngine;

namespace Zenject
{
    public abstract class Context : MonoBehaviour
    {
        [SerializeField]
        List<ScriptableObjectInstaller> _scriptableObjectInstallers = new List<ScriptableObjectInstaller>();

        [SerializeField]
        List<MonoInstaller> _monoInstallers = new List<MonoInstaller>();

        [SerializeField]
        List<MonoInstaller> _installerPrefabs = new List<MonoInstaller>();

        public IEnumerable<MonoInstaller> Installers
        {
            get { return _monoInstallers; }
            set
            {
                _monoInstallers.Clear();
                _monoInstallers.AddRange(value);
            }
        }

        public IEnumerable<MonoInstaller> InstallerPrefabs
        {
            get { return _installerPrefabs; }
            set
            {
                _installerPrefabs.Clear();
                _installerPrefabs.AddRange(value);
            }
        }

        public IEnumerable<ScriptableObjectInstaller> ScriptableObjectInstallers
        {
            get { return _scriptableObjectInstallers; }
            set
            {
                _scriptableObjectInstallers.Clear();
                _scriptableObjectInstallers.AddRange(value);
            }
        }

        public abstract DiContainer Container
        {
            get;
        }
        public abstract IEnumerable<GameObject> GetRootGameObjects();


        void CheckInstallerPrefabTypes(List<MonoInstaller> installers, List<MonoInstaller> installerPrefabs)
        {
            foreach (var installer in installers)
            {
                Assert.IsNotNull(installer, "Found null installer in Context '{0}'", name);

                /*
#if UNITY_EDITOR
#if UNITY_2019_1_OR_NEWER
                // TODO - Is there a way to check this using GetPrefabAssetType in 2019+?
#else
#if UNITY_2018_3
                Assert.That(PrefabUtility.GetPrefabAssetType(installer.gameObject) == PrefabAssetType.NotAPrefab,
#else
                Assert.That(PrefabUtility.GetPrefabType(installer.gameObject) != PrefabType.Prefab,
#endif
                    "Found prefab with name '{0}' in the Installer property of Context '{1}'.  You should use the property 'InstallerPrefabs' for this instead.", installer.name, name);
#endif
#endif
                */
            }

            foreach (var installerPrefab in installerPrefabs)
            {
                Assert.IsNotNull(installerPrefab, "Found null prefab in Context");

                // We'd like to do this but this is actually a valid case sometimes
                // (eg. loading an asset bundle with a scene containing a scene context when inside unity editor)
//#if UNITY_EDITOR
                //Assert.That(PrefabUtility.GetPrefabType(installerPrefab.gameObject) == PrefabType.Prefab,
                    //"Found non-prefab with name '{0}' in the InstallerPrefabs property of Context '{1}'.  You should use the property 'Installer' for this instead",
                    //installerPrefab.name, this.name);
//#endif
                Assert.That(installerPrefab.GetComponent<MonoInstaller>() != null,
                    "Expected to find component with type 'MonoInstaller' on given installer prefab '{0}'", installerPrefab.name);
            }
        }

        protected void InstallInstallers()
        {
            InstallInstallers(_scriptableObjectInstallers, _monoInstallers, _installerPrefabs);
        }

        protected void InstallInstallers(
            List<ScriptableObjectInstaller> scriptableObjectInstallers,
            List<MonoInstaller> installers,
            List<MonoInstaller> installerPrefabs)
        {
            CheckInstallerPrefabTypes(installers, installerPrefabs);

            // Ideally we would just have one flat list of all the installers
            // since that way the user has complete control over the order, but
            // that's not possible since Unity does not allow serializing lists of interfaces
            // (and it has to be an inteface since the scriptable object installers only share
            // the interface)
            //
            // So the best we can do is have a hard-coded order in terms of the installer type
            //
            // The order is:
            //      - Normal installers given directly via code
            //      - ScriptableObject installers
            //      - MonoInstallers in the scene
            //      - Prefab Installers
            //
            // We put ScriptableObject installers before the MonoInstallers because
            // ScriptableObjectInstallers are often used for settings (including settings
            // that are injected into other installers like MonoInstallers)

            var allInstallers = scriptableObjectInstallers.Cast<IInstaller>()
                .Concat(installers.Cast<IInstaller>()).ToList();

            foreach (var installerPrefab in installerPrefabs)
            {
                Assert.IsNotNull(installerPrefab, "Found null installer prefab in '{0}'", GetType());

                GameObject installerGameObject;

                installerGameObject = GameObject.Instantiate(installerPrefab.gameObject);

                installerGameObject.transform.SetParent(transform, false);
                var installer = installerGameObject.GetComponent<MonoInstaller>();

                Assert.IsNotNull(installer, "Could not find installer component on prefab '{0}'", installerPrefab.name);

                allInstallers.Add(installer);
            }

            foreach (var installer in allInstallers)
            {
                Assert.IsNotNull(installer,
                    "Found null installer in '{0}'", GetType());

                Container.Inject(installer);

                installer.InstallBindings();
            }
        }

        protected void InstallSceneBindings(List<MonoBehaviour> injectableMonoBehaviours)
        {
            foreach (var binding in injectableMonoBehaviours.OfType<ZenjectBinding>())
            {
                if (binding == null)
                {
                    continue;
                }

                if (binding.Context == null || (binding.UseSceneContext && this is SceneContext))
                {
                    binding.Context = this;
                }
            }

            // We'd prefer to use GameObject.FindObjectsOfType<ZenjectBinding>() here
            // instead but that doesn't find inactive gameobjects
            // TODO: Consider changing this
            // Maybe ZenjectBinding could add itself to a registry class on Awake/OnEnable
            // then we could avoid calling the slow Resources.FindObjectsOfTypeAll here
            foreach (var binding in Resources.FindObjectsOfTypeAll<ZenjectBinding>())
            {
                if (binding == null)
                {
                    continue;
                }

                // This is necessary for cases where the ZenjectBinding is inside a GameObjectContext
                // since it won't be caught in the other loop above
                if (this is SceneContext)
                {
                    if (binding.Context == null && binding.UseSceneContext
                        && binding.gameObject.scene == gameObject.scene)
                    {
                        binding.Context = this;
                    }
                }

                if (binding.Context == this)
                {
                    InstallZenjectBinding(binding);
                }
            }
        }

        void InstallZenjectBinding(ZenjectBinding binding)
        {
            if (!binding.enabled)
            {
                return;
            }

            if (binding.Components == null || binding.Components.IsEmpty())
            {
                Log.Warn("Found empty list of components on ZenjectBinding on object '{0}'", binding.name);
                return;
            }

            string identifier = null;

            if (binding.Identifier.Trim().Length > 0)
            {
                identifier = binding.Identifier;
            }

            foreach (var component in binding.Components)
            {
                var bindType = binding.BindType;

                if (component == null)
                {
                    Log.Warn("Found null component in ZenjectBinding on object '{0}'", binding.name);
                    continue;
                }

                var componentType = component.GetType();

                switch (bindType)
                {
                    case ZenjectBinding.BindTypes.Self:
                    {
                        Container.Bind(componentType).WithId(identifier).FromInstance(component);
                        break;
                    }
                    case ZenjectBinding.BindTypes.AllInterfaces:
                    {
                        Container.Bind(componentType.Interfaces()).WithId(identifier).FromInstance(component);
                        break;
                    }
                    case ZenjectBinding.BindTypes.AllInterfacesAndSelf:
                    {
                        Container.Bind(componentType.Interfaces().Concat(new[] { componentType }).ToArray()).WithId(identifier).FromInstance(component);
                        break;
                    }
                    default:
                    {
                        throw Assert.CreateException();
                    }
                }
            }
        }

        protected abstract void GetInjectableMonoBehaviours(List<MonoBehaviour> components);
    }
}

#endif
