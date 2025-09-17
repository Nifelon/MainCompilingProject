using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class ServiceLocator
    {
        private readonly Dictionary<Type, object> _map = new();

        public void Register<T>(T instance) where T : class
        {
            _map[typeof(T)] = instance; // �����������, ���� ��� ��� � ��� �� ��� �����
        }

        public T Get<T>() where T : class
        {
            return _map.TryGetValue(typeof(T), out var o) ? (T)o : null;
        }

        public bool Contains<T>() where T : class => _map.ContainsKey(typeof(T));

        // ����������� � ������ �� ������� �����������
        public bool TryRegister<T>(T instance) where T : class
        {
            var t = typeof(T);
            if (_map.ContainsKey(t)) return false;
            _map[t] = instance;
            return true;
        }

        // ����������� � �������
        public IEnumerable<Type> ListRegisteredTypes() => _map.Keys;
    }
}
