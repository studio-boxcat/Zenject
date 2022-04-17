#if !NOT_UNITY3D

using UnityEngine;

namespace Zenject
{
    public abstract class Context : MonoBehaviour
    {
        [SerializeField]
        ScriptableObjectInstaller[] _scriptableObjectInstallers;

        [SerializeField]
        MonoInstaller[] _monoInstallers;

        public abstract DiContainer Container { get; }

        protected void InstallInstallers()
        {
            // Ideally we would just have one flat list of all the installers
            // since that way the user has complete control over the order, but
            // that's not possible since Unity does not allow serializing lists of interfaces
            // (and it has to be an inteface since the scriptable object installers only share
            // the interface)
            //
            // So the best we can do is have a hard-coded order in terms of the installer type
            //
            // The order is:
            //      - ScriptableObject installers
            //      - MonoInstallers in the scene
            //
            // We put ScriptableObject installers before the MonoInstallers because
            // ScriptableObjectInstallers are often used for settings (including settings
            // that are injected into other installers like MonoInstallers)

            foreach (var installer in _scriptableObjectInstallers)
            {
                Container.Inject(installer);
                installer.InstallBindings();
            }

            foreach (var installer in _monoInstallers)
            {
                Container.Inject(installer);
                installer.InstallBindings();
            }
        }
    }
}

#endif
