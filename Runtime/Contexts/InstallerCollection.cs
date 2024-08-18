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

        public void InstallScriptableObjectInstallers(InstallScheme scheme)
        {
            foreach (var installer in _scriptableObjectInstallers)
                InstallBindings(installer, scheme);
        }

        public void InjectAndInstallMonoBehaviourInstallers(InstallScheme scheme, DiContainer parentContainer)
        {
            if (_monoInstallers.Length is 0)
                return;

            // Inject first.
            var injectionProxy = scheme.AsInjectionProxy(parentContainer);
            foreach (var installer in _monoInstallers)
                InjectToInstaller(injectionProxy, installer);

            // Then install.
            foreach (var installer in _monoInstallers)
                InstallBindings(installer, scheme);
        }

        static void InstallBindings(IInstaller installer, InstallScheme scheme)
        {
#if DEBUG
            L.I("InstallBindings: " + GetDebugName(installer), (Object) installer);

            try
#endif
            {
                installer.InstallBindings(scheme);
            }
#if DEBUG
            catch (Exception)
            {
                L.E("Failed to Install Bindings: " + (Object) installer, (Object) installer);
                throw;
            }
#endif
        }

        static void InjectToInstaller(DiContainer container, MonoBehaviourInstaller installer)
        {
#if DEBUG
            L.I("Inject: " + GetDebugName(installer), installer);

            try
#endif
            {
                container.Inject(installer, default);
            }
#if DEBUG
            catch (Exception)
            {
                L.E("Failed to Inject: " + GetDebugName(installer), installer);
                throw;
            }
#endif
        }

#if DEBUG
        static string GetDebugName(IInstaller installer)
        {
            var obj = (Object) installer;
            return obj != null
                ? $"{obj.name} ({obj.GetType().Name})"
                : "null";
        }
#endif
    }
}