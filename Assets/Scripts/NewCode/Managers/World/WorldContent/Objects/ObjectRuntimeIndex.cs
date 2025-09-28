using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Objects
{
    /// Индекс активных инстансов: по чанку → по id → хэндл.
    /// Никакой логики пула/спавна — только учёт и быстрые выборки.
    public sealed class ObjectRuntimeIndex
    {
        private readonly Dictionary<ulong, Dictionary<ulong, ObjectHandle>> _byChunk = new();
        private static readonly List<ulong> _tmpIds = new(64);

        public void Add(ulong chunkKey, ObjectHandle h)
        {
            if (!h.IsValid) return;
            if (!_byChunk.TryGetValue(chunkKey, out var dict))
            {
                dict = new Dictionary<ulong, ObjectHandle>(32);
                _byChunk[chunkKey] = dict;
            }
            dict[h.Id] = h;
        }

        public bool Remove(ulong chunkKey, ulong id, out ObjectHandle h)
        {
            h = default;
            return _byChunk.TryGetValue(chunkKey, out var dict) && dict.Remove(id, out h);
        }

        public IReadOnlyDictionary<ulong, ObjectHandle> GetChunk(ulong chunkKey)
            => _byChunk.TryGetValue(chunkKey, out var dict) ? dict : null;

        public IEnumerable<ObjectHandle> RemoveAllInChunk(ulong chunkKey)
        {
            if (_byChunk.TryGetValue(chunkKey, out var dict))
            {
                _tmpIds.Clear();
                foreach (var id in dict.Keys) _tmpIds.Add(id);
                foreach (var id in _tmpIds)
                {
                    if (dict.Remove(id, out var h)) yield return h;
                }
                _byChunk.Remove(chunkKey);
            }
        }

        public void Clear() => _byChunk.Clear();
    }
}