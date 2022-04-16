using System;
#if !NOT_UNITY3D
using UnityEngine;
#endif

namespace Zenject
{
    [Serializable]
    [NoReflectionBaking]
    public class ZenjectSettings
    {
        public static ZenjectSettings Default = new ZenjectSettings();

#if !NOT_UNITY3D
        [SerializeField]
#endif
        bool _ensureDeterministicDestructionOrderOnApplicationQuit;

#if !NOT_UNITY3D
        [SerializeField]
#endif
        bool _displayWarningWhenResolvingDuringInstall;

        public ZenjectSettings(
            bool displayWarningWhenResolvingDuringInstall = true,
            bool ensureDeterministicDestructionOrderOnApplicationQuit = false)
        {
            _displayWarningWhenResolvingDuringInstall = displayWarningWhenResolvingDuringInstall;
            _ensureDeterministicDestructionOrderOnApplicationQuit =ensureDeterministicDestructionOrderOnApplicationQuit;
        }

        public bool DisplayWarningWhenResolvingDuringInstall
        {
            get { return _displayWarningWhenResolvingDuringInstall; }
        }

        // When this is set to true and the application is exitted, all the scenes will be
        // destroyed in the reverse order in which they were loaded, and then the project context
        // will be destroyed last
        // When this is set to false (the default) the order that this occurs in is not predictable
        // It is set to false by default because manually destroying objects during OnApplicationQuit
        // event can cause crashes on android (see github issue #468)
        public bool EnsureDeterministicDestructionOrderOnApplicationQuit
        {
            get { return _ensureDeterministicDestructionOrderOnApplicationQuit; }
        }
    }
}
