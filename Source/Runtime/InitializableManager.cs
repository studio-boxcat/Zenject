using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModestTree;

namespace Zenject
{
    // Responsibilities:
    // - Run Initialize() on all Iinitializable's, in the order specified by InitPriority
    public class InitializableManager
    {
        readonly List<InitializableInfo> _initializables;

        bool _hasInitialized;

        [Inject]
        public InitializableManager(
            [Inject(Optional = true, Source = InjectSources.Local)]
            List<IInitializable> initializables)
        {
            _initializables = new List<InitializableInfo>(initializables.Count);

            foreach (var initializable in initializables)
            {
                // Note that we use zero for unspecified priority
                // This is nice because you can use negative or positive for before/after unspecified
                var priority = initializable.GetType().GetCustomAttribute<ExecutionPriorityAttribute>()?.Priority ?? 0;
                _initializables.Add(new InitializableInfo(initializable, priority));
            }
        }

        public void Add(IInitializable initializable)
        {
            Add(initializable, 0);
        }

        public void Add(IInitializable initializable, int priority)
        {
            Assert.That(!_hasInitialized);
            _initializables.Add(new InitializableInfo(initializable, priority));
        }

        public void Initialize()
        {
            Assert.That(!_hasInitialized);
            _hasInitialized = true;

            _initializables.Sort((a, b)=> a.Priority.CompareTo(b.Priority));

#if UNITY_EDITOR
            foreach (var initializable in _initializables.Select(x => x.Initializable).GetDuplicates())
            {
                Assert.That(false, "Found duplicate IInitializable with type '{0}'".Fmt(initializable.GetType()));
            }
#endif

            foreach (var initializable in _initializables)
            {
                try
                {
#if ZEN_INTERNAL_PROFILING
                    using (ProfileTimers.CreateTimedBlock("User Code"))
#endif
#if UNITY_EDITOR
                    using (ProfileBlock.Start("{0}.Initialize()", initializable.Initializable.GetType()))
#endif
                    {
                        initializable.Initializable.Initialize();
                    }
                }
                catch (Exception e)
                {
                    throw Assert.CreateException(
                        e, "Error occurred while initializing IInitializable with type '{0}'", initializable.Initializable.GetType());
                }
            }
        }

        struct InitializableInfo
        {
            public readonly IInitializable Initializable;
            public readonly int Priority;

            public InitializableInfo(IInitializable initializable, int priority)
            {
                Initializable = initializable;
                Priority = priority;
            }
        }
    }
}
