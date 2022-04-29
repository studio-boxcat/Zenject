using System;
using ModestTree;
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
            {
                Log.Debug("Inject: " + installer.name);
                container.Inject(installer);
                Log.Debug("InstallBindings: " + installer.name);
                installer.InstallBindings();
            }

            foreach (var installer in _monoInstallers)
            {
                Log.Debug("Inject: " + installer.name);
                container.Inject(installer);
                Log.Debug("InstallBindings: " + installer.name);
                installer.InstallBindings();
            }
        }
    }
}