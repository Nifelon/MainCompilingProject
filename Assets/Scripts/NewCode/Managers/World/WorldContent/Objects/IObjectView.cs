using UnityEngine;

namespace Game.World.Objects
{
    public interface IObjectView
    {
        ObjectHandle Spawn(ObjectInstanceData inst);
        void Despawn(ObjectHandle handle);
        Bounds GetWorldBounds(ObjectHandle handle);
    }

    public readonly struct ObjectHandle
    {
        public readonly ulong Id;
        public readonly GameObject Go;

        public ObjectHandle(ulong id, GameObject go) { Id = id; Go = go; }
        public bool IsValid => Go;
    }
}