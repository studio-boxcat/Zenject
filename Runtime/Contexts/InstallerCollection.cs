using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Zenject
{
    [Serializable]
    internal struct InstallerCollection
    {
        [SerializeField, Required]
        private ScriptableObjectInstaller[] _scriptableObjectInstallers;
        [SerializeField, Required]
        private MonoBehaviourInstaller[] _monoInstallers;


        public InstallerCollection(
            ScriptableObjectInstaller[] scriptableObjectInstallers,
            MonoBehaviourInstaller[] monoInstallers)
        {
            _scriptableObjectInstallers = scriptableObjectInstallers;
            _monoInstallers = monoInstallers;
        }

        public void Install(InstallScheme scheme, [CanBeNull] DiContainer parentContainer)
        {
            // Install ScriptableObjectInstallers first.
            {
                // ReSharper disable once CoVariantArrayConversion
                InstallBindings(_scriptableObjectInstallers, scheme);
            }

            // Inject and Install MonoBehaviourInstallers.
            if (_monoInstallers.Length is not 0)
            {
                // Inject first.
                var injectionProxy = scheme.AsInjectionProxy(parentContainer);
                InjectToInstallers(_monoInstallers, injectionProxy);

                // Then install.
                // ReSharper disable once CoVariantArrayConversion
                InstallBindings(_monoInstallers, scheme);
            }

            static void InstallBindings(IInstaller[] installers, InstallScheme scheme)
            {
                var count = installers.Length;

                for (var index = 0; index < count; index++)
                {
                    var installer = installers[index];

#if DEBUG
                    L.I("InstallBindings: " + GetDebugName(installer), (Object) installer);

                    if (installer == null)
                    {
                        L.E("Null Installer at index: " + index);
                        continue;
                    }

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
            }

            static void InjectToInstallers(MonoBehaviourInstaller[] installers, DiContainer container)
            {
                var count = installers.Length;

                for (var index = 0; index < count; index++)
                {
                    var installer = installers[index];

#if DEBUG
                    L.I("Inject: " + GetDebugName(installer), installer);

                    if (installer == null)
                    {
                        L.E("Null Installer at index: " + index);
                        continue;
                    }

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
}