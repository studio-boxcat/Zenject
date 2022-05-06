using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    [Serializable]
    public struct InstallerCollection
    {
        [SerializeField, Required]
        ScriptableObjectInstaller[] _scriptableObjectInstallers;

        [SerializeField, Required]
        MonoInstaller[] _monoInstallers;


        public InstallerCollection(
            ScriptableObjectInstaller[] scriptableObjectInstallers,
            MonoInstaller[] monoInstallers)
        {
            _scriptableObjectInstallers = scriptableObjectInstallers;
            _monoInstallers = monoInstallers;
        }

        public void InjectAndInstall(DiContainer container)
        {
            foreach (var installer in _scriptableObjectInstallers)
                InjectAndInstallBindings(container, installer);

            foreach (var installer in _monoInstallers)
                InjectAndInstallBindings(container, installer);

            static void InjectAndInstallBindings(DiContainer container, IInstaller installer)
            {
#if DEBUG
                try
#endif
                {
                    // Log.Debug("Inject: " + installer.name);
                    container.Inject(installer);
                }
#if DEBUG
                catch (Exception)
                {
                    Debug.LogError("Failed to Inject to Installer: " + installer, (UnityEngine.Object) installer);
                    throw;
                }
#endif

#if DEBUG
                try
#endif
                {
                    // Log.Debug("InstallBindings: " + installer.name);
                    installer.InstallBindings();
                }
#if DEBUG
                catch (Exception)
                {
                    Debug.LogError("Failed to Install Bindings of Installer: " + installer, (UnityEngine.Object) installer);
                    throw;
                }
#endif
            }
        }
    }
}