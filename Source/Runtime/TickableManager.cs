using System.Collections.Generic;

namespace Zenject
{
    public class TickableManager
    {
        readonly List<ITickable> _tickables;
        readonly List<ILateTickable> _lateTickables;

        public TickableManager(List<ITickable> tickables, List<ILateTickable> lateTickables)
        {
            _tickables = tickables;
            _lateTickables = lateTickables;
        }

        public void Update()
        {
            // XXX: 재진입을 지원하기 위해서 Count 까지만 업데이트.
            var count = _tickables.Count;
            for (var i = 0; i < count; i++)
                _tickables[i].Tick();
        }

        public void LateUpdate()
        {
            // XXX: 재진입을 지원하기 위해서 Count 까지만 업데이트.
            var count = _lateTickables.Count;
            for (var i = 0; i < count; i++)
                _lateTickables[i].LateTick();
        }
    }
}