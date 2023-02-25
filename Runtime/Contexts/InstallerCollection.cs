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
        MonoBehaviourInstaller[] _monoInstallers;


        public InstallerCollection(
            ScriptableObjectInstaller[] scriptableObjectInstallers,
            MonoBehaviourInstaller[] monoInstallers)
        {
            _scriptableObjectInstallers = scriptableObjectInstallers;
            _monoInstallers = monoInstallers;
        }

        public void InjectAndInstall(DiContainer container)
        {
            for (var index = 0; index < _scriptableObjectInstallers.Length; index++)
            {
                var installer = _scriptableObjectInstallers[index];
#if DEBUG
                if (installer == null)
                    Debug.LogError("ScriptableObjectInstaller is null at index: " + index);
#endif
                InjectAndInstallBindings(container, installer);
            }

            for (var index = 0; index < _monoInstallers.Length; index++)
            {
                var installer = _monoInstallers[index];
#if DEBUG
                if (installer == null)
                    Debug.LogError("MonoBehaviourInstaller is null at index: " + index);
#endif
                InjectAndInstallBindings(container, installer);
            }

            _scriptableObjectInstallers = default;
            _monoInstallers = default;

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
                    installer.InstallBindings(container);
                }
#if DEBUG
                catch (Exception)
                {
                    Debug.LogError("Failed to Install Bindings of Installer: " + installer,
                        (UnityEngine.Object) installer);
                    throw;
                }
#endif
            }
        }
    }
}