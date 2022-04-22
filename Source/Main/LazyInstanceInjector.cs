using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Zenject
{
    // When the app starts up, typically there is a list of instances that need to be injected
    // The question is, what is the order that they should be injected?  Originally we would
    // just iterate over the list and inject in whatever order they were in
    // What is better than that though, is to inject based on their dependency order
    // So if A depends on B then it would be nice if B was always injected before A
    // That way, in [Inject] methods for A, A can access members on B knowing that it's
    // already been initialized.
    // So in order to do this, we add the initial pool of instances to this class then
    // notify this class whenever an instance is resolved via a FromInstance binding
    // That way we can lazily call inject on-demand whenever the instance is requested
    public class LazyInstanceInjector
    {
        readonly DiContainer _container;
        readonly List<object> _instancesToInject = new();

        bool _isInjecting;

        public LazyInstanceInjector(DiContainer container)
        {
            _container = container;
        }

        public void AddInstance(object instance)
        {
            Assert.IsFalse(_isInjecting);
            _instancesToInject.Add(instance);
            Assert.AreEqual(_instancesToInject.Count, _instancesToInject.Distinct().Count());
        }

        public void AddInstances(object[] instances)
        {
            Assert.IsFalse(_isInjecting);
            _instancesToInject.AddRange(instances);
            Assert.AreEqual(_instancesToInject.Count, _instancesToInject.Distinct().Count());
        }

        public void LazyInjectAll()
        {
            Assert.IsFalse(_isInjecting);
            _isInjecting = true;

            foreach (var instance in _instancesToInject)
            {
                // We use LazyInject instead of calling _container.inject directly
                // Because it might have already been lazily injected
                // as a result of a previous call to inject
                _container.Inject(instance);
            }

            _instancesToInject.Clear();
            _isInjecting = false;
        }
    }
}