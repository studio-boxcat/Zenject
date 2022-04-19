using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    [Serializable]
    public struct InstallerCollection
    {
        [SerializeField, Required]
        [ListDrawerSettings(DraggableItems = false)]
        ScriptableObjectInstaller[] _scriptableObjectInstallers;

        [SerializeField, Required]
        [ListDrawerSettings(DraggableItems = false)]
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
            {
                container.Inject(installer);
                installer.InstallBindings();
            }

            foreach (var installer in _monoInstallers)
            {
                container.Inject(installer);
                installer.InstallBindings();
            }
        }
    }
}