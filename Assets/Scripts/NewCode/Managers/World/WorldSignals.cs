// Game.World/Signals/WorldSignals.cs
using System;

namespace Game.World.Signals
{
    /// ѕроста€ событийна€ шина дл€ мира.
    public static class WorldSignals
    {
        /// —рабатывает перед/во врем€ регенерации мира (смена сида и т.д.)
        public static event Action OnWorldRegen;

        public static void FireWorldRegen() => OnWorldRegen?.Invoke();
    }
}