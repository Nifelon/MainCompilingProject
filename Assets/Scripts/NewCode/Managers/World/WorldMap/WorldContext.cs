using System;
using UnityEngine;

namespace Game.Core
{
    /// �������� ����, ������� �������� ��� ���������� � Initialize(...).
    /// ��� ������ � ��������� ������ (WorldManager) � � �������� (����� �������).
    public sealed class WorldContext
    {
        /// �������� ������ (Size/Seed/Width/Height).
        public WorldManager World { get; }

        /// ������� �������� (������������ ����������� ����� ������������).
        public ServiceLocator Services { get; }

        // ������� ������ � ������ ������ ���������� �������� �� World.
        public Vector2Int Size => World.SizeMap;
        public int Width => World.Width;
        public int Height => World.Height;
        public int Seed => World.Seed;

        /// ������ ��������. ������ ���������� �� WorldManager.InitializeWorld().
        public WorldContext(WorldManager world, ServiceLocator services = null)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Services = services ?? new ServiceLocator();
        }

        // ��������������� ������ ��� �������� (������ � ������������):

        /// ������� ������ ��� null, ���� �� ���������������.
        public T GetService<T>() where T : class => Services.Get<T>();

        /// ������� ������ ��� ������� �������� ���������� (��� ��������� ������������).
        public T RequireService<T>() where T : class
        {
            var s = Services.Get<T>();
            if (s == null)
                throw new InvalidOperationException($"������ {typeof(T).Name} �� ��������������� � WorldContext.");
            return s;
        }

        /// ����������� �������� ������ ��� ����������.
        public bool TryGetService<T>(out T service) where T : class
        {
            service = Services.Get<T>();
            return service != null;
        }
    }
}