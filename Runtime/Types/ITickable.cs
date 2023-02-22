namespace Zenject
{
    public interface ITickable
    {
        void Tick();
    }

    public interface ILateTickable
    {
        void LateTick();
    }
}

