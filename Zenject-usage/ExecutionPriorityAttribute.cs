using System;

namespace Zenject
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExecutionPriorityAttribute : Attribute
    {
        public readonly int Priority;

        public ExecutionPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}