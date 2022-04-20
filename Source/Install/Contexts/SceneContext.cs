using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Zenject
{
    public class SceneContext : MonoBehaviour
    {
        public static Action<DiContainer> ExtraBindingsInstallMethod;

        public DiContainer Container;

        [InlineProperty, HideLabel]
        public InstallerCollection InstallerCollection;

        public void Awake()
        {
            SceneContextRegistry.Add(this);

            Container = new DiContainer(ProjectContext.Instance.Container);

            Container.Bind(typeof(MonoKernel)).FromNewComponentOn(gameObject).NonLazy();

            gameObject.GetComponent<ZenjectBindingCollection>().Bind(Container);
            Container.QueueForInject(gameObject.GetComponent<InjectTargetCollection>().Targets);

            if (ExtraBindingsInstallMethod != null)
            {
                ExtraBindingsInstallMethod(Container);
                ExtraBindingsInstallMethod = null;
            }

            InstallerCollection.InjectAndInstall(Container);

            Container.ResolveRoots();
        }

        void OnDestroy()
        {
            SceneContextRegistry.Remove(this);
        }
    }
}