using System.Linq;
using ModestTree;
using UnityEngine;

namespace Zenject
{
    public static class ContextUtils
    {
        // This is installed by default in ProjectContext, however, if you are using Zenject outside
        // of Unity then you might want to call this
        //
        // In this case though, you will have to manually call InitializableManager.Initialize,
        // DisposableManager.Dispose, TickableManager.Tick, etc. when appropriate for the environment
        // you are working in
        //
        // You might also want to use this installer in a ZenjectUnitTestFixture
        public static void InstallBindings_Managers(DiContainer container)
        {
            container.Bind(typeof(TickableManager)).ToSelf().AsSingle();
            container.Bind(typeof(DisposableManager)).ToSelf().AsSingle();
        }

        public static void InstallBindings_ZenjectBindings(Context context, Object[] injectableMonoBehaviours)
        {
            var container = context.Container;

            foreach (var injectableMonoBehaviour in injectableMonoBehaviours)
            {
                if (injectableMonoBehaviour is ZenjectBinding binding)
                    BindZenjectBinding(container, binding);
            }
        }

        static void BindZenjectBinding(DiContainer container, ZenjectBinding binding)
        {
            string identifier = null;

            if (binding.Identifier.Trim().Length > 0)
            {
                identifier = binding.Identifier;
            }

            foreach (var component in binding.Components)
            {
                var bindType = binding.BindType;

                if (component == null)
                {
                    Log.Warn("Found null component in ZenjectBinding on object '{0}'", binding.name);
                    continue;
                }

                var componentType = component.GetType();

                switch (bindType)
                {
                    case ZenjectBinding.BindTypes.Self:
                    {
                        container.Bind(componentType).WithId(identifier).FromInstance(component);
                        break;
                    }
                    case ZenjectBinding.BindTypes.AllInterfaces:
                    {
                        container.Bind(componentType.Interfaces()).WithId(identifier).FromInstance(component);
                        break;
                    }
                    case ZenjectBinding.BindTypes.AllInterfacesAndSelf:
                    {
                        container.Bind(componentType.Interfaces().Concat(new[] {componentType}).ToArray()).WithId(identifier).FromInstance(component);
                        break;
                    }
                    default:
                    {
                        throw Assert.CreateException();
                    }
                }
            }
        }
    }
}