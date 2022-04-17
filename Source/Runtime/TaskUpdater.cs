using System.Collections.Generic;
using System.Diagnostics;

namespace Zenject
{
    // Update tasks once per frame based on a priority
    [DebuggerStepThrough]
    public abstract class TaskUpdater<TTask> where TTask : class
    {
        readonly List<(TTask Task, int Priority)> _tasks = new();
        bool _sorted = true;
        int _priorityMax = 0;

        public void AddTask(TTask task, int priority)
        {
            _tasks.Add((task, priority));

            if (_tasks.Count == 1)
                _priorityMax = priority;

            // 정렬된 상태 + 재정렬이 필요없을 경우.
            if (_sorted && priority >= _priorityMax)
            {
                _priorityMax = priority;
            }
            // 재정렬이 필요할 경우.
            else
            {
                _sorted = false;
            }
        }

        public void UpdateAll()
        {
            if (_sorted == false)
            {
                _tasks.Sort((a, b) => a.Priority - b.Priority);
                _sorted = true;
            }

            // XXX: 재진입을 지원하기 위해서 Count 까지만 업데이트.
            var count = _tasks.Count;
            for (var i = 0; i < count; i++)
                UpdateItem(_tasks[i].Task);
        }

        protected abstract void UpdateItem(TTask task);
    }

    public class TickablesTaskUpdater : TaskUpdater<ITickable>
    {
        protected override void UpdateItem(ITickable task)
        {
            task.Tick();
        }
    }

    public class LateTickablesTaskUpdater : TaskUpdater<ILateTickable>
    {
        protected override void UpdateItem(ILateTickable task)
        {
            task.LateTick();
        }
    }
}