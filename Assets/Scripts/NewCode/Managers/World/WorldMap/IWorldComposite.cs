using System.Collections.Generic;

namespace Game.Core
{
    /// ����������, ������� �������� ������ ���������� (�������).
    public interface IWorldComposite : IWorldSystem
    {
        IReadOnlyList<IWorldSystem> Children { get; }
    }
}
