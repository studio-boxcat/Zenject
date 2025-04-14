// ReSharper disable CoVariantArrayConversion

using System;
using System.Diagnostics;
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

        public void Install(InstallScheme scheme, [CanBeNull] DiContainer parentContainer, Component context)
        {
#if DEBUG
            try
#endif
            {
                // Install ScriptableObjectInstallers first.
                {
                    InstallBindingsSO(_scriptableObjectInstallers, scheme, context);
                }

                // Inject and then Install MonoBehaviourInstallers.
                if (_monoInstallers.Length is not 0)
                {
                    var injectionProxy = scheme.AsInjectionProxy(parentContainer);
                    InjectToInstallers(_monoInstallers, injectionProxy);
                    InstallBindingsMono(_monoInstallers, scheme);
                }
            }
#if DEBUG
            catch
            {
                LogErrorWithStatus();
                throw;
            }
#endif
            return;

            static void InstallBindingsSO(ScriptableObjectInstaller[] installers, InstallScheme scheme, Component context)
            {
                EnsureInstallers(installers);
                var count = installers.Length;
                for (var index = 0; index < count; index++)
                {
                    var installer = installers[index];
                    RecordStatus("InstallBindings", installer);
                    installer.InstallBindings(scheme, context);
                }
            }

            static void InstallBindingsMono(MonoBehaviourInstaller[] installers, InstallScheme scheme)
            {
                EnsureInstallers(installers);
                var count = installers.Length;
                for (var index = 0; index < count; index++)
                {
                    var installer = installers[index];
                    RecordStatus("InstallBindings", installer);
                    installer.InstallBindings(scheme);
                }
            }

            static void InjectToInstallers(MonoBehaviourInstaller[] installers, DiContainer container)
            {
                var count = installers.Length;

                for (var index = 0; index < count; index++)
                {
                    var installer = installers[index];
                    RecordStatus("Inject", installer);
                    container.Inject(installer, default);
                }
            }

            [Conditional("DEBUG")]
            static void EnsureInstallers(Object[] installers)
            {
                for (var index = 0; index < installers.Length; index++)
                {
                    var installer = installers[index];
                    if (installer) continue;
                    throw new Exception("Installer is null at index: " + index);
                }
            }
        }

#if DEBUG
        private static string _statusMessage;
#endif

        [Conditional("DEBUG")]
        private static void RecordStatus(string status, Object target)
        {
#if DEBUG
            _statusMessage = $"{status} {target.name} ({target.GetType().Name})";
            L.I(_statusMessage, target);
#endif
        }

        [Conditional("DEBUG")]
        private static void LogErrorWithStatus()
        {
#if DEBUG
            L.E(_statusMessage);
            _statusMessage = null;
#endif
        }
    }
}