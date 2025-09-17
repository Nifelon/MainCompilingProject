using System;
using UnityEngine;

namespace Game.Core
{
    /// Контекст мира, который получают все подсистемы в Initialize(...).
    /// Даёт доступ к источнику правды (WorldManager) и к сервисам (через локатор).
    public sealed class WorldContext
    {
        /// Источник правды (Size/Seed/Width/Height).
        public WorldManager World { get; }

        /// Локатор сервисов (интерфейсные зависимости между подсистемами).
        public ServiceLocator Services { get; }

        // Удобные прокси — всегда читаем актуальные значения из World.
        public Vector2Int Size => World.SizeMap;
        public int Width => World.Width;
        public int Height => World.Height;
        public int Seed => World.Seed;

        /// Создаём контекст. Обычно вызывается из WorldManager.InitializeWorld().
        public WorldContext(WorldManager world, ServiceLocator services = null)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Services = services ?? new ServiceLocator();
        }

        // Вспомогательные методы для сервисов (удобно в потребителях):

        /// Вернуть сервис или null, если не зарегистрирован.
        public T GetService<T>() where T : class => Services.Get<T>();

        /// Вернуть сервис или бросить понятное исключение (для критичных зависимостей).
        public T RequireService<T>() where T : class
        {
            var s = Services.Get<T>();
            if (s == null)
                throw new InvalidOperationException($"Сервис {typeof(T).Name} не зарегистрирован в WorldContext.");
            return s;
        }

        /// Попробовать получить сервис без исключений.
        public bool TryGetService<T>(out T service) where T : class
        {
            service = Services.Get<T>();
            return service != null;
        }
    }
}