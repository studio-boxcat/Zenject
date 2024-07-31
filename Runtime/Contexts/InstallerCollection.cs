using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Zenject
{
    [Serializable]
    struct InstallerCollection
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

        public void InjectAndInstall(DiContainer container, ArgumentArray extraArgs)
        {
            for (var index = 0; index < _scriptableObjectInstallers.Length; index++)
            {
                var installer = _scriptableObjectInstallers[index];
#if DEBUG
                if (installer == null)
                    Debug.LogError("ScriptableObjectInstaller is null at index: " + index);
#endif
                InjectAndInstallBindings(container, extraArgs, installer);
            }

            for (var index = 0; index < _monoInstallers.Length; index++)
            {
                var installer = _monoInstallers[index];
#if DEBUG
                if (installer == null)
                    Debug.LogError("MonoBehaviourInstaller is null at index: " + index);
#endif
                InjectAndInstallBindings(container, extraArgs, installer);
            }

            _scriptableObjectInstallers = default;
            _monoInstallers = default;
            return;

            static void InjectAndInstallBindings(DiContainer container, ArgumentArray extraArgs, IInstaller installer)
            {
#if DEBUG
                try
                {
                    L.I("Inject: " + GetDebugName(installer), (Object) installer);
                    container.Inject(installer, extraArgs);
                }
                catch (Exception)
                {
                    L.E("Failed to Inject to Installer: " + GetDebugName(installer), (Object) installer);
                    throw;
                }

                try
                {
                    L.I("InstallBindings: " + GetDebugName(installer), (Object) installer);
                    installer.InstallBindings(container);
                }
                catch (Exception)
                {
                    L.E("Failed to Install Bindings of Installer: " + ((Object) installer).name, (Object) installer);
                    throw;
                }

                static string GetDebugName(IInstaller installer)
                {
                    var obj = (Object) installer;
                    return $"{obj.name} ({obj.GetType().Name})";
                }
#else
                container.Inject(installer, extraArgs);
                installer.InstallBindings(container);
#endif
            }
        }
    }
}