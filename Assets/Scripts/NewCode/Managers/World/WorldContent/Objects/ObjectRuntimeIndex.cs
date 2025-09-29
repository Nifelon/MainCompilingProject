using System.Collections.Generic;

namespace Game.World.Objects
{
    /// Индекс активных инстансов: по чанку → по id → хэндл.
    /// Никакой логики пула/спавна — только учёт и быстрые выборки.
    public sealed class ObjectRuntimeIndex
    {
        private readonly Dictionary<ulong, Dictionary<ulong, ObjectHandle>> _byChunk = new();

        // --- базовые операции ---

        public void Add(ulong chunkKey, ObjectHandle h)
        {
            if (!h.IsValid) return;
            if (!_byChunk.TryGetValue(chunkKey, out var dict))
            {
                dict = new Dictionary<ulong, ObjectHandle>(32);
                _byChunk[chunkKey] = dict;
            }
            dict[h.Id] = h; // upsert
        }

        public bool Remove(ulong chunkKey, ulong id, out ObjectHandle h)
        {
            h = default;
            return _byChunk.TryGetValue(chunkKey, out var dict) && dict.Remove(id, out h);
        }

        public bool TryGet(ulong chunkKey, ulong id, out ObjectHandle h)
        {
            h = default;
            return _byChunk.TryGetValue(chunkKey, out var dict) && dict.TryGetValue(id, out h);
        }

        public bool Contains(ulong chunkKey, ulong id)
            => _byChunk.TryGetValue(chunkKey, out var dict) && dict.ContainsKey(id);

        /// Возвращает read-only представление словаря чанка (или null, если чанка нет).
        public IReadOnlyDictionary<ulong, ObjectHandle> GetChunk(ulong chunkKey)
            => _byChunk.TryGetValue(chunkKey, out var dict) ? dict : null;

        public int GetActiveCountInChunk(ulong chunkKey)
            => _byChunk.TryGetValue(chunkKey, out var dict) ? dict.Count : 0;

        /// Быстрый перебор всех ключей чанков (снимок).
        public void GetAllChunkKeys(List<ulong> buffer)
        {
            buffer.Clear();
            foreach (var key in _byChunk.Keys) buffer.Add(key);
        }

        // --- массовое удаление ---

        /// Удаляет все записи чанка и возвращает перечисление хэндлов (для despawn).
        public IEnumerable<ObjectHandle> RemoveAllInChunk(ulong chunkKey)
        {
            if (_byChunk.TryGetValue(chunkKey, out var dict))
            {
                // снимок ключей, чтобы безопасно удалять
                var tmpIds = ListPool<ulong>.Get(); // см. простой пул ниже; можно заменить на локальный List
                try
                {
                    foreach (var id in dict.Keys) tmpIds.Add(id);
                    foreach (var id in tmpIds)
                    {
                        if (dict.Remove(id, out var h)) yield return h;
                    }
                }
                finally
                {
                    _byChunk.Remove(chunkKey);
                    ListPool<ulong>.Release(tmpIds);
                }
            }
        }

        /// Быстрое удаление с внешним буфером (без IEnumerable аллокаций).
        public int RemoveAllInChunk(ulong chunkKey, List<ObjectHandle> buffer)
        {
            buffer.Clear();
            if (_byChunk.TryGetValue(chunkKey, out var dict))
            {
                var tmpIds = ListPool<ulong>.Get();
                try
                {
                    foreach (var id in dict.Keys) tmpIds.Add(id);
                    foreach (var id in tmpIds)
                    {
                        if (dict.Remove(id, out var h)) buffer.Add(h);
                    }
                }
                finally
                {
                    _byChunk.Remove(chunkKey);
                    ListPool<ulong>.Release(tmpIds);
                }
            }
            return buffer.Count;
        }

        public void Clear() => _byChunk.Clear();
    }

    /// Очень простой пул списков, чтобы избежать лишних аллокаций на горячем пути.
    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool = new();

        public static List<T> Get()
            => _pool.Count > 0 ? _pool.Pop() : new List<T>(64);

        public static void Release(List<T> list)
        {
            list.Clear();
            _pool.Push(list);
        }
    }
}