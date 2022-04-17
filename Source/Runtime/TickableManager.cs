using System.Collections.Generic;
using System.Reflection;

namespace Zenject
{
    public class TickableManager
    {
        [Inject(Optional = true, Source = InjectSources.Local)]
        readonly List<ITickable> _tickables = null;

        [Inject(Optional = true, Source = InjectSources.Local)]
        readonly List<ILateTickable> _lateTickables = null;

        readonly TickablesTaskUpdater _updater = new();
        readonly LateTickablesTaskUpdater _lateUpdater = new();

        [Inject]
        public void Initialize()
        {
            InitTickables();
            InitLateTickables();
        }

        void InitTickables()
        {
            foreach (var tickable in _tickables)
            {
                // Note that we use zero for unspecified priority
                // This is nice because you can use negative or positive for before/after unspecified
                var priority = tickable.GetType().GetCustomAttribute<ExecutionPriorityAttribute>()?.Priority ?? 0;
                _updater.AddTask(tickable, priority);
            }
        }

        void InitLateTickables()
        {
            foreach (var tickable in _lateTickables)
            {
                // Note that we use zero for unspecified priority
                // This is nice because you can use negative or positive for before/after unspecified
                var priority = tickable.GetType().GetCustomAttribute<ExecutionPriorityAttribute>()?.Priority ?? 0;
                _lateUpdater.AddTask(tickable, priority);
            }
        }

        public void Update()
        {
            _updater.UpdateAll();
        }

        public void LateUpdate()
        {
            _lateUpdater.UpdateAll();
        }
    }
}
