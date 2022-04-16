using UnityEngine;

namespace Zenject
{
    public struct GameObjectCreationParameters
    {
        public Transform ParentTransform;
        public Vector3? Position;
        public Quaternion? Rotation;
    }
}