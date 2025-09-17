using System.Collections.Generic;

namespace Game.Core
{
    /// Подсистема, которая содержит другие подсистемы (матрёшка).
    public interface IWorldComposite : IWorldSystem
    {
        IReadOnlyList<IWorldSystem> Children { get; }
    }
}
